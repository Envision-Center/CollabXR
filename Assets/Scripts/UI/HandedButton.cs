using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CollabXR.UI
{
	public class HandedButton : MonoBehaviour, IPointerClickHandler
	{
		public UnityEvent onClick;
		public UnityEvent<bool> onClickIsRight;

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData is TrackedDeviceEventData trackedDeviceEventData)
			{
				if (trackedDeviceEventData.interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor xrInteractor)
				{
					if (xrInteractor.xrController is XRController xrController)
					{
						if (xrController.controllerNode == XRNode.LeftHand || xrController.controllerNode == XRNode.RightHand)
						{
							onClick.Invoke();
							onClickIsRight.Invoke(xrController.controllerNode == XRNode.RightHand);
						}
					}
					else if (HardwareConfig.IsVisionOS)
					{
#if UNITY_VISIONOS
						bool isRight = VisionOSInput.Instance.IsInteractorRight(xrInteractor);
						onClick.Invoke();
						onClickIsRight.Invoke(isRight);
						Debug.Log(isRight ? "Right!" : "Left!");
#endif
					}
				}
			}
		}
	}
}
