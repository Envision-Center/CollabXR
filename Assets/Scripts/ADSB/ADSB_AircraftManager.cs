using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CesiumForUnity;
using CollabXR.Geolocation;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace CollabXR.ADSB
{
	public class ADSB_AircraftManager : MonoBehaviour
	{
		[Header("Prefab")]
		[SerializeField]
		private ADSB_AircraftNet aircraftPrefab;

		[Header("OpenSky OAuth2")]
		[SerializeField]
		private OpenSkyCredentials credentials;

		[Header("Polling")]
		[SerializeField, Range(1f, 60f)]
		private float pollIntervalSeconds = 10f;

		[SerializeField]
		private float staleTimeoutSeconds = 10f;

		[Header("Limits")]
		[SerializeField]
		private int maxAircraft = 120;

		[SerializeField]
		private int initialTrackSeedPoints = 12;

		[SerializeField]
		private bool seedTracksFromOpenSky = true;

		//tuning knobs for scale mode:
		[SerializeField]
		private float exaggeratedMultiplier = 25f;

		[SerializeField]
		private float exaggeratedMinUnity = 0.02f;

		[SerializeField]
		private float realScaleVisibilityMin = 0.0025f; // minimum aircraft localScale in real mode

		[SerializeField]
		private float realScaleVisibilityMax = 0.15f;

		[SerializeField]
		private bool pausePollingWhenHidden = true;

		public float RealScaleVisibilityMin => realScaleVisibilityMin;
		public float RealScaleVisibilityMax => realScaleVisibilityMax;

		public bool ExaggeratedScale { get; set; } = true;

		private readonly Dictionary<string, HexDbAircraftInfo> _hexdbCache = new();
		private readonly HashSet<string> _hexdbInFlight = new();

		private struct HexDbAircraftInfo
		{
			public string registration;
			public string icaoTypeCode;
			public string manufacturer;
			public string owner;
			public string typeModel;
		}

		public float ExaggeratedMultiplier => exaggeratedMultiplier;
		public float ExaggeratedMinUnity => exaggeratedMinUnity;

		private OpenSkyClient _client;
		private CancellationTokenSource _cts;

		private CesiumMapController _map;
		private Cesium3DTileset _tileset;
		private CesiumGeoreference _georef;

		private readonly Dictionary<string, TrackedAircraft> _tracked = new();

		private struct TrackedAircraft
		{
			public ADSB_AircraftNet aircraft;
			public float lastSeenLocalTime;
		}

		private void Awake()
		{
			_map = GetComponentInChildren<CesiumMapController>();
			_tileset = GetComponentInChildren<Cesium3DTileset>();
			_georef = GetComponentInChildren<CesiumGeoreference>();

			_client = new OpenSkyClient(credentials.openSkyClientId, credentials.openSkyClientSecret);

			_cts = new CancellationTokenSource();
			StartCoroutine(PollLoop());
		}

		private void OnDestroy()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;
		}

		public void SetExaggeratedScale(bool exaggerated)
		{
			ExaggeratedScale = exaggerated;
		}

		public void ToggleOnOff(bool onOff)
		{
			ApplyVisibilityToAllTracked(_map.adsbEnabled);

			//if (pausePollingWhenHidden)
			//{
			//	if (Object.HasStateAuthority)
			//		Debug.Log($"[ADSB] Polling {(adsbEnabled ? "ENABLED" : "DISABLED")} by toggle.");
			//}

			//Debug.Log($"[ADSB] Visualization {(adsbEnabled ? "ON" : "OFF")}.");
		}

		private void ApplyVisibilityToAllTracked(bool visible)
		{
			foreach (var kvp in _tracked)
			{
				var aircraft = kvp.Value.aircraft;
				if (aircraft != null)
				{
					SetAircraftVisuals(aircraft, visible);
				}
			}
		}

		private static void SetAircraftVisuals(ADSB_AircraftNet aircraft, bool visible)
		{
			var renderers = aircraft.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < renderers.Length; i++)
				renderers[i].enabled = visible;

			var lines = aircraft.GetComponentsInChildren<LineRenderer>(true);
			for (int i = 0; i < lines.Length; i++)
				lines[i].enabled = visible;

			var canvases = aircraft.GetComponentsInChildren<Canvas>(true);
			for (int i = 0; i < canvases.Length; i++)
				canvases[i].enabled = visible;

			var uiGraphics = aircraft.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
			for (int i = 0; i < uiGraphics.Length; i++)
				uiGraphics[i].enabled = visible;
		}

		private System.Collections.IEnumerator PollLoop()
		{
			yield return new WaitForSeconds(0.5f);

			while (_cts != null && !_cts.IsCancellationRequested)
			{
				try
				{
					if (_map != null && _georef != null)
						TickFetchAndApply();
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[ADSB] Poll tick error: {e.Message}");
				}

				yield return new WaitForSeconds(pollIntervalSeconds);
			}
		}

		private async void TickFetchAndApply()
		{
			if (pausePollingWhenHidden && !_map.adsbEnabled)
				return;

			ComputeBoundingBox(out double lamin, out double lamax, out double lomin, out double lomax);

			var resp = await _client.GetStatesAsync(lamin, lamax, lomin, lomax, _cts.Token);

			float now = Time.time;
			int processed = 0;

			foreach (var sv in resp.states)
			{
				if (processed >= maxAircraft)
					break;
				if (!sv.latitude.HasValue || !sv.longitude.HasValue)
					continue;

				string key = sv.icao24;
				if (string.IsNullOrEmpty(key))
					continue;

				double lat = sv.latitude.Value;
				double lon = sv.longitude.Value;

				// geometric altitude above WGS84 ellipsoid matches Cesium LLH height
				double altM = (float)(sv.geoAltitudeM ?? sv.baroAltitudeM ?? 0);

				if (!_tracked.TryGetValue(key, out var t) || t.aircraft.gameObject == null)
				{
					ADSB_AircraftNet spawned = Instantiate(aircraftPrefab, Vector3.zero, Quaternion.identity, _georef.transform);
					SetAircraftVisuals(spawned, _map.adsbEnabled);
					t = new TrackedAircraft { aircraft = spawned, lastSeenLocalTime = now };
					_tracked[key] = t;

					var net = spawned.GetComponent<ADSB_AircraftNet>();

					net.SetIcao24(key);
					_ = SeedInitialTrackAsync(key, net);
				}

				t.lastSeenLocalTime = now;
				_tracked[key] = t;

				var aircraftNet = t.aircraft;
				TryApplyHexDbMetadata(key, aircraftNet);

				float headingDeg = (float)(sv.trueTrackDeg ?? 0.0);
				float speedMS = (float)(sv.velocityMS ?? 0.0);

				int category = sv.category ?? -1;
				//if (processed < 5)
				//	Debug.Log($"[ADSB] sample {key} callsign={sv.callsign} cat={sv.category}");

				aircraftNet.DoOnAwake(
					delegate
					{
						aircraftNet.SetCategory(category);
						aircraftNet.SetKinematics(lat, lon, altM, headingDeg, speedMS);
						aircraftNet.SetCallsign(sv.callsign ?? string.Empty);
					}
				);

				processed++;
			}
			//Debug.Log($"[ADSB] fetched states={resp.states.Count}");
			//Debug.Log($"[ADSB] processed={processed} tracked={_tracked.Count}");

			// Despawn stale
			var toRemove = new List<string>();
			foreach (var kvp in _tracked)
			{
				if (kvp.Value.aircraft == null)
				{
					toRemove.Add(kvp.Key);
					continue;
				}
				if (now - kvp.Value.lastSeenLocalTime > staleTimeoutSeconds)
				{
					Destroy(kvp.Value.aircraft.gameObject);
					toRemove.Add(kvp.Key);
				}
			}
			foreach (var k in toRemove)
				RemoveAircraft(k);
		}

		public void RemoveAircraft(string key)
		{
			_tracked.Remove(key);
		}

		// attempt at finding terrain height to offset points
		//public async Task<double> GetAltitudeOffsetAt(double lon, double lat)
		//{
		//	double3 llh = new double3(lon, lat, 0);
		//	CesiumSampleHeightResult result = await _tileset.SampleHeightMostDetailed(llh);
		//	return result.longitudeLatitudeHeightPositions[0].z;
		//}

		private async System.Threading.Tasks.Task SeedInitialTrackAsync(string icao24, ADSB_AircraftNet net)
		{
			if (!seedTracksFromOpenSky)
				return;
			if (net == null)
				return;

			try
			{
				// time=0 requests "current/ongoing flight track" behavior in OpenSky client
				var track = await _client.GetTrackAsync(icao24, 0, _cts.Token);
				if (track == null || track.path == null || track.path.Count < 2)
					return;

				// Keep only last N points
				int start = Math.Max(0, track.path.Count - initialTrackSeedPoints);
				var sliced = track.path.GetRange(start, track.path.Count - start);

				net.SeedTrailFromTrackQuantized(sliced);
			}
			catch (OpenSkyRateLimitException)
			{
				// ignore – don’t spam on spawn
			}
			catch
			{
				// ignore – keep live trail only
			}
		}

		public void ComputeBoundingBox(out double lamin, out double lamax, out double lomin, out double lomax)
		{
			double latDeg = _map.latitude;
			double lonDeg = _map.longitude;

			double radiusUnity = Math.Max(1.0, _map.range);
			double scale = Math.Max(1e-8, _georef.scale);
			double radiusM = radiusUnity / scale;

			const double EarthRadiusM = 6371000.0;

			double latRad = latDeg * Math.PI / 180.0;
			double dLat = (radiusM / EarthRadiusM) * (180.0 / Math.PI);
			double dLon = dLat / Math.Max(0.1, Math.Cos(latRad));

			lamin = Clamp(latDeg - dLat, -90, 90);
			lamax = Clamp(latDeg + dLat, -90, 90);
			lomin = Clamp(lonDeg - dLon, -180, 180);
			lomax = Clamp(lonDeg + dLon, -180, 180);
		}

		public bool IsInBounds(double longitude, double latitude)
		{
			ComputeBoundingBox(out double lamin, out double lamax, out double lomin, out double lomax);
			return longitude > lomin && longitude < lomax && latitude > lamin && latitude < lamax;
		}

		private void TryApplyHexDbMetadata(string icao24, ADSB_AircraftNet net)
		{
			if (net == null)
				return;

			if (_hexdbCache.TryGetValue(icao24, out var info))
			{
				net.SetMetadata(info.registration, info.icaoTypeCode, info.manufacturer, info.owner, info.typeModel);
				return;
			}

			if (_hexdbInFlight.Contains(icao24))
				return;

			_hexdbInFlight.Add(icao24);
			_ = FetchHexDbAsync(icao24, net);
		}

		private async System.Threading.Tasks.Task FetchHexDbAsync(string icao24, ADSB_AircraftNet net)
		{
			try
			{
				// HexDB expects uppercase hex, but generally case-insensitive.
				string hex = (icao24 ?? "").Trim().ToUpperInvariant();
				string url = $"https://hexdb.io/api/v1/aircraft/{hex}"; // :contentReference[oaicite:5]{index=5}

				using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
				req.timeout = 15;

				var op = req.SendWebRequest();
				while (!op.isDone)
					await System.Threading.Tasks.Task.Yield();

				if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
					throw new Exception(req.error);

				var j = Newtonsoft.Json.Linq.JObject.Parse(req.downloadHandler.text);

				var info = new HexDbAircraftInfo
				{
					registration = j["Registration"]?.Value<string>() ?? "",
					icaoTypeCode = j["ICAOTypeCode"]?.Value<string>() ?? "",
					manufacturer = j["Manufacturer"]?.Value<string>() ?? "",
					owner = j["RegisteredOwners"]?.Value<string>() ?? "",
					typeModel = j["Type"]?.Value<string>() ?? "",
				};

				_hexdbCache[icao24] = info;

				// Only set if still valid
				if (net != null)
					net.SetMetadata(info.registration, info.icaoTypeCode, info.manufacturer, info.owner, info.typeModel);
			}
			catch
			{
				// Cache a negative result to avoid repeated calls
				_hexdbCache[icao24] = new HexDbAircraftInfo();
			}
			finally
			{
				_hexdbInFlight.Remove(icao24);
			}
		}

		private static double Clamp(double v, double a, double b) => Math.Max(a, Math.Min(b, v));

		public void UpdateAircraftPoses()
		{
			if (_map.adsbEnabled)
			{
				foreach (TrackedAircraft trackedAircraft in _tracked.Values)
				{
					trackedAircraft.aircraft.ApplyPoseAndVisuals();
				}
			}
		}
	}
}
