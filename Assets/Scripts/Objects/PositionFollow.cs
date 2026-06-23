using UnityEngine;

namespace CollabXR.Objects
{
	[DefaultExecutionOrder(10)]
	public class PositionFollow : MonoBehaviour, IFollower
	{
		public Transform target;

		public bool shouldLerp;
		public float translationLerp = 1.0f;

		public Vector3 globalOffset;

		private void Awake()
		{
			if (target != null)
			{
				SetTarget(target);
			}
		}

		private void LateUpdate()
		{
			Vector3 targetPos = target.position + globalOffset;

			if (shouldLerp)
			{
				transform.position = Vector3.Lerp(transform.position, targetPos, translationLerp * Time.deltaTime);
			}
			else
			{
				transform.position = targetPos;
			}
		}

		private void OnEnable()
		{
			if (target == null)
				enabled = false;
		}

		public void SetTarget(Transform t)
		{
			target = t;
			enabled = t != null;
		}

		public void SetTarget(Component m)
		{
			SetTarget(m.transform);
		}
	}
}
