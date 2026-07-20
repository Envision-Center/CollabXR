using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CollabXR.Tools;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.Serialization;

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

		[SerializeField]
		private bool subscribeToRaycasterOnStart = true;

		[SerializeField]
		private bool subscribeToToolChangeOnStart = true;

		private void Awake()
		{
			lineRenderer = GetComponent<LineRenderer>();

			if (subscribeToRaycasterOnStart)
			{
				SubscribeToRaycaster(GetComponent<Raycaster>());
			}
		}

		public void SubscribeToRaycaster(Raycaster raycaster)
		{
			if (raycaster == null)
			{
				return;
			}

			// ensure the offset of the ray reflects the true origin of the ray
			raycaster.onEnable.AddListener((enabled) =>
			{
				this.SetEnabled(enabled);
				//if (enabled)
				//{
				//	startOffset = Vector3.forward * raycaster.forwardRayOffset + Vector3.up * raycaster.upwardRayOffset;
				//}
			});
			raycaster.onHitPoint.AddListener(SetEndPositionForFrame);
			raycaster.onHitNothing.AddListener(SetEndPositionForFrame);


		}

		public void SubscribeToToolChange()
		{
			if (ToolPalette.Left == null || ToolPalette.Right == null)
			{
				return;
			}

			XRInteractorLaserDriver laserDriver = hand.GetComponentInChildren<XRInteractorLaserDriver>();
			if (laserDriver != null)
			{
				laserDriver.SetRayCastOrigin(startOffset);
			}

			Debug.Log($"{gameObject} subbing to tool change");
			// TODO, add to left hand
			ToolPalette.Right.onToolChange.AddListener((prevTool, newTool) =>
			{
				if (!newTool)
				{
					return;
				}

				if (newTool.TryGetComponent(out Raycaster raycaster))
				{
					startOffset = Vector3.forward * raycaster.forwardRayOffset + Vector3.up * raycaster.upwardRayOffset;
					XRInteractorLaserDriver laserDriver = hand.GetComponentInChildren<XRInteractorLaserDriver>();
					if (laserDriver != null)
					{
						laserDriver.SetRayCastOrigin(startOffset);
					}
				}
				else
				{
					XRInteractorLaserDriver laserDriver = hand.GetComponentInChildren<XRInteractorLaserDriver>();
					if (laserDriver != null)
					{
						laserDriver.SetRayCastOrigin();
					}
				}
			});
		}

		private void Start()
		{
			lineRenderer.positionCount = 2;
			lineRenderer.useWorldSpace = false;
			hand = GetComponentInParent<RigHandRef>()?.Hand.Value;

			if (subscribeToToolChangeOnStart)
			{
				SubscribeToToolChange();
			}
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
