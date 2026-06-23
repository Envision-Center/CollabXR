using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace CollabXR.Input
{
	[DefaultExecutionOrder(30)]
	public class HandInput : MonoBehaviour
	{
		public UnityEvent OnTriggerPress;
		public UnityEvent OnTriggerRelease;
		public UnityEvent<bool> OnTriggerChange;

		public UnityEvent OnStickPress;
		public UnityEvent OnStickRelease;
		public UnityEvent<bool> OnStickButtonChange;

		public UnityEvent<Vector2> OnPrimaryAxis;

		private RigHandRef handRef;
		private RigHand hand;
		private bool handNotNull;
		private bool subscribed;

		private void Awake()
		{
			OnTriggerPress.AddListener(() => OnTriggerChange.Invoke(true));
			OnTriggerRelease.AddListener(() => OnTriggerChange.Invoke(false));

			OnStickPress.AddListener(() => OnStickButtonChange.Invoke(true));
			OnStickRelease.AddListener(() => OnStickButtonChange.Invoke(false));

			handRef = this.GetRigHandRef();
			handRef.Hand.AddListenerAndCheck(SubscribeToHand);
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
			Unsubscribe();

			hand = newHand;
			handNotNull = newHand != null;

			if (enabled)
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
				if (HardwareConfig.IsVisionOS)
				{
					hand.Controller.SubscribeToVisionOSTap(OnTriggerPress.Invoke);
					hand.Controller.SubscribeToVisionOSRelease(OnTriggerRelease.Invoke);
				}
				else
				{
					hand.Controller.SubscribeToOnPressEvent(CommonUsages.triggerButton, OnTriggerPress.Invoke);
					hand.Controller.SubscribeToOnReleaseEvent(CommonUsages.triggerButton, OnTriggerRelease.Invoke);

					hand.Controller.SubscribeToOnPressEvent(CommonUsages.primary2DAxisClick, OnStickPress.Invoke);
					hand.Controller.SubscribeToOnReleaseEvent(CommonUsages.primary2DAxisClick, OnStickRelease.Invoke);
				}

				subscribed = true;
			}
		}

		private void Update()
		{
			if (handNotNull)
			{
				OnPrimaryAxis.Invoke(hand.Controller.GetFeatureState(CommonUsages.primary2DAxis));
			}
		}

		private void Unsubscribe()
		{
			if (subscribed)
			{
				subscribed = false;

				if (HardwareConfig.IsVisionOS)
				{
					hand.Controller.UnsubscribeToVisionOSTap(OnTriggerPress.Invoke);
					hand.Controller.UnsubscribeToVisionOSRelease(OnTriggerRelease.Invoke);
				}
				else
				{
					hand.Controller.UnsubscribeToOnPressEvent(CommonUsages.triggerButton, OnTriggerPress.Invoke);
					hand.Controller.UnsubscribeToOnReleaseEvent(CommonUsages.triggerButton, OnTriggerRelease.Invoke);

					hand.Controller.UnsubscribeToOnPressEvent(CommonUsages.primary2DAxisClick, OnStickPress.Invoke);
					hand.Controller.UnsubscribeToOnReleaseEvent(CommonUsages.primary2DAxisClick, OnStickRelease.Invoke);
				}
			}
		}
	}
}
