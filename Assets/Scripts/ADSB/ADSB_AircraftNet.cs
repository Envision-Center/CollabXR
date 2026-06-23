using System;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;

namespace CollabXR.ADSB
{
	public class ADSB_AircraftNet : MonoBehaviour
	{
		public enum AircraftVisualType : byte
		{
			GeneralAviation = 0,
			Airliner = 1,
			Military = 2,
		}

		[Header("Components")]
		[SerializeField]
		private CesiumGlobeAnchor globeAnchor;

		[SerializeField]
		private LineRenderer trail;

		[Header("Trail")]
		[SerializeField, Range(10, 300)]
		private int maxTrailPoints = 90;

		[Tooltip("Minimum movement (meters) before adding a new trail point.")]
		[SerializeField]
		private float minTrailStepMeters = 250f;

		[Header("Visual Meshes")]
		[SerializeField]
		private GameObject meshGA;

		[SerializeField]
		private GameObject meshAirliner;

		[SerializeField]
		private GameObject meshMilitary;

		[Header("Mesh Wingspan Calibration (Unity units at localScale=1)")]
		[SerializeField]
		private float gaWingspanUnity = 12.75f;

		[SerializeField]
		private float airlinerWingspanUnity = 41.44f;

		[SerializeField]
		private float militaryWingspanUnity = 15.15f;

		[Header("Representative Real Wingspans (meters)")]
		[SerializeField]
		private float gaWingspanMeters = 11f;

		[SerializeField]
		private float airlinerWingspanMeters = 60f;

		[SerializeField]
		private float militaryWingspanMeters = 14f;

		[Header("Trail Width (scaled by aircraft scale)")]
		[SerializeField]
		private float trailWidthScale = 0.6f; // was too small

		[SerializeField]
		private float trailMinWidth = 0.01f; // world-space minimum

		[SerializeField]
		private float trailMaxWidth = 0.2f;

		public string Icao24 { get; private set; }

		public string Callsign { get; private set; }

		public double Latitude { get; private set; }

		public double Longitude { get; private set; }

		public double AltitudeM { get; private set; }

		public float HeadingDeg { get; private set; }

		public float SpeedMS { get; private set; }
		public string Registration { get; private set; }
		public string IcaoTypeCode { get; private set; }
		public string Manufacturer { get; private set; }

		public string OwnerOperator { get; private set; }

		public string TypeModel { get; private set; }

		public int Category { get; private set; } = -1;

		private Action OnAwake;

		private CesiumGeoreference _georef;
		private ADSB_AircraftManager _manager;

		private readonly List<double3> _trailLLH = new();
		private double3? _lastTrailLLH;

		private bool _lastExaggerated;
		private double _lastGeorefScale;
		private bool _trailSeeded = false;

		private void Awake()
		{
			if (trail == null)
				trail = GetComponentInChildren<LineRenderer>(true);

			if (globeAnchor == null)
				globeAnchor = GetComponent<CesiumGlobeAnchor>();
			if (trail != null)
			{
				trail.positionCount = 0;
				trail.SetPositions(System.Array.Empty<Vector3>());
				trail.enabled = false;
			}
			_georef = GetComponentInParent<CesiumGeoreference>();
			_manager = GetComponentInParent<ADSB_AircraftManager>();
			_lastExaggerated = _manager != null && _manager.ExaggeratedScale;
			_lastGeorefScale = _georef != null ? _georef.scale : 0;

			OnAwake?.Invoke();
		}

		public void DoOnAwake(Action toDo)
		{
			if (didAwake)
			{
				toDo();
			}
			else
			{
				OnAwake += toDo;
			}
		}

		private void Start()
		{
			ApplyPoseAndVisuals();
			SeedTrailAtCurrentPose();
		}

		private void SeedTrailAtCurrentPose()
		{
			_trailLLH.Clear();
			_lastTrailLLH = null;

			// Only seed if plausible pose
			if (double.IsNaN(Longitude) || double.IsNaN(Latitude))
				return;

			var llh = new double3(Longitude, Latitude, AltitudeM);
			_trailLLH.Add(llh);
			_lastTrailLLH = llh;
			_trailSeeded = true;

			// Don’t render until we have at least 2 points
			if (trail != null)
				trail.positionCount = 0;
		}

		private void Update()
		{
			if (_manager == null || _georef == null)
				return;

			bool ex = _manager.ExaggeratedScale;
			double sc = _georef.scale;

			if (ex != _lastExaggerated || Math.Abs(sc - _lastGeorefScale) > 1e-12)
			{
				_lastExaggerated = ex;
				_lastGeorefScale = sc;

				ApplyVisualType();
				ApplyScale();
				ApplyTrailWidth();
			}
		}

		// ----- Authority-side setters -----
		public void SetIcao24(string hex)
		{
			Icao24 = (hex ?? string.Empty).Trim();
		}

