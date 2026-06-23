using TMPro;
using UnityEngine;

namespace CollabXR.ADSB
{
	public class ADSB_AircraftLabel : MonoBehaviour
	{
		[SerializeField]
		private ADSB_AircraftNet aircraft;

		[Header("TMP Refs")]
		[SerializeField]
		private TMP_Text icao24Text;

		[SerializeField]
		private TMP_Text callsignText;

		[SerializeField]
		private TMP_Text altitudeText;

		[SerializeField]
		private TMP_Text headingText;

		[SerializeField]
		private TMP_Text speedText;

		[SerializeField]
		private TMP_Text typeText;

		[Header("Update Rate")]
		[SerializeField, Range(1f, 30f)]
		private float hz = 8f;

		private float _next;

		private void Awake()
		{
			if (aircraft == null)
				aircraft = GetComponentInParent<ADSB_AircraftNet>();
		}

		private void Update()
		{
			if (aircraft == null)
				return;
			if (Time.time < _next)
				return;
			_next = Time.time + (1f / Mathf.Max(1f, hz));

			if (icao24Text != null)
				icao24Text.text = aircraft.Icao24.ToString();
			if (callsignText != null)
				callsignText.text = $"{aircraft.Callsign.ToString().Trim()}";
			if (altitudeText != null)
				altitudeText.text = $"{aircraft.AltitudeM:0} m";
			if (headingText != null)
				headingText.text = $"{aircraft.HeadingDeg:0}°";
			if (speedText != null)
				speedText.text = $"{aircraft.SpeedMS:0.0} m/s";

			if (typeText != null)
				typeText.text = $"{aircraft.TypeModel}";
		}
	}
}
