using System;
using System.Collections.Generic;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.Tools.Drawing;
using Fusion;
using UnityEngine;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	[DefaultExecutionOrder(50)]
	public class DrawingBrush : MonoBehaviour
	{
		[SerializeField]
		private Transform brushTipTransform;

		[SerializeField]
		private GameObject strokeContainerPrefab;

		[SerializeField]
		private GameObject substrokePrefab;

		public bool IsDrawing { get; set; }

		public void SetIsDrawing(bool isDrawing)
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				isDrawing = false;
				return;
			}

			IsDrawing = isDrawing;
		}

		public Color32 StrokeColor { get; set; } = Color.red;

		private Vector3 lastStrokePointPos;
		private Quaternion lastStrokePointRot;
		private BrushSubStroke currentSubStroke;

		private List<BrushSubStroke> currentWholeStroke = new();
		private NetworkObject currentStrokeContainer = null;

		private CollabObject intersectedObject;

		//[SerializeField] private float triggerWeightPower = 0.25f;
		[SerializeField]
		private float baseStrokeWeight = 0.02f;

		public void SetHueFromColorWheelDirection(Vector2 direction)
		{
			if (direction.magnitude < 0.8f)
				return;

			float hue = Mathf.Atan2(-direction.x, -direction.y) / (2 * Mathf.PI) + 0.5f;
			StrokeColor = Color.HSVToRGB(hue, 1, 1);
		}

		public void BeginStroke()
		{
			Debug.Log("Beginning stroke");
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}
			IsDrawing = true;

			currentStrokeContainer = NetworkManager.Runner.Spawn(strokeContainerPrefab, brushTipTransform.position);
			CheckOverlaps();

			CreateSubstroke();
		}

		private void CreateSubstroke()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}

			NetworkObject spawnedStroke = NetworkManager.Runner.Spawn(substrokePrefab, position: brushTipTransform.position);

			currentSubStroke = spawnedStroke.GetComponent<BrushSubStroke>();
			currentSubStroke.SetParent(currentStrokeContainer);
			currentSubStroke.Init(StrokeColor, baseStrokeWeight);

			currentSubStroke.name += currentWholeStroke.Count;

			currentWholeStroke.Add(currentSubStroke);
		}

		private void LateUpdate()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}

			if (IsDrawing)
			{
				CheckOverlaps();
				if (Vector3.Distance(lastStrokePointPos, brushTipTransform.position) >= 0.01f || Quaternion.Angle(lastStrokePointRot, brushTipTransform.rotation) >= 5f)
				{
					ContinueStroke();
				}
				else
				{
					currentSubStroke.SetLastPoint(brushTipTransform.position, brushTipTransform.rotation);
				}
			}
		}

		public void SetStrokeParentFromObject(GameObject obj)
		{
			CollabObject c = obj?.GetComponentInParent<CollabObject>();
			NetworkObject netObj = obj?.GetComponentInParent<NetworkObject>();

			if (c != null && c.HasData) // is a valid collab object with data
			{
				intersectedObject = c;
				CheckOverlaps();
			}
			else if (obj == null)
			{
				intersectedObject = null;
			}
		}

		private void CheckOverlaps()
		{
			if (IsDrawing && intersectedObject != null && currentStrokeContainer.transform.parent == null)
			{
				currentStrokeContainer.GetComponent<CollabObject>().ParentToOtherCollabObject(intersectedObject);
			}
		}

		private void ContinueStroke()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}

			currentSubStroke.AddStrokePoint(brushTipTransform.position, brushTipTransform.rotation);

			if (currentSubStroke.GetCapacityRemaining() == 0)
			{
				CreateSubstroke();
				ContinueStroke();
			}

			lastStrokePointPos = brushTipTransform.position;
			lastStrokePointRot = brushTipTransform.rotation;
		}

		public void EndStroke()
		{
			if (!IsDrawing || (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw))
			{
				return;
			}

			IsDrawing = false;
			if (currentSubStroke != null)
			{
				currentSubStroke.SetLastPoint(brushTipTransform.position, brushTipTransform.rotation);
				currentSubStroke = null;
			}
			currentWholeStroke.Clear();
			currentStrokeContainer = null;
		}

		private void OnDisable()
		{
			EndStroke();
		}
	}
}
