using CollabXR.Tools;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Objects.Components
{
	[RequireComponent(typeof(LineRenderer))]
	[DefaultExecutionOrder(1000)]
	public class LineRendererLaser : MonoBehaviour
	{
		public Vector3 startOffset = new(0, 0, 0.05f);
		private LineRenderer lineRenderer;

		private Vector3 localStartPos,
			localEndPos;
		private RigHand hand;
		private XRInteractorLaserDriver uiLaserDriver;

		[SerializeField, Tooltip("Syncs line visual to RayCaster tool")]
		private bool subscribeToRaycasterOnStart = true;

		[SerializeField, Tooltip("Syncs line visual on tool changes")]
		private bool subscribeToToolChangeOnStart = true;

		private void Awake()
		{
			lineRenderer = GetComponent<LineRenderer>();

			if (subscribeToRaycasterOnStart)
			{
				SubscribeToRaycaster(GetComponent<Raycaster>());
			}
		}

		private void Start()
		{
			lineRenderer.positionCount = 2;
			lineRenderer.useWorldSpace = false;

			// laser pointer can exist on hand rig or tool palette
			hand = GetComponentInParent<RigHandRef>()?.Hand.Value ?? GetComponentInParent<RigHand>();
			if (hand)
			{
				uiLaserDriver = hand.GetComponentInChildren<XRInteractorLaserDriver>();
			}

			if (subscribeToToolChangeOnStart && hand)
			{
				SubscribeToToolChange(hand.isRight ? ToolPalette.Right : ToolPalette.Left);
			}
		}

		private void SubscribeToRaycaster(Raycaster raycaster)
		{
			if (raycaster == null)
			{
				return;
			}

			raycaster.onEnable.AddListener(this.SetEnabled);
			raycaster.onHitPoint.AddListener(SetEndPositionForFrame);
			raycaster.onHitNothing.AddListener(SetEndPositionForFrame);
		}

		private void SubscribeToToolChange(ToolPalette palette)
		{
			if (palette == null)
			{
				return;
			}

			SyncUILaserPointer(startOffset);

			palette.onToolChange.AddListener((prevTool, newTool) =>
			{
				if (!newTool)
				{
					return;
				}

				if (newTool.TryGetComponent(out Raycaster raycaster))
				{
					startOffset = Vector3.forward * raycaster.forwardRayOffset + Vector3.up * raycaster.upwardRayOffset;
					SyncUILaserPointer(startOffset);
				}
				else
				{
					SyncUILaserPointer();
				}
			});
		}

		/// <summary>
		/// Update the ray visual when hovering over UI.
		/// </summary>
		/// <remarks>
		/// UI ray is a separate entity as ray projected from tool so must be updated here as tool changes.
		/// </remarks>
		public void SyncUILaserPointer(Vector3 origin = default)
		{
			if (uiLaserDriver == null)
			{
				return;
			}

			uiLaserDriver.SetRayCastOrigin(origin);
		}

		private void OnEnable()
		{
			lineRenderer.enabled = enabled;
		}

		private void OnDisable()
		{
			lineRenderer.enabled = enabled;
		}

		public void SetEndPositionForFrame(Vector3 worldPos)
		{
			localStartPos = (hand != null && hand.usingCustomRaycast) ? transform.InverseTransformPoint(hand.raycastOrigin.position) : startOffset;
			localEndPos = transform.InverseTransformPoint(worldPos);
		}

		private void OnWillRenderObject()
		{
			lineRenderer.SetPosition(0, localStartPos);
			lineRenderer.SetPosition(1, localEndPos);
		}
	}
}
