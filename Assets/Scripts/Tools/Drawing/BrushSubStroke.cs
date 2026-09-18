using System;
using CollabXR.Networking;
using CollabXR.Objects;
using Fusion;
using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	public class BrushSubStroke : SpawnableObject
	{
		private RibbonMesh strokeMesh;

		[SerializeField]
		[Networked, OnChangedRender(nameof(OnPointsChanged)), Capacity(128)]
		private NetworkLinkedList<Vector3> ribbonPoints => default;

		[Networked, Capacity(128)]
		private NetworkLinkedList<Vector3> ribbonEulerAngles => default;

		[Networked]
		private float ribbonWeight { get; set; }

		[Networked, Capacity(128)]
		private NetworkLinkedList<Color32> ribbonColors => default;

		private NetworkObject intendedParent;

		private void Awake()
		{
			strokeMesh = GetComponent<RibbonMesh>();
		}

		//public void LateUpdate()
		//{
		//	if (dirty)
		//	{
		//		UpdateStrokeRenderer();
		//		dirty = false;
		//	}
		//}

		//public void SetDirty()
		//{
		//	dirty = true;
		//}

		public override void Spawned()
		{
			base.Spawned();
			UpdateStrokeRenderer();
			gameObject.name = "Stroke " + gameObject.GetInstanceID();
		}

		public void UpdateStrokeRenderer()
		{
			strokeMesh.ClearRibbon();
			int verts = ribbonPoints.Count;
			for (int i = strokeMesh.PointCount; i < verts; i++)
			{
				strokeMesh.AddRibbonPoint(ribbonPoints[i], Quaternion.Euler(ribbonEulerAngles[i]), ribbonWeight, ribbonColors[i]);
			}
			strokeMesh.UpdateGeometry();

			if (verts < 1)
			{
				return;
			}

			if (intendedParent != null)
			{
				transform.parent = intendedParent.transform;
			}
		}

		public int GetCapacityRemaining()
		{
			return ribbonPoints.Capacity - ribbonPoints.Count;
		}

		public void Init(float weight)
		{
			if (!Object.HasStateAuthority)
			{
				return;
			}

			ribbonWeight = weight;
		}

		public void AddStrokePoint(Vector3 point, Quaternion rotation, Color color)
		{
			Vector3 localPoint = transform.InverseTransformPoint(point);
			ribbonPoints.Add(localPoint);
			ribbonEulerAngles.Add(rotation.eulerAngles);
			ribbonColors.Add(color);

			//SetDirty();
			UpdateStrokeRenderer();
		}

		public void SetLastPoint(Vector3 point, Quaternion rotation)
		{
			Vector3 localPoint = transform.InverseTransformPoint(point);
			int count = Mathf.Min(ribbonPoints.Count, ribbonEulerAngles.Count);
			if (count < 2)
				return;

			ribbonPoints.Set(count - 1, localPoint);
			ribbonEulerAngles.Set(count - 1, rotation.eulerAngles);
		}

		private void OnPointsChanged()
		{
			UpdateStrokeRenderer();
		}

		public void SetParent(NetworkObject parent)
		{
			intendedParent = parent;
		}

		public override void MarkForDeletion()
		{
			base.MarkForDeletion();
			SpawnableObject parentContainer = transform.parent.GetComponent<SpawnableObject>();
			parentContainer?.MarkForDeletion();
		}
	}
}
