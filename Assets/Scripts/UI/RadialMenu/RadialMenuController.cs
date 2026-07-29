using System;
using System.Collections.Generic;
using CollabXR.Tools;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.XR;

namespace CollabXR.UI
{
	// Must run after TransformFollow's LateUpdate (order 10), since this hand's
	// Tool Palette root has its pose copied from the hand there rather than being
	// a real Unity parent - otherwise we'd rotate/read the canvas before it's synced.
	[DefaultExecutionOrder(20)]
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
		private float selectDistance = 0.1f;

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
		private Vector3 openScale;

		private RadialMenuButton selectedButton;
		private Vector3 startHandPosition;
		private Vector3 startMenuGlobalPosition;
		private Vector3 awakeLocalPosition;
		private Quaternion openRotation;

		private void Awake()
		{
			openScale = transform.localScale;
			transform.localScale = Vector3.zero;

			handRef = this.GetRigHandRef();
			handRef.Hand.AddListenerAndCheck(SubscribeToHand);

			awakeLocalPosition = transform.localPosition;
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

			if (isPressed && isMenuOpen && openAnimFinished)
				UpdateSelection();
		}

		private void LateUpdate()
		{
			if (isMenuOpen)
			{
				transform.position = startMenuGlobalPosition;
				transform.rotation = openRotation;
			}
		}

		private void UpdateSelection()
		{
			float distance = Vector3.Distance(handRef.transform.position, startHandPosition);
			Vector3 direction = (handRef.transform.position - startHandPosition).normalized;
			Vector2 tiltAxis = new(Vector3.Dot(direction, transform.right), Vector3.Dot(direction, transform.up));

			RadialMenuButton newSelection = defaultButton;

			if (distance >= selectDistance)
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

			startHandPosition = handRef.transform.position;
			startMenuGlobalPosition = transform.position;
			transform.position = startMenuGlobalPosition;

			Vector3 offsetDirection = Camera.main.transform.position - startMenuGlobalPosition;
			openRotation = Quaternion.LookRotation(new(offsetDirection.x, 0, offsetDirection.z));
			transform.rotation = openRotation;

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
			try
			{
				selectedButton?.OnClicked(hand.isRight);

				foreach (var button in buttons)
				{
					button.OnDeselected();
				}
				defaultButton?.OnDeselected();
				selectedButton = null;
			}
			catch (Exception e)
			{
				Debug.LogError($"Error caught closing menu; proceeding: {e}");
			}

			this.GenericTween(
				transform,
				transform.localScale,
				Vector3.zero,
				openCloseTweenDuration,
				openCloseEaseType,
				v => transform.localScale = v,
				(a, b, t) => Vector3.Lerp(a, b, t),
				() => transform.localPosition = awakeLocalPosition
			);
		}
	}
}
