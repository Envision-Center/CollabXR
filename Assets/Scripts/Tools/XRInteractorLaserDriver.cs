using CollabXR.Objects.Components;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CollabXR.Tools
{
	public class XRInteractorLaserDriver : MonoBehaviour
	{
		private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor interactor;
		private LineRendererLaser laser;
		private LineRenderer lineRenderer;

		private Vector3 defaultRayCastOrig;

		private void Awake()
		{
			interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
			laser = GetComponent<LineRendererLaser>();
			lineRenderer = GetComponent<LineRenderer>();
		}

		private void Start()
		{
			defaultRayCastOrig = interactor.rayOriginTransform.position;
		}

		private void LateUpdate()
		{
			bool isOverUI = interactor.IsOverUIGameObject();

			interactor.TryGetCurrentUIRaycastResult(out RaycastResult result);

			lineRenderer.enabled = isOverUI;
			laser.SetEndPositionForFrame(result.worldPosition);
		}

		/// <summary>
		/// Updates where the UI interaction ray is projected from the controller.
		/// Used for when the controller tool updates.
		/// </summary>
		/// <param name="origin">New local pos for ray's origin. Resets to default position by default</param>
		public void SetRayCastOrigin(Vector3 origin = default)
		{
			if (origin == default && defaultRayCastOrig != null)
			{
				origin = defaultRayCastOrig;
			}
			laser.startOffset = interactor.rayOriginTransform.localPosition = origin;
		}
	}
}
