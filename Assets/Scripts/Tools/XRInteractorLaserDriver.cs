using CollabXR.Objects.Components;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CollabXR.Tools
{
	public class XRInteractorLaserDriver : MonoBehaviour
	{
		private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor interactor;
		private LineRendererLaser laser;
		private LineRenderer lineRenderer;

		private void Awake()
		{
			interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
			laser = GetComponent<LineRendererLaser>();
			lineRenderer = GetComponent<LineRenderer>();
		}

		private void LateUpdate()
		{
			bool isOverUI = interactor.IsOverUIGameObject();

			interactor.TryGetCurrentUIRaycastResult(out RaycastResult result);

			lineRenderer.enabled = isOverUI;
			laser.SetEndPositionForFrame(result.worldPosition);
		}
	}
}
