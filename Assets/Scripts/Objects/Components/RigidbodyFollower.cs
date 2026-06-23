using UnityEngine;

namespace CollabXR.Objects.Components
{
	public class RigidbodyFollower : MonoBehaviour, IFollower
	{
		public float velocityDamping = 1;
		public float angularVelocityDamping = 1;

		[SerializeField]
		private Transform target;

		private Pose offset;

		private new Rigidbody rigidbody;

		private void Awake()
		{
			rigidbody = GetComponent<Rigidbody>();
			rigidbody.maxAngularVelocity = float.MaxValue;
		}

		private void FixedUpdate()
		{
			// Do angular velocity tracking
			// Scale initialized velocity by prediction factor
			rigidbody.angularVelocity *= 1f - angularVelocityDamping;
			Quaternion rotationDelta = target.rotation * Quaternion.Inverse(transform.rotation * offset.rotation);
			rotationDelta.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);
			if (angleInDegrees > 180f)
				angleInDegrees -= 360f;

			if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
				rigidbody.angularVelocity += rotationAxis * (angleInDegrees * Mathf.Deg2Rad) / Time.fixedDeltaTime;

			// Do velocity tracking
			// Scale initialized velocity by prediction factor
			rigidbody.linearVelocity *= 1f - velocityDamping;
			Vector3 positionDelta = target.position - transform.TransformPoint(offset.position);
			rigidbody.linearVelocity += positionDelta / Time.fixedDeltaTime;
		}

		private void OnEnable()
		{
			if (target == null)
				enabled = false;
			else
				SetTarget(target);
		}

		public void SetTarget(Transform t)
		{
			enabled = t != null;

			target = t;

			offset.position = transform.InverseTransformPoint(t.position);
			offset.rotation = Quaternion.Inverse(transform.rotation) * target.rotation;
		}

		public void SetTarget(Component m)
		{
			SetTarget(m.transform);
		}
	}
}
