using CollabXR.Objects;
using UnityEngine;
using UnityEngine.VFX;

namespace CollabXR.Data
{
	public class StarCatalog : MonoBehaviour
	{
		public VisualEffect vfxGraph;
		public MeshRenderer cubemap;
		public Material cubemat;
		public GameObject labels;
		public float cubemapLatitudeOffset,
			cubemapLongitudeOffset;
		float dimmestMagnitude = 25.0f,
			longitude = 0.0f,
			latitude = 40.0f,
			timeOffset = 0.0f;
		bool showConstellations = true,
			showSimulation = true,
			showLabels = true;

		[SerializeField]
		bool debug = false;

		private void Start()
		{
			cubemat = new Material(cubemat);
			AdjustVisuals();
			GetComponent<TransformFollow>().SetTarget(Camera.main.transform);
		}

		public void SetMagnitude(float mag)
		{
			dimmestMagnitude = mag;
			AdjustVisuals();
		}

		public void SetDisplay(bool simulation, bool constellations, bool labels)
		{
			showSimulation = simulation;
			showConstellations = constellations;
			showLabels = labels;
			AdjustVisuals();
		}

		public void SetSiderealValues(float lat, float lon, float time)
		{
			latitude = lat;
			longitude = lon;
			timeOffset = time;
			AdjustVisuals();
		}

		public void AdjustVisuals()
		{
			vfxGraph.SetFloat("DimmestMagnitude", dimmestMagnitude);
			vfxGraph.SetBool("ShowConstellations", showConstellations && showSimulation);
			vfxGraph.SetBool("ShowPolarisMarker", !showSimulation || debug);
			vfxGraph.SetBool("ShowStars", showSimulation || debug);
			cubemap.enabled = showSimulation || debug;
			vfxGraph.transform.localEulerAngles = new Vector3(EffectiveLatitude(), 0, EffectiveLongitude());
			cubemat.SetFloat("_Latitude", CubemapLatitude());
			cubemat.SetFloat("_Longitude", CubemapLongitude());
			cubemap.material = cubemat;
			labels.SetActive(showLabels && showSimulation);
		}

		public float EffectiveLatitude() => latitude;

		public float EffectiveLongitude() => longitude + timeOffset;

		public float CubemapLatitude()
		{
			return cubemapLatitudeOffset - EffectiveLatitude();
		}

		public float CubemapLongitude()
		{
			return cubemapLongitudeOffset - EffectiveLongitude();
		}
	}
}
