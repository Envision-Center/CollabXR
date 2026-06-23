using CollabXR.Colocation;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Tools
{
	public class Teleporter : MonoBehaviour
	{
		public Vector3 TeleportTarget { get; set; }
		public bool CanTeleport { get; set; } = true;

		private const float TurnAxisInputThreshold = 0.8f;
		private const float TurnAngle = 45;
		private bool didTurn;

		public void Teleport(Vector3 point)
		{
			if (!isActiveAndEnabled || !CanTeleport || ColocationDriver.IsAnchored.Value)
				return;

			if (isActiveAndEnabled)
				HardwareRig.Instance.MovePersonTo(point);
		}

		public void Turn(float a)
		{
			if (!isActiveAndEnabled || ColocationDriver.IsAnchored.Value)
				return;

			HardwareRig.Instance.RotatePersonBy(a);
		}

		public void Teleport() => Teleport(TeleportTarget);

		public void TurnAxisInput(Vector2 axis)
		{
			bool shouldTurn = Mathf.Abs(axis.x) > TurnAxisInputThreshold;

			if (!didTurn && shouldTurn)
			{
				didTurn = true;

				Turn(TurnAngle * Mathf.Sign(axis.x));
			}

			didTurn = shouldTurn;
		}
	}
}
