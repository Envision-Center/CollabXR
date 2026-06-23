using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace CollabXR.Input
{
	[DefaultExecutionOrder(30)]
	public class HandGripInput : MonoBehaviour
	{
		public UnityEvent OnGripPress;
		public UnityEvent OnGripRelease;
		public UnityEvent<bool> OnGripChange;

		private RigHand hand;
		private RigHandRef handRef;
		private bool handNotNull;
		private bool subscribed;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
			handRef.Hand.AddListenerAndCheck(SubscribeToHand);

			OnGripPress.AddListener(() => OnGripChange.Invoke(true));
			OnGripRelease.AddListener(() => OnGripChange.Invoke(false));
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
				hand.Controller.SubscribeToVisionOSTap(OnGripPress.Invoke);
				hand.Controller.SubscribeToVisionOSRelease(OnGripRelease.Invoke);
				hand.Controller.SubscribeToOnPressEvent(CommonUsages.gripButton, OnGripPress.Invoke);
				hand.Controller.SubscribeToOnReleaseEvent(CommonUsages.gripButton, OnGripRelease.Invoke);

				subscribed = true;
			}
		}

		private void Unsubscribe()
		{
			if (subscribed)
			{
				subscribed = false;

				hand.Controller.UnsubscribeToVisionOSTap(OnGripPress.Invoke);
				hand.Controller.UnsubscribeToVisionOSRelease(OnGripRelease.Invoke);
				hand.Controller.UnsubscribeToOnPressEvent(CommonUsages.gripButton, OnGripPress.Invoke);
				hand.Controller.UnsubscribeToOnReleaseEvent(CommonUsages.gripButton, OnGripRelease.Invoke);
			}
		}
	}
}
