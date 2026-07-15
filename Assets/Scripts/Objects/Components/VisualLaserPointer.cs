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
		public Vector3 startOffset = new Vector3(0, 0.03f, 0.05f);
		private LineRenderer lineRenderer;

		private Vector3 localStartPos,
			localEndPos;
		private RigHand hand;

		[SerializeField]
		private bool subscribeToRaycasterOnStart = true;

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
				return;

			raycaster.onEnable.AddListener(this.SetEnabled);
			raycaster.onHitPoint.AddListener(SetEndPositionForFrame);
			raycaster.onHitNothing.AddListener(SetEndPositionForFrame);
		}

		private void Start()
		{
			lineRenderer.positionCount = 2;
			lineRenderer.useWorldSpace = false;
			hand = GetComponentInParent<RigHandRef>()?.Hand.Value;
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
