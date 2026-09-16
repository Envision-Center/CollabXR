using UnityEngine;

namespace CollabXR.Objects
{
	public interface IFollower
	{
		public void SetTarget(Transform t);
		public void SetTarget(Component m);
	}

	[DefaultExecutionOrder(10)]
	public class TransformFollow : MonoBehaviour, IFollower
	{
		public Transform target;

		public float translationLerp = 1.0f;
		public float rotationLerp = 1.0f;

		public Pose localOffset;
		public bool offsetOnSetTarget;

		private void Awake()
		{
			if (target != null)
			{
				SetTarget(target);
			}
		}

		private void LateUpdate()
		{
			Quaternion targetRot = target.rotation * localOffset.rotation;
			transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationLerp);
			Vector3 targetPos = transform.TransformPoint(transform.InverseTransformPoint(target.position) - localOffset.position);
			transform.position = Vector3.Lerp(transform.position, targetPos, translationLerp);
		}

		private void OnEnable()
		{
			if (target == null)
				enabled = false;
		}

		public void SetTarget(Transform t)
		{
			Debug.Log("set target " + t);
			target = t;
			enabled = t != null;

			if (offsetOnSetTarget)
			{
				localOffset.position = transform.InverseTransformPoint(target.position);
				localOffset.rotation = Quaternion.Inverse(target.rotation) * transform.rotation;
			}
			else
			{
				localOffset.position = Vector3.zero;
				localOffset.rotation = Quaternion.identity;
			}
		}

		public void SetTarget(Component m)
		{
			SetTarget(m.transform);
		}
	}
}
