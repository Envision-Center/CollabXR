using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace CollabXR.Input
{
	public class HandSecondaryInput : MonoBehaviour
	{
		public UnityEvent OnPress;
		public UnityEvent OnRelease;
		public UnityEvent<bool> OnChange;

		private RigHand hand;
		private RigHandRef handRef;
		private bool handNotNull;
		private bool subscribed;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
			handRef.Hand.AddListenerAndCheck(SubscribeToHand);

			OnPress.AddListener(() => OnChange.Invoke(true));
			OnRelease.AddListener(() => OnChange.Invoke(false));
		}

		private void OnEnable()
		{
			Subscribe();
		}

		private void OnDisable()
		{
			Unsubscribe();
		}

		public void SubscribeToHand(RigHand newHand)
		{
			if (subscribed)
				Unsubscribe();

			hand = newHand;
			handNotNull = newHand != null;

			if (handNotNull)
				Subscribe();
		}

		public void UnsubscribeFromHand()
		{
			Unsubscribe();
			handNotNull = false;
			hand = null;
		}

		private void Subscribe()
		{
			if (handNotNull && !subscribed)
			{
				hand.Controller.SubscribeToOnPressEvent(CommonUsages.secondaryButton, OnPress.Invoke);
				hand.Controller.SubscribeToOnReleaseEvent(CommonUsages.secondaryButton, OnRelease.Invoke);

				subscribed = true;
			}
		}

		private void Unsubscribe()
		{
			if (subscribed)
			{
				subscribed = false;

				hand.Controller.UnsubscribeToOnPressEvent(CommonUsages.secondaryButton, OnPress.Invoke);
				hand.Controller.UnsubscribeToOnReleaseEvent(CommonUsages.secondaryButton, OnRelease.Invoke);
			}
		}
	}
}
