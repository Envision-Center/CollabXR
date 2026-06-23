using System;
using System.Collections.Generic;
using System.Linq;
using CollabXR.Objects;
using CollabXR.UI;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Geolocation
{
	public class CesiumMapContext : CollabContext
	{
		[System.Serializable]
		public struct CesiumMapLocation
		{
			public string locationName;
			public double latitude;
			public double longitude;
		}

		public List<CesiumMapLocation> locations;
		public TMP_Dropdown locationsDropdown;
		public TextMeshProUGUI zoomText,
			exaggerationText,
			rangeText;
		public TextMeshProUGUI currentCoordsText;
		public TMP_InputField latitudeInput,
			longitudeInput;
		public Slider zoom,
			exaggeration,
			range;
		public TMP_Text statusText;

		private CesiumMapController mapController;
		private CollabXR.ADSB.ADSB_AircraftManager adsbManager;

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);
			mapController = context.GetComponentInChildren<CesiumMapController>();
			adsbManager = context.GetComponentInChildren<CollabXR.ADSB.ADSB_AircraftManager>();
			AdjustState();
		}

		public void SetCoordinates(double latitude, double longitude)
		{
			mapController.latitude = latitude;
			mapController.longitude = longitude;
			AdjustText();
		}

		public void SetLocation()
		{
			CesiumMapLocation location = locations[locationsDropdown.value];

			SetCoordinates(location.latitude, location.longitude);
		}

		public void SetAircraftScaleMode(bool exaggerated)
		{
			adsbManager.SetExaggeratedScale(exaggerated);
		}

		public void ToggleAircraftVisualization(bool onOff)
		{
			mapController.adsbEnabled = onOff;
			mapController.UpdateAircraftVisualization();
		}

		public void SetTextCoordinates()
		{
			double latitudeDouble;
			double longitudeDouble;

			bool latitudeSucess = Double.TryParse(latitudeInput.text, out latitudeDouble);
			bool longitudeSucess = Double.TryParse(longitudeInput.text, out longitudeDouble);

			if (latitudeSucess && longitudeSucess)
			{
				if (90 - math.abs(latitudeDouble) >= 0 && 180 - math.abs(longitudeDouble) >= 0)
				{
					SetCoordinates(latitudeDouble, longitudeDouble);
					statusText.text = "";
					return;
				}
			}

			if (!latitudeSucess || 90 - math.abs(latitudeDouble) < 0)
				statusText.text = "Invalid latitude";
			if (!longitudeSucess || 180 - math.abs(longitudeDouble) < 0)
				statusText.text = "Invalid longitude";
		}

		public void IncrementLat(float increment)
		{
			double latitudeDouble;

			bool latitudeSucess = Double.TryParse(latitudeInput.text, out latitudeDouble);

			if (latitudeSucess && 90 - math.abs(latitudeDouble + increment) >= 0)
			{
				latitudeInput.text = (latitudeDouble + increment).ToString();
				SetTextCoordinates();
				statusText.text = "";
			}
			else
			{
				statusText.text = "Invalid latitude";
			}
		}

		public void IncrementLong(float increment)
		{
			double longitudeDouble;

			bool longitudeSucess = Double.TryParse(longitudeInput.text, out longitudeDouble);

			if (longitudeSucess && 180 - math.abs(longitudeDouble + increment) >= 0)
			{
				longitudeInput.text = (longitudeDouble + increment).ToString();
				SetTextCoordinates();
				statusText.text = "";
			}
			else
			{
				statusText.text = "Invalid longitude";
			}
		}

		public void Zoom()
		{
			mapController.zoom = zoom.value / 100;
			AdjustText();
		}

		public void AdjustExaggeration()
		{
			mapController.exaggeration = exaggeration.value;
			AdjustText();
		}

		public void AdjustRange()
		{
			mapController.range = range.value;
			AdjustText();
		}

		public void AdjustCompass(bool enabled)
		{
			mapController.displayRing = enabled;
		}

		public void AdjustState()
		{
			List<string> locationNames = locations.Select(p => p.locationName).ToList();
			locationsDropdown.AddOptions(locationNames);
			AdjustText();
		}

		private void AdjustText()
		{
			zoomText.text = $"{Math.Round(mapController.zoom * 100).ToString():F4}%";
			exaggerationText.text = $"{mapController.exaggeration:F2}x";
			rangeText.text = $"{mapController.range:F2}m";
			currentCoordsText.text = $"latitude: {mapController.latitude:F4} longitude: {mapController.longitude:F4}";
			latitudeInput.text = $"{mapController.latitude:F4}";
			longitudeInput.text = $"{mapController.longitude:F4}";
		}
	}
}
