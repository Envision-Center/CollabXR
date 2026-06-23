using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR
{
	public class HandTrackingVisibility : MonoBehaviour
	{
		public List<GameObject> DisableDuringHandTracking;
		public List<GameObject> EnableDuringHandTracking;

		void OnEnable()
		{
			if (HardwareConfig.IsVisionOS) // to do: extend this to general hand tracking detection
			{
				foreach (GameObject obj in DisableDuringHandTracking)
				{
					obj.SetActive(false);
				}
				foreach (GameObject obj in EnableDuringHandTracking)
				{
					obj.SetActive(true);
				}
			}
		}
	}
}
