using CollabXR.VR;
using UnityEngine;

namespace CollabXR.UI
{
	public class MenuPositioner : MonoBehaviour
	{
		private void OnEnable()
		{
			Reposition();
		}

		public void Reposition()
		{
			transform.position = HardwareRig.Instance.actualHead.position;

			Vector3 flatForward = HardwareRig.Instance.actualHead.forward;
			flatForward.y = 0;
			flatForward.Normalize();

			transform.forward = flatForward;
		}
	}
}
