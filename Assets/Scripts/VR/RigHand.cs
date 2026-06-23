using System;
using CollabXR.Objects.Components;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_VISIONOS
using UnityEngine.XR.VisionOS;
#endif

namespace CollabXR.VR
{
	public class RigHand : MonoBehaviour
	{
		public bool isRight;

		public UnityEvent<bool> OnHoverUI;

		[SerializeField]
		private RigHand otherHand;
		public static RigHand Left { get; private set; }
		public static RigHand Right { get; private set; }

		public static EventVariable<bool> BothHandsInitialized = new();
		public RigHand OtherHand => otherHand;

		public XRControllerSubject Controller { get; private set; }
		public UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor Interactor { get; private set; }

		public LineRendererLaser VisualLaserPointer { get; private set; }
		public Transform raycastOrigin,
			raycastPoint;
		public bool usingCustomRaycast => raycastOrigin != null && raycastPoint != null;
		public Vector3 raycastDirection => (raycastPoint.position - raycastOrigin.position).normalized;

		private void Awake()
		{
			Controller = GetComponentInChildren<XRControllerSubject>();
			Interactor = GetComponentInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
			VisualLaserPointer = GetComponentInChildren<LineRendererLaser>();

			if (isRight)
				Right = this;
			else
				Left = this;

			if (Left != null && Right != null)
				BothHandsInitialized.Value = true;
		}

		private bool wasHoveringOverUI;

		private void Update()
		{
			if (GetInteractor() != null)
			{
				bool isHoveringOverUI = TestIfOverUI();

				if (isHoveringOverUI != wasHoveringOverUI)
				{
					OnHoverUI?.Invoke(isHoveringOverUI);
				}

				wasHoveringOverUI = isHoveringOverUI;
			}
		}

		private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor GetInteractor()
		{
			if (HardwareConfig.IsVisionOS)
			{
#if UNITY_VISIONOS
				return (UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor)VisionOSInput.Instance.GetInteractor(isRight);
#endif
			}
			return Interactor;
		}

		public bool TestIfOverUI()
		{
			return GetInteractor().IsOverUIGameObject();
		}
	}
}
