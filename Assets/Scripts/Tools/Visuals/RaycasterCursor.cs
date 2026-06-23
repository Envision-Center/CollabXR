using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Tools.Visuals
{
	public class RaycasterCursor : MonoBehaviour
	{
		private Raycaster raycaster;

		private void Awake()
		{
			raycaster = GetComponentInParent<Raycaster>();
			raycaster.onHitPoint.AddListener(SetPos);
		}

		private void SetPos(Vector3 pos)
		{
			transform.position = pos;
		}
	}
}
