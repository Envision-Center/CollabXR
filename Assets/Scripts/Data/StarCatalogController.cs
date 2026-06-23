using CollabXR.Environments;
using Fusion;
using Photon.Realtime;
using UnityEngine;

namespace CollabXR.Data
{
	public class StarCatalogController : NetworkBehaviour
	{
		StarCatalog catalog;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public float dimmestMagnitude { get; set; } = 1.0f;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public float latitude { get; set; } = 40.0f;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public float longitude { get; set; } = 0.0f;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public float timeOffset { get; set; } = 0.0f;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public float siderealSpeedMultiplier { get; set; } = 1.0f;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public bool showSimulation { get; set; } = true;

		[Networked, OnChangedRender(nameof(UpdateCatalog))]
		public bool showConstellations { get; set; } = true;

		private float siderealSpeed = (360.0f) / (1436 * 60);

		private void FindCatalogInstance()
		{
			catalog = FindFirstObjectByType<StarCatalog>();
			UpdateCatalog();
		}

		public override void Spawned()
		{
			base.Spawned();
			FindCatalogInstance();
			EnvironmentManager.Instance.OnEnvironmentLoadComplete.AddListener(FindCatalogInstance);
		}

		public void Update()
		{
			if (HasStateAuthority && catalog != null)
			{
				//to do: uncomment when some motion sickness prevention is in place
				//timeOffset += siderealSpeed * siderealSpeedMultiplier * Time.deltaTime;
				UpdateCatalog();
			}
		}

		public void UpdateCatalog()
		{
			if (catalog != null)
			{
				catalog.SetMagnitude(dimmestMagnitude);
				catalog.SetDisplay(showSimulation, showConstellations);
				catalog.SetSiderealValues(latitude, longitude, timeOffset);
			}
		}

		public string GetTimeString()
		{
			return timeOffset.ToString();
		}
	}
}
