using UnityEngine;

namespace CollabXR.ADSB
{
	public class PropellerSpinner : MonoBehaviour
	{
		[SerializeField]
		private ADSB_AircraftNet aircraft;

		[SerializeField]
		private float degreesPerSecond = 1800f;

		[SerializeField]
		private Vector3 localAxis = new Vector3(0, 0, 1);

		private void Awake()
		{
			if (aircraft == null)
				aircraft = GetComponentInParent<ADSB_AircraftNet>();
		}

		private void Update()
		{
			if (aircraft == null)
				return;

			// GA only
			//if (aircraft.CurrentVisualType != ADSB_AircraftNet.AircraftVisualType.GeneralAviation)
			//return;

			transform.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);
		}
	}
}