		public void SetCallsign(string cs)
		{
			Callsign = (cs ?? string.Empty).Trim();
		}

		public void SetCategory(int category)
		{
			Category = category;
		}

		public void SetKinematics(double lat, double lon, double altM, float headingDeg, float speedMS)
		{
			Latitude = lat;
			Longitude = lon;
			AltitudeM = altM;
			HeadingDeg = headingDeg;
			SpeedMS = speedMS;
			ApplyPoseAndVisuals();
		}

		// ----- Render-side -----
		public void ApplyPoseAndVisuals()
		{
			ApplyPose();
			ApplyVisualType();
			ApplyTrailColor();
			ApplyScale();
			ApplyTrailWidth();
			PushTrailPointIfMoved();
			RebuildTrail();
		}

		private void ApplyPose()
		{
			if (globeAnchor == null)
				return;

			globeAnchor.longitudeLatitudeHeight = new double3(Longitude, Latitude, AltitudeM);
			transform.localRotation = Quaternion.Euler(0f, HeadingDeg, 0f);

			if (!_manager.IsInBounds(Longitude, Latitude))
			{
				_manager.RemoveAircraft(Icao24);
				Destroy(gameObject);
			}
		}

		private AircraftVisualType Classify(int category)
		{
			if (category >= 0)
			{
				// Airliner / large transport classes
				if (category == 4 || category == 5 || category == 6)
					return AircraftVisualType.Airliner;

				// Military-ish
				if (category == 7 || category == 14)
					return AircraftVisualType.Military;

				// Rotorcraft and everything else -> GA bucket
				return AircraftVisualType.GeneralAviation;
			}

			//Temporary Approximation
			// fall back to kinematics only
			// Airliners tend to be high altitude + high speed.
			if (AltitudeM > 6000f && SpeedMS > 170f)
				return AircraftVisualType.Airliner;

			if (SpeedMS > 250f)
				return AircraftVisualType.Military;

			return AircraftVisualType.GeneralAviation;
		}

		private void ApplyVisualType()
		{
			var type = Classify(Category);

			if (meshGA != null)
				meshGA.SetActive(type == AircraftVisualType.GeneralAviation);
			if (meshAirliner != null)
				meshAirliner.SetActive(type == AircraftVisualType.Airliner);
			if (meshMilitary != null)
				meshMilitary.SetActive(type == AircraftVisualType.Military);
		}

		private void ApplyScale()
		{
			if (_georef == null)
				return;

			var type = Classify(Category);

			float prefabWingspanUnity = type switch
			{
				AircraftVisualType.GeneralAviation => Mathf.Abs(gaWingspanUnity),
				AircraftVisualType.Airliner => Mathf.Abs(airlinerWingspanUnity),
				AircraftVisualType.Military => Mathf.Abs(militaryWingspanUnity),
				_ => Mathf.Abs(gaWingspanUnity),
			};

			float targetWingspanMeters = type switch
			{
				AircraftVisualType.GeneralAviation => gaWingspanMeters,
				AircraftVisualType.Airliner => airlinerWingspanMeters,
				AircraftVisualType.Military => militaryWingspanMeters,
				_ => gaWingspanMeters,
			};

			float desiredUnityWingspan = targetWingspanMeters * (float)_georef.scale;
			float realScale = (prefabWingspanUnity > 1e-6f) ? (desiredUnityWingspan / prefabWingspanUnity) : 1f;

			float realMin = (_manager != null) ? _manager.RealScaleVisibilityMin : 0.0025f;
			float realMax = (_manager != null) ? _manager.RealScaleVisibilityMax : 0.15f;
			realScale = Mathf.Clamp(realScale, realMin, realMax);

			bool exaggerated = (_manager != null) ? _manager.ExaggeratedScale : true;
			float finalScale = realScale;

			if (exaggerated)
			{
				float mult = (_manager != null) ? _manager.ExaggeratedMultiplier : 12f; // lower default
				float minU = (_manager != null) ? _manager.ExaggeratedMinUnity : 0.01f; // slightly higher
				finalScale = Mathf.Max(realScale * mult, minU);
			}
			else
				finalScale = realScale;

			transform.localScale = new Vector3(finalScale, finalScale, finalScale);
		}

		private void ApplyTrailWidth()
		{
			if (trail == null)
				return;

			float s = transform.localScale.x;
			float w = Mathf.Clamp(s * trailWidthScale, trailMinWidth, trailMaxWidth);

			trail.startWidth = w;
			trail.endWidth = w;
		}

		private void ApplyTrailColor()
		{
			if (trail == null)
				return;

			var type = Classify(Category);

			Color c = type switch
			{
				AircraftVisualType.Military => Color.green,
				AircraftVisualType.Airliner => Color.red,
				_ => Color.blue,
			};

			trail.startColor = c;
			trail.endColor = c;

			if (trail.material != null && trail.material.HasProperty("_BaseColor"))
				trail.material.SetColor("_BaseColor", c);
			else if (trail.material != null && trail.material.HasProperty("_Color"))
				trail.material.SetColor("_Color", c);
		}

