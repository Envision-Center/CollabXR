using UnityEngine;

namespace CollabXR.UI
{
	public class BillboardCanvas : MonoBehaviour
	{
		private Canvas canvas;
		private Transform camTransform;

		void OnEnable()
		{
			canvas = GetComponent<Canvas>();
			Camera cam = Camera.main;
			if (cam != null)
				camTransform = cam.transform;
		}

		// Update is called once per frame
		void Update()
		{
			Vector3 targetLook = new(camTransform.position.x, canvas.transform.position.y, camTransform.position.z);
			transform.rotation = Quaternion.LookRotation(transform.position - targetLook);
		}
	}
}
