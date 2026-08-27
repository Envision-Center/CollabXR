using System;
using System.Collections.Generic;
using CollabXR.Tools;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.XR;

namespace CollabXR.UI
{
	[DefaultExecutionOrder(20)] // for late updating the position
	public class RadialMenuController : MonoBehaviour
	{
		[Header("Buttons")]
		[SerializeField]
		private List<RadialMenuButton> buttons = new();

		[SerializeField, Tooltip("Tool that is automatically selected upon button tap if the menu is not opened.")]
		private RadialMenuButton defaultButton;

		[SerializeField]
		private RectTransform cursorTransform;

		[Header("Timing")]
		[SerializeField]
		private float holdThreshold = 0.15f;

		[SerializeField]
		private float selectDistance = 0.1f;

		[SerializeField]
		private float maxMoveDistance = 0.2f;

		[SerializeField]
		private float centerRadius;

		[SerializeField]
		private float totalRadius;

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
		private Vector2 cursorPosition;
		private int openRequestId;

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
			{
				Unsubscribe();
			}

			hand = newHand;
			handNotNull = newHand != null;

			if (handNotNull)
			{
				Subscribe();
			}
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

		/// <summary>
		/// Forcibly switches to the default tool. Closes menu if it is currently open.
		/// Note: this method is bound to by the Spawner tool in the Tool Palette prefab.
		/// </summary>
		public void SwitchToDefault()
		{
			isPressed = false;
			if (isMenuOpen)
			{
				OnMenuClose();
				isMenuOpen = false;
			}
			defaultButton.OnClicked(hand.isRight);
		}

		private void Update()
		{
			if (!isPressed)
			{
				return;
			}

			if (!isMenuOpen && Time.time - pressStartTime >= holdThreshold)
			{
				isMenuOpen = true;
				OnMenuOpen();
			}

			if (isPressed && isMenuOpen && openAnimFinished)
			{
				UpdateSelection();
			}
		}

		private void LateUpdate()
		{
			if (isMenuOpen)
			{
				transform.position = startMenuGlobalPosition;
				transform.rotation = openRotation;

				cursorTransform.anchoredPosition = cursorPosition;
			}
		}

		private void SetCursorPosition(Vector2 direction, float distance)
		{
			// let a = selectDistance, b = maxMoveDistance, c = centerRadius, d = totalRadius, x = distance

			if (distance < selectDistance)
			{
				// y1 = cx/a
				float multiplier = distance * centerRadius / selectDistance;
				cursorPosition = direction * Mathf.Clamp(multiplier, 0, centerRadius);
			}
			else
			{
				// y2 = (d-c) / (b-a) * (x-a) + c
				// y1 intersects y2 at (a, c) and y2 reaches (b, d)
				float multiplier = (totalRadius - centerRadius) / (maxMoveDistance - selectDistance) * (distance - selectDistance) + centerRadius;
				cursorPosition = direction * Mathf.Clamp(multiplier, 0, totalRadius);
			}
		}

		private void UpdateSelection()
		{
			Vector3 worldDisplacement = handRef.transform.position - startHandPosition;

			Vector2 tiltAxis = new(Vector3.Dot(worldDisplacement, transform.right), Vector3.Dot(worldDisplacement, transform.up));

			float distance = tiltAxis.magnitude;
			Vector2 direction = tiltAxis.normalized;

			SetCursorPosition(direction, distance);

			RadialMenuButton newSelection = defaultButton;

			if (distance >= selectDistance)
			{
				float tiltAngle = Mathf.Atan2(tiltAxis.x, tiltAxis.y) * 180f / Mathf.PI;
				if (tiltAngle < 0f)
				{
					tiltAngle += 360f;
				}

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

			int requestId = ++openRequestId;

			var palette = ToolPalette.Get(hand.isRight);
			if (palette != null)
			{
				palette.DeEquipTool(() => CompleteAnim(requestId));
			}
			else
			{
				CompleteAnim(requestId);
			}
		}

		private void CompleteAnim(int requestId)
		{
			// The tool deequip animation driving this can finish so ignore it unless its still for the current open
			if (!isMenuOpen || requestId != openRequestId)
			{
				return;
			}

			openAnimFinished = true;
			this.GenericTween(transform, transform.localScale, openScale, openCloseTweenDuration, openCloseEaseType, v => transform.localScale = v, (a, b, t) => Vector3.Lerp(a, b, t));
		}

		private void OnMenuClose()
		{
			try
			{
				// fallback to default just in case null
				(selectedButton ?? defaultButton)?.OnClicked(hand.isRight);

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
