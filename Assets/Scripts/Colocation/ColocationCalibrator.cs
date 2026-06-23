using CollabXR.Oculus;
using UnityEngine;

namespace CollabXR.Colocation
{
	public class ColocationCalibrator : MonoBehaviour
	{
		private GameObject anchorTarget;

		public Vector3 anchorPos;

		[SerializeField]
		private GameObject anchorVisualPrefab;

		public void CreateAnchorTarget()
		{
			anchorTarget = Instantiate(anchorVisualPrefab);
		}

		public void SetAnchorPos(Vector3 pos)
		{
			anchorPos = pos;

			if (anchorTarget == null)
				return;

			Vector3 flatForward = transform.forward;
			flatForward.y = 0;
			flatForward.Normalize();

			Quaternion rot = Quaternion.LookRotation(flatForward, Vector3.up);

			anchorTarget.transform.SetPositionAndRotation(anchorPos, rot);
		}

		private void DestroyAnchorTarget()
		{
			Destroy(anchorTarget);
		}

		public void Anchor() => Anchor(anchorTarget.transform.position, anchorTarget.transform.rotation);

		public void Anchor(Vector3 pos, Quaternion rot)
		{
			ColocationDriver d = ColocationDriver.Instance;
			d.Anchor(pos, rot, () => d.UploadCurrentAnchor());
			DestroyAnchorTarget();
		}

		private void OnDisable()
		{
			DestroyAnchorTarget();
		}
	}
}
