using System;
using System.Collections.Generic;
using CollabXR.Hands;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
using UnityEngine.XR.VisionOS.InputDevices;
#endif

namespace CollabXR
{
	public class VisionOSInput : SingletonBehavior<VisionOSInput>
	{
		public Transform pointerHitPrimary,
			pointerHitSecondary;

		[SerializeField]
		Transform m_CameraOffset;

#if UNITY_VISIONOS
		PointerInput m_PointerInput;

		public enum PointerFeatureState
		{
			None,
			Tapped,
			Held,
			Released,
		}

		public PointerFeatureState primaryPointerState,
			secondaryPointerState;

		public XRBaseInteractor primaryInteractor,
			secondaryInteractor;

		bool primaryIsRight;
		HandTrackingInput handInput;

		void OnEnable()
		{
			m_PointerInput ??= new PointerInput();
			m_PointerInput.Enable();
			handInput = GetComponent<HandTrackingInput>();
		}

		void OnDisable()
		{
			m_PointerInput.Disable();
		}

		void Update()
		{
			var defaultActions = m_PointerInput.Default;
			var primaryPointer = defaultActions.PrimaryPointer.ReadValue<VisionOSSpatialPointerState>();
			var secondaryPointer = defaultActions.SecondaryPointer.ReadValue<VisionOSSpatialPointerState>();
			UpdateFeatures(primaryPointer, pointerHitPrimary, true, ref primaryPointerState);
			UpdateFeatures(secondaryPointer, pointerHitSecondary, false, ref secondaryPointerState);
		}

		void UpdateFeatures(VisionOSSpatialPointerState pointerState, Transform hitTransform, bool isPrimary, ref PointerFeatureState currentState)
		{
			var phase = pointerState.phase;
			var began = phase == VisionOSSpatialPointerPhase.Began;
			var active = began || phase == VisionOSSpatialPointerPhase.Moved;
			//hitTransform.gameObject.SetActive(active);

			if (began)
			{
				var rayOrigin = m_CameraOffset.TransformPoint(pointerState.startRayOrigin);
				var rayDirection = m_CameraOffset.TransformDirection(pointerState.startRayDirection);
				var devicePosition = m_CameraOffset.TransformPoint(pointerState.inputDevicePosition);

				var ray = new Ray(rayOrigin, rayDirection);
				var hit = Physics.Raycast(ray, out var hitInfo);
				//hitTransform.gameObject.SetActive(hit);
				hitTransform.position = hitInfo.point;

				bool thisPointerIsRight =
					Vector3.Distance(devicePosition, handInput.handFollowers[0].fingertip.transform.position)
					> Vector3.Distance(devicePosition, handInput.handFollowers[1].fingertip.transform.position);
				if (isPrimary)
					primaryIsRight = thisPointerIsRight;
			}
			if (began)
				currentState = PointerFeatureState.Tapped;
			else if (currentState == PointerFeatureState.Tapped && active)
				currentState = PointerFeatureState.Held;
			else if ((currentState == PointerFeatureState.Tapped || currentState == PointerFeatureState.Held) && !active)
				currentState = PointerFeatureState.Released;
			else if (currentState == PointerFeatureState.Released && !active)
				currentState = PointerFeatureState.None;
		}

		public PointerFeatureState GetPointerState(InputDeviceCharacteristics characteristics)
		{
			if (characteristics.HasFlag(InputDeviceCharacteristics.Right))
			{
				return primaryIsRight ? primaryPointerState : secondaryPointerState;
			}
			else
				return primaryIsRight ? secondaryPointerState : primaryPointerState;
		}

		public bool IsInteractorRight(XRBaseInteractor interactor)
		{
			bool isPrimaryInteractor = interactor == primaryInteractor;
			return primaryIsRight ? isPrimaryInteractor : !isPrimaryInteractor;
		}

		public XRBaseInteractor GetInteractor(bool right)
		{
			return primaryIsRight ? (right ? primaryInteractor : secondaryInteractor) : (right ? secondaryInteractor : primaryInteractor);
		}

#endif
	}
}
