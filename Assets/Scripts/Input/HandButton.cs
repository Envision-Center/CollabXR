//using CollabXR.VR;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.XR;

//namespace CollabXR.Input
//{
//	public class HandButton : MonoBehaviour
//	{
//		public InputFeatureUsage<bool> button;
//		public UnityEvent OnPress;
//		public UnityEvent OnRelease;
//		public InputFeatureUsage Pressed;

//		private RigHandRef handRef;
//		private RigHand hand;
//		private bool subscribed;

//		private void Awake()
//		{
//			handRef = this.GetRigHandRef();
//			handRef.Hand.AddListenerAndCheck(SubscribeToHand);
//		}

//		private void OnEnable()
//		{
//			Subscribe();
//		}

//		private void OnDisable()
//		{
//			Unsubscribe();
//		}

//		public void SubscribeToHand(RigHand newHand)
//		{
//			Unsubscribe();

//			hand = newHand;

//			Subscribe();
//		}

//		public void UnsubscribeFromHand()
//		{
//			Unsubscribe();
//			hand = null;
//		}

//		private void Subscribe()
//		{
//			if (!subscribed)
//			{
//				hand.Controller.SubscribeToOnPressEvent(button, OnPress.Invoke);
//				hand.Controller.SubscribeToOnReleaseEvent(button, OnRelease.Invoke);

//				subscribed = true;
//			}
//		}

//		private void Unsubscribe()
//		{
//			if (subscribed)
//			{
//				subscribed = false;

//				hand.Controller.UnsubscribeToOnPressEvent(button, OnPress.Invoke);
//				hand.Controller.UnsubscribeToOnReleaseEvent(button, OnRelease.Invoke);
//			}
//		}
//	}
//}
