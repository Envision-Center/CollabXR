using System.Collections.Generic;
using CesiumForUnity;
using CollabXR.ADSB;
using Fusion;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollabXR.Geolocation
{
	public class CesiumMapController : NetworkBehaviour
	{
		private CesiumGeoreference georeference;
		private Cesium3DTileset tileset;
		private BoxCollider boxCollider;

		[Networked, OnChangedRender(nameof(Zoom))]
		public double zoom { get; set; } = 0.0001;

		[Networked, OnChangedRender(nameof(Zoom))]
		public float exaggeration { get; set; } = 1.0f;

		[Networked, OnChangedRender(nameof(Zoom))]
		public float range { get; set; } = 1.0f;

		[Networked, OnChangedRender(nameof(SampleHeight))]
		public double longitude { get; set; } = -86.910523;

		[Networked, OnChangedRender(nameof(SampleHeight))]
		public double latitude { get; set; } = 40.42413;

		[Networked, OnChangedRender(nameof(UpdateCompass))]
		public bool displayRing { get; set; } = true;
		public double maximumScale = 0.1;
		public double minimumScale = 0.0001;
		public Material cesiumClippingMaterial;
		public Transform meshParent;
		public List<Transform> samplePoints;
		bool needsRefresh = false;
		double lowestHeight;

		[Networked, OnChangedRender(nameof(UpdateAircraftVisualization))]
		public bool adsbEnabled { get; set; } = false;

		private ADSB_AircraftManager adsb;

		public override void Spawned()
		{
			base.Spawned();

			georeference = GetComponentInChildren<CesiumGeoreference>();
			tileset = GetComponentInChildren<Cesium3DTileset>();
			boxCollider = GetComponentInChildren<BoxCollider>();
			cesiumClippingMaterial = new Material(cesiumClippingMaterial);
			adsb = GetComponent<ADSB_AircraftManager>();

			SampleHeight();
			Zoom();
		}

		public void SampleHeight()
		{
			// temporary zoom handling for moon and mars
			//if (georeference.transform.parent.name.Contains("Moon") || georeference.transform.parent.name.Contains("Mars"))
			//{
			//    georeference.SetOriginLongitudeLatitudeHeight(longitude, latitude, 0);
			//    return;
			//}

			double3[] longlats = new double3[samplePoints.Count];

			georeference.SetOriginLongitudeLatitudeHeight(longitude, latitude, georeference.height);

			for (int i = 0; i < samplePoints.Count; ++i)
			{
				Transform point = samplePoints[i];
				Vector3 pos = transform.InverseTransformPoint(point.position);
				double3 ecef = georeference.TransformUnityPositionToEarthCenteredEarthFixed(new double3(pos.x, pos.y, pos.z));
				longlats[i] = georeference.ellipsoid.CenteredFixedToLongitudeLatitudeHeight(ecef);
			}

			tileset
				.SampleHeightMostDetailed(longlats)
				.ContinueWith(task =>
				{
					if (task.Exception != null)
					{
						Debug.LogError("Error in SampleHeightMostDetailed: " + task.Exception.Message);
					}
					else
					{
						CesiumSampleHeightResult result = task.Result;
						lowestHeight = math.INFINITY;
						for (int i = 0; i < result.longitudeLatitudeHeightPositions.Length; ++i)
						{
							double3 position = result.longitudeLatitudeHeightPositions[i];
							lowestHeight = System.Math.Min(lowestHeight, position[2]);
							Debug.Log($"({position[0]},{position[1]}) : {position[2]}");
						}
						needsRefresh = true;
						//Debug.Log(CesiumWgs84Ellipsoid.GetRadii());
						//Debug.Log(georeference.ellipsoid.GetRadii());
						//double ellipsoidDelta = CesiumWgs84Ellipsoid.GetRadii()[0] - georeference.ellipsoid.GetRadii()[0];
						//Debug.Log($"lowestHeight = {lowestHeight} + {ellipsoidDelta}");
						//lowestHeight += ellipsoidDelta;
					}
				});
			adsb.UpdateAircraftPoses();
		}

		private void Update()
		{
			Shader.SetGlobalVector("_cesiumPosition", transform.position);
			if (needsRefresh)
			{
				georeference.SetOriginLongitudeLatitudeHeight(longitude, latitude, lowestHeight);
				needsRefresh = false;
			}
		}

		public void Zoom()
		{
			georeference.scale = math.lerp(minimumScale, maximumScale, zoom);
			tileset.transform.localScale = new Vector3(1, exaggeration, 1);
			cesiumClippingMaterial.SetFloat("_clippingDistance", range);
			tileset.opaqueMaterial = cesiumClippingMaterial;
			boxCollider.size = new Vector3(2 * range, 100, 2 * range);
			UpdateCompass();

			SampleHeight(); // also updates the height to deal with the change in scale
			adsb.UpdateAircraftPoses();
		}

		public void UpdateCompass()
		{
			meshParent.localScale = new Vector3(range, 1, range);
			meshParent.gameObject.SetActive(displayRing);
		}

		public void UpdateAircraftVisualization()
		{
			adsb.ToggleOnOff(adsbEnabled);
		}
	}
}
