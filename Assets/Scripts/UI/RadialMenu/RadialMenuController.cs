using System.Collections.Generic;
using CollabXR.Tools;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.XR;

namespace CollabXR.UI
{
	public class RadialMenuController : MonoBehaviour
	{
		[Header("Buttons")]
		[SerializeField]
		private List<RadialMenuButton> buttons = new();

		[SerializeField]
		private RadialMenuButton defaultButton;

		[Header("Timing")]
		[SerializeField]
		private float holdThreshold = 0.15f;

		[SerializeField]
		private float tiltDeadzone = 0.1f;

		[Header("Open/Close")]
		[SerializeField]
		private float openCloseTweenDuration = 0.15f;

		[SerializeField]
		private EaseType openCloseEaseType = EaseType.EaseOut;

		private RigHandRef handRef;
		private RigHand hand;
		private bool handNotNull;
		private bool subscribed;

		private bool isPressed;
		private bool isMenuOpen;
		private bool openAnimFinished;
		private float pressStartTime;
		private Quaternion referenceRotation;
		private Vector3 openScale;

		private RadialMenuButton selectedButton;

		private void Awake()
		{
			openScale = transform.localScale;
			transform.localScale = Vector3.zero;

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
			if (subscribed)
				Unsubscribe();

			hand = newHand;
			handNotNull = newHand != null;

			if (handNotNull)
				Subscribe();
		}

		private void Subscribe()
		{
			if (handNotNull && !subscribed)
			{
				hand.Controller.SubscribeToOnPressEvent(CommonUsages.secondaryButton, OnButtonPress);
				hand.Controller.SubscribeToOnReleaseEvent(CommonUsages.secondaryButton, OnButtonRelease);
				subscribed = true;
			}
		}

		private void Unsubscribe()
		{
			if (subscribed)
			{
				subscribed = false;
				hand.Controller.UnsubscribeToOnPressEvent(CommonUsages.secondaryButton, OnButtonPress);
				hand.Controller.UnsubscribeToOnReleaseEvent(CommonUsages.secondaryButton, OnButtonRelease);
			}
		}

		private void OnButtonPress()
		{
			isPressed = true;
			pressStartTime = Time.time;
			referenceRotation = hand.Controller.transform.rotation;
		}

		private void OnButtonRelease()
		{
			isPressed = false;

			if (isMenuOpen)
			{
				OnMenuClose();
				isMenuOpen = false;
			}
			else
			{
				defaultButton.OnClicked(hand.isRight);
			}
		}

		private void Update()
		{
			if (!isPressed)
				return;

			if (!isMenuOpen && Time.time - pressStartTime >= holdThreshold)
			{
				isMenuOpen = true;
				OnMenuOpen();
			}

			if (isMenuOpen && openAnimFinished)
				UpdateSelection();
		}

		private void UpdateSelection()
		{
			Vector3 localForward = Quaternion.Inverse(referenceRotation) * hand.Controller.transform.forward;
			Vector2 tiltAxis = new(localForward.x, localForward.y);

			RadialMenuButton newSelection = defaultButton;

			if (tiltAxis.magnitude >= tiltDeadzone)
			{
				float tiltAngle = -Mathf.Atan2(tiltAxis.x, tiltAxis.y) * 360f / (Mathf.PI * 2f);
				if (tiltAngle < 0f)
					tiltAngle += 360f;

				foreach (var button in buttons)
				{
					if (button.IsAngleInRange(tiltAngle))
					{
						newSelection = button;
						break;
					}
				}
			}

			if (newSelection != selectedButton)
			{
				selectedButton?.OnDeselected();
				selectedButton = newSelection;
				selectedButton.OnSelected(hand.isRight);
			}
		}

		private void OnMenuOpen()
		{
			foreach (var button in buttons)
			{
				button.OnDeselected();
			}
			defaultButton?.OnDeselected();
			selectedButton = null;

			openAnimFinished = false;

			var palette = ToolPalette.Get(hand.isRight);
			if (palette != null)
				palette.DeEquipTool(CompleteAnim);
			else
				CompleteAnim();
		}

		private void CompleteAnim()
		{
			openAnimFinished = true;
			this.GenericTween(transform, transform.localScale, openScale, openCloseTweenDuration, openCloseEaseType, v => transform.localScale = v, (a, b, t) => Vector3.Lerp(a, b, t));
		}

		private void OnMenuClose()
		{
			selectedButton?.OnClicked(hand.isRight);

			foreach (var button in buttons)
			{
				button.OnDeselected();
			}
			defaultButton?.OnDeselected();
			selectedButton = null;

			this.GenericTween(transform, transform.localScale, Vector3.zero, openCloseTweenDuration, openCloseEaseType, v => transform.localScale = v, (a, b, t) => Vector3.Lerp(a, b, t));
		}
	}
}
