using UnityEngine;

namespace CollabXR.ADSB
{
	[ExecuteInEditMode]
	public class BillboardToCamera : MonoBehaviour
	{
		[SerializeField]
		private Camera targetCamera;
		[SerializeField]
		private bool billboardInEditor = false;

		private void LateUpdate()
		{
			if (targetCamera == null)
			{
				targetCamera = Camera.main;
			}
			if (targetCamera == null)
			{
				return;
			}
			if(!billboardInEditor && Application.isEditor)
			{
				return;
			}

			transform.forward = (transform.position - targetCamera.transform.position).normalized;
		}
	}
}
