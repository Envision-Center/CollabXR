using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.VR
{
	public class RigHandRef : MonoBehaviour
	{
		private enum InitialHandSide
		{
			None = 0,
			Left = 1,
			Right = 2,
		}

		[SerializeField]
		private InitialHandSide initialHandSide;

		public EventVariable<RigHand> Hand = new();

		private void Awake()
		{
			RigHand.BothHandsInitialized.AddListenerAndCheck(HandsInitializedCheck);

			if ((int)initialHandSide > 0)
			{
				SetHandFromSide(initialHandSide == InitialHandSide.Right);
			}
		}

		private void HandsInitializedCheck(bool handsAreSet)
		{
			if (handsAreSet)
			{
				if ((int)initialHandSide > 0)
				{
					SetHandFromSide(initialHandSide == InitialHandSide.Right);
				}
			}
		}

		public void SetHandFromSide(bool isRight)
		{
			var newHand = isRight ? RigHand.Right : RigHand.Left;

			Hand.Value = newHand;
		}
	}

	public static class RigHandRefExt
	{
		public static RigHandRef GetRigHandRef(this MonoBehaviour obj)
		{
			return obj.GetComponentInParent<RigHandRef>();
		}

		public static void SetEnabled(this Behaviour behaviour, bool enabled)
		{
			behaviour.enabled = enabled;
		}

		public static void SetDisabled(this Behaviour behaviour, bool disabled)
		{
			SetEnabled(behaviour, !disabled);
		}
	}
}
