using UnityEngine;

namespace CollabXR.ADSB
{
	public class BillboardToCamera : MonoBehaviour
	{
		[SerializeField]
		private Camera targetCamera;

		private void LateUpdate()
		{
			if (targetCamera == null)
				targetCamera = Camera.main;
			if (targetCamera == null)
				return;

			transform.forward = (transform.position - targetCamera.transform.position).normalized;
		}
	}
}
