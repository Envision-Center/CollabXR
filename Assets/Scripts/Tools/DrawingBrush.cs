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
	[RequireComponent(typeof(OverlapTracker))]
	public class DrawingBrush : MonoBehaviour
	{
		[SerializeField]
		private Transform brushTipTransform;

		[SerializeField]
		private GameObject strokeContainerPrefab;

		[SerializeField]
		private GameObject substrokePrefab;

		private OverlapTracker overlapTracker;

		public bool IsDrawing { get; set; }

		private void Awake()
		{
			overlapTracker = GetComponent<OverlapTracker>();
		}

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
		private BrushContainer currentStrokeContainer = null;

		//[SerializeField] private float triggerWeightPower = 0.25f;
		[SerializeField]
		private float baseStrokeWeight = 0.02f;

		private SpawnableObject lastOverlap;

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

			currentStrokeContainer = NetworkManager.Runner.Spawn(strokeContainerPrefab, brushTipTransform.position).GetComponent<BrushContainer>();

			CreateSubstroke();
		}

		private void CreateSubstroke()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}
			if (SessionManager.Instance.CanAddBrushStroke())
			{
				SessionManager.Instance.AddBrushStroke();
				NetworkObject spawnedStroke = NetworkManager.Runner.Spawn(substrokePrefab, position: brushTipTransform.position);

				currentSubStroke = spawnedStroke.GetComponent<BrushSubStroke>();
				currentSubStroke.SetParent(currentStrokeContainer.Object);
				currentSubStroke.Init(StrokeColor, baseStrokeWeight);
				currentSubStroke.name += currentWholeStroke.Count;
				currentWholeStroke.Add(currentSubStroke);
				overlapTracker.ignoreObject = currentSubStroke.gameObject;
			}
			else
			{
				EndStroke();
			}
		}

		private void LateUpdate()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDraw)
			{
				return;
			}

			if (IsDrawing)
			{
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
			BrushSubStroke b = obj?.GetComponentInParent<BrushSubStroke>();
			BrushContainer bContainer = b?.GetComponentInParent<BrushContainer>();
			NetworkObject netObj = obj?.GetComponentInParent<NetworkObject>();

			if (c != null && c.HasData) // is a valid collab object with data
			{
				CheckShouldParent(c);
			}
			else if(b != null && bContainer != null) // is a valid brush stroke with a container
			{
				CheckShouldParent(bContainer);
			}
			else
			{
				lastOverlap = null;
			}
		}

		private void CheckShouldParent(SpawnableObject obj)
		{
			lastOverlap = obj;
			if (IsDrawing) // brush tool is active
			{
				if (currentStrokeContainer != null) // have already created a brush container
				{
					if (lastOverlap != null && currentStrokeContainer.transform.parent == null) // intersected with something while drawing and container isn't already parented
					{
						if (lastOverlap.GetType() == typeof(CollabObject)) // is collab object
						{
							currentStrokeContainer.ParentToOtherSpawnableObject(lastOverlap);
						}
						else if (lastOverlap.GetType() == typeof(BrushContainer) && currentStrokeContainer != lastOverlap) // is a different container
						{
							foreach (BrushSubStroke stroke in currentWholeStroke)
							{
								stroke.SetParent(lastOverlap.Object);
							}
							currentStrokeContainer.MarkForDeletionWhenEmpty();
						}
					}
				}
				else // haven't created a brush container
				{
					if (lastOverlap.GetType() == typeof(CollabObject)) // is collab object
					{
						currentStrokeContainer.ParentToOtherSpawnableObject(lastOverlap);
					}
				}
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
				overlapTracker.ignoreObject = null;
			}
			currentWholeStroke.Clear();
			currentStrokeContainer = null;
		}

		public void MoveToTopLevelContainer()
		{

		}

		private void OnDisable()
		{
			EndStroke();
		}
	}
}