		private void PushTrailPointIfMoved()
		{
			if (!_trailSeeded)
			{
				SeedTrailAtCurrentPose();
				return;
			}

			double3 llh = new double3(Longitude, Latitude, AltitudeM);

			if (_lastTrailLLH.HasValue)
			{
				// Approx distance using lat/lon
				double meters = HaversineMeters(_lastTrailLLH.Value.y, _lastTrailLLH.Value.x, llh.y, llh.x);
				if (meters < minTrailStepMeters)
					return;
			}

			_lastTrailLLH = llh;

			_trailLLH.Add(llh);
			if (_trailLLH.Count > maxTrailPoints)
				_trailLLH.RemoveAt(0);
		}

		private void RebuildTrail()
		{
			if (trail == null || _georef == null)
				return;

			trail.enabled = false;
			trail.positionCount = 0;

			for (int i = 0; i < _trailLLH.Count; i++)
			{
				double3 llh = _trailLLH[i];

				if (_manager.IsInBounds(llh.x, llh.y) || trail.positionCount > 0) // add positions only if this point or a previous point is in bounds
				{
					double3 ecef = _georef.ellipsoid.LongitudeLatitudeHeightToCenteredFixed(llh);
					double3 local = _georef.TransformEarthCenteredEarthFixedPositionToUnity(ecef);

					Vector3 world = _georef.transform.TransformPoint(new Vector3((float)local.x, (float)local.y, (float)local.z));

					Vector3 localToThisAircraft = transform.InverseTransformPoint(world);
					trail.positionCount++;
					trail.SetPosition(trail.positionCount - 1, localToThisAircraft);
				}
			}

			if (trail.positionCount > 1)
			{
				trail.enabled = true;
			}
		}

		public async void SeedTrailFromTrackQuantized(System.Collections.Generic.List<OpenSkyTrackResponse.TrackPoint> pts)
		{
			if (pts == null || pts.Count < 2)
				return;

			int n = pts.Count;

			var latE5 = new int[n];
			var lonE5 = new int[n];
			var altM = new short[n];

			for (int i = 0; i < n; i++)
			{
				latE5[i] = (int)System.Math.Round(pts[i].lat * 1e5);
				lonE5[i] = (int)System.Math.Round(pts[i].lon * 1e5);

				double a = pts[i].altM;

				if (double.IsNaN(a) || double.IsInfinity(a))
					a = 0;
				a = System.Math.Clamp(a, 0, 32000);
				altM[i] = (short)System.Math.Round(a);
			}

			SeedTrailQuantized(latE5, lonE5, altM);
		}

		private void SeedTrailQuantized(int[] latE5, int[] lonE5, short[] altM)
		{
			if (gameObject == null)
				return;
			if (trail == null)
				trail = GetComponentInChildren<LineRenderer>(true);
			if (latE5 == null || lonE5 == null || altM == null)
				return;

			int n = Mathf.Min(latE5.Length, Mathf.Min(lonE5.Length, altM.Length));
			if (n < 2)
				return;

			// Clear/disable before rebuilding to avoid origin segment
			if (trail != null)
			{
				trail.useWorldSpace = false;
				trail.positionCount = 0;
				trail.SetPositions(System.Array.Empty<Vector3>());
				trail.enabled = false;
			}

			_trailLLH.Clear();
			_lastTrailLLH = null;
			_trailSeeded = true;

			for (int i = 0; i < n; i++)
			{
				double lat = latE5[i] / 1e5;
				double lon = lonE5[i] / 1e5;
				double alt = altM[i];

				_trailLLH.Add(new Unity.Mathematics.double3(lon, lat, alt));
			}

			_lastTrailLLH = _trailLLH[_trailLLH.Count - 1];
			RebuildTrail();
		}

		private static double HaversineMeters(double lat1Deg, double lon1Deg, double lat2Deg, double lon2Deg)
		{
			const double R = 6371000.0;
			double dLat = (lat2Deg - lat1Deg) * Math.PI / 180.0;
			double dLon = (lon2Deg - lon1Deg) * Math.PI / 180.0;

			double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1Deg * Math.PI / 180.0) * Math.Cos(lat2Deg * Math.PI / 180.0) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return R * c;
		}

		public void SetMetadata(string reg, string typeCode, string manufacturer, string ownerOperator, string typeModel)
		{
			Registration = (reg ?? "").Trim();
			IcaoTypeCode = (typeCode ?? "").Trim();
			Manufacturer = (manufacturer ?? "").Trim();
			OwnerOperator = (ownerOperator ?? "").Trim();
			TypeModel = (typeModel ?? "").Trim();
		}
	}
}
