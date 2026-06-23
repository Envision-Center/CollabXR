using CollabXR.Objects;
using CollabXR.UI;
using GLTFast.Schema;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Data
{
	public class StarCatalogContext : CollabContext
	{
		public Slider magnitudeSlider,
			latitudeSlider,
			longitudeSlider,
			speedSlider;
		public Toggle constellationToggle;
		public TextMeshProUGUI magnitudeText,
			latitudeText,
			longitudeText,
			speedText;
		public TextMeshProUGUI currentTimeText;
		private StarCatalogController controller;
		float lastRotationAdjustment;

		protected override void Update()
		{
			base.Update();
			if (controller != null)
			{
				AdjustText();
				bool simulationNeedsReenabling = controller.showSimulation != true && Time.time - lastRotationAdjustment > 0.5;
				if (controller.HasStateAuthority && simulationNeedsReenabling)
				{
					ShowSimulation(true);
				}
			}
		}

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);
			controller = context.GetComponentInChildren<StarCatalogController>();
			AdjustText();
			AdjustSliders();
		}

		public void ShowConstellations(bool enabled)
		{
			controller.showConstellations = enabled;
		}

		public void ShowSimulation(bool enabled)
		{
			controller.showSimulation = enabled;
			lastRotationAdjustment = Time.time;
		}

		public void AdjustMagnitude(float magnitude)
		{
			controller.dimmestMagnitude = magnitude;
			AdjustText();
		}

		public void AdjustLongitude(float longitude)
		{
			controller.longitude = longitude;
			AdjustText();
			ShowSimulation(false);
		}

		public void AdjustLatitude(float latitude)
		{
			controller.latitude = latitude;
			AdjustText();
			ShowSimulation(false);
		}

		public void AdjustSiderealSpeed(float speed)
		{
			controller.siderealSpeedMultiplier = speed;
			AdjustText();
		}

		public void AdjustText()
		{
			currentTimeText.text = controller.GetTimeString();
			magnitudeText.text = $"{controller.dimmestMagnitude:F2}";
			latitudeText.text = $"{controller.latitude:F2}";
			longitudeText.text = $"{controller.longitude:F2}";
			speedText.text = $"{controller.siderealSpeedMultiplier:F1}";
		}

		public void AdjustSliders()
		{
			magnitudeSlider.value = controller.dimmestMagnitude;
			latitudeSlider.value = controller.latitude;
			longitudeSlider.value = controller.longitude;
			speedSlider.value = controller.siderealSpeedMultiplier;
			constellationToggle.isOn = controller.showConstellations;
		}
	}
}
