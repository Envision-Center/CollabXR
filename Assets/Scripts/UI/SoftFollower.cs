using SaintsField.Playa;
using UnityEngine;

namespace CollabXR.UI
{
	public class SoftFollower : MonoBehaviour
	{
		public Transform FollowTarget;

		public bool FixHeight = true;
		public float Height;

		public bool UseRotation = true;

		[LayoutShowIf("UseRotation")]
		[LayoutStart("Rotation", ELayout.FoldoutBox)]
		public bool KeepUpright = true;

		public float RotationDeadzone = 30f;
		public float RotateDuration = 1f;
		public float RotateDelay = 1f;
		public AnimationCurve RotationCurve;

		[LayoutEnd]
		public bool UsePosition = true;

		[LayoutShowIf("UsePosition")]
		[LayoutStart("Position", ELayout.FoldoutBox)]
		public float PositionDeadzone = 2f;
		public float TranslateDuration = 0.5f;
		public float TranslateDelay = 1f;
		public AnimationCurve TranslationCurve;

		[LayoutEnd]
		private bool isRotating;

		private bool isTranslating;

		private Vector3 previousTargetPosition = Vector3.zero;
		private float rotateTime;
		private Vector3 startingPosition;
		private Quaternion startingRotation;
		private float translateTime;

		private void Update()
		{
			if (UseRotation)
			{
				Quaternion targetRotation = GetTargetRotation();

				if (isRotating)
				{
					rotateTime += Time.deltaTime;
					float progress = rotateTime / RotateDuration;
					float lerp = RotationCurve.Evaluate(progress);
					transform.rotation = Quaternion.Slerp(startingRotation, targetRotation, lerp);
					if (progress >= 1f)
						isRotating = false;
				}
				else
				{
					if (Quaternion.Angle(transform.rotation, targetRotation) > RotationDeadzone)
					{
						isRotating = true;
						rotateTime = 0f;
						startingRotation = transform.rotation;

						isTranslating = true;
						translateTime = 0f;
						startingPosition = transform.position;
					}
				}
			}

			if (UsePosition)
			{
				Vector3 targetPosition = GetTargetPosition();
				Vector3 targetDelta = targetPosition - previousTargetPosition;

				if (isTranslating)
				{
					translateTime += Time.deltaTime;
					float progress = translateTime / TranslateDuration;
					float lerp = TranslationCurve.Evaluate(progress);
					transform.position = Vector3.Lerp(startingPosition, targetPosition, lerp);
					if (progress >= 1f)
						isTranslating = false;
				}
				else
				{
					if ((transform.position - targetPosition).sqrMagnitude > PositionDeadzone * PositionDeadzone)
					{
						if (targetDelta.sqrMagnitude > PositionDeadzone * PositionDeadzone)
						{
							transform.position = targetPosition;
						}
						else
						{
							isTranslating = true;
							translateTime = 0f;
							startingPosition = transform.position;
						}
					}
				}

				previousTargetPosition = targetPosition;
			}
		}

		private void OnEnable()
		{
			if (FollowTarget)
				SnapToTarget();
			else
				enabled = false;
		}

		public void SnapToTarget()
		{
			transform.position = GetTargetPosition();
			previousTargetPosition = transform.position;

			transform.rotation = GetTargetRotation();
			startingRotation = transform.rotation;
		}

		public void FollowTransform(Transform t)
		{
			FollowTarget = t;
			enabled = true;
		}

		private Vector3 GetTargetPosition()
		{
			Vector3 targetPosition = FollowTarget.position;
			if (FixHeight)
				targetPosition.y = Height;

			return targetPosition;
		}

		private Quaternion GetTargetRotation()
		{
			Quaternion targetRotation = FollowTarget.rotation;

			if (KeepUpright)
			{
				Quaternion toUpright = Quaternion.FromToRotation(FollowTarget.up, Vector3.up);
				targetRotation = toUpright * targetRotation;
			}

			return targetRotation;
		}
	}
}
