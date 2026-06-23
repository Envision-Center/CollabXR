using System;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	[DefaultExecutionOrder(50)]
	public class Mover : MonoBehaviour
	{
		public bool IsMoving { get; private set; } = false;
		public CollabObject CollabObjectToMove { get; private set; }
		private NetworkScalable scalable;

		private Vector3 movingObjectLocalPos;
		private Vector3 movingObjectEuler;

		public Vector2 AxisInput { get; set; }

		public float RotateSpeedMultiplier = 150f;
		public float ZSpeedMultiplier = 15f;
		public float ScaleSpeedMultiplier = 5f;
		public bool ScaleWithAxisInput { get; set; } = false;

		public UnityEvent<GameObject> OnSetObjectToMove;
		public UnityEvent<bool> OnCanMoveObject;
		public UnityEvent<bool> OnNotMovingObject;
		public UnityEvent OnStartMove;
		public UnityEvent OnStopMove;

		private Vector3 lastHandLocation;

		public void SetTargetObject(GameObject gameObject)
		{
			if (IsMoving)
				return;

			CollabObjectToMove = gameObject?.GetComponentInParent<CollabObject>();
			if (CollabObjectToMove != null && !CollabObjectToMove.IsIndepedentObject)
				CollabObjectToMove = null;
			scalable = CollabObjectToMove?.GetComponentInChildren<NetworkScalable>();
			OnSetObjectToMove.Invoke(CollabObjectToMove?.gameObject);
			OnCanMoveObject.Invoke(CollabObjectToMove != null);
		}

		public void StartStopMoving(bool start)
		{
			if (start)
			{
				StartMoving();
			}
			else
			{
				StopMoving();
			}
		}

		public void StartMoving()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanInteract)
			{
				return;
			}

			if (IsMoving || CollabObjectToMove == null)
				return;

			CollabObjectToMove.Object.RequestStateAuthority();

			movingObjectLocalPos = transform.InverseTransformPoint(CollabObjectToMove.transform.position);
			movingObjectEuler = CollabObjectToMove.transform.eulerAngles;
			lastHandLocation = transform.position;
			IsMoving = true;
			OnNotMovingObject.Invoke(false);
			OnStartMove.Invoke();
		}

		private void LateUpdate()
		{
			if (IsMoving && CollabObjectToMove == null)
				StopMoving();

			if (!IsMoving)
				return;

			Transform t = CollabObjectToMove.transform;

			if (ScaleWithAxisInput)
			{
				if (scalable != null)
				{
					scalable.AddUniformScaleJoystick(AxisInput);
				}
			}
			else
			{
				if (Mathf.Abs(AxisInput.x) > Mathf.Abs(AxisInput.y))
				{
					float f = -AxisInput.x * RotateSpeedMultiplier * Time.deltaTime;
					movingObjectEuler += t.InverseTransformDirection(CollabObjectToMove.transform.up * f);
				}
				else
				{
					movingObjectLocalPos.z += AxisInput.y * ZSpeedMultiplier * Time.deltaTime;
					movingObjectLocalPos.z = Mathf.Max(movingObjectLocalPos.z, 0);
				}
			}

			if (HardwareConfig.IsVisionOS)
			{
				float zVelocity = transform.InverseTransformPoint(lastHandLocation).z / Time.deltaTime;
				if (Mathf.Abs(zVelocity) > 0.1f)
				{
					float resultingPush = -Mathf.Sign(zVelocity) * Mathf.Pow(zVelocity, 2);
					movingObjectLocalPos.z = Mathf.Min(movingObjectLocalPos.z + resultingPush * 0.2f, 100.0f);
					movingObjectLocalPos.z = Mathf.Max(movingObjectLocalPos.z, 0);
				}
			}
			lastHandLocation = transform.position;

			t.position = transform.TransformPoint(movingObjectLocalPos);
			t.rotation = Quaternion.Euler(movingObjectEuler);
			//t.localScale = moveMatrix.lossyScale.x * Vector3.one;
		}

		public void StopMoving()
		{
			if (!IsMoving)
				return;

			IsMoving = false;
			OnNotMovingObject.Invoke(true);
			OnStopMove.Invoke();
		}

		private void OnDisable()
		{
			StopMoving();
		}
	}
}
