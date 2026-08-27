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
		[Networked, OnChangedRender(nameof(OnPointsChanged)), Capacity(256)]
		private NetworkLinkedList<Vector3> ribbonPoints => default;

		[Networked, Capacity(256)]
		private NetworkLinkedList<Vector3> ribbonEulerAngles => default;

		[Networked]
		private float ribbonWeight { get; set; }

		[Networked]
		private Color32 ribbonColor { get; set; }

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
				strokeMesh.AddRibbonPoint(ribbonPoints[i], Quaternion.Euler(ribbonEulerAngles[i]), ribbonWeight, ribbonColor);
			}
			if (verts < 1)
				return;

			if (intendedParent != null)
				transform.parent = intendedParent.transform;
		}

		public int GetCapacityRemaining()
		{
			return ribbonPoints.Capacity - ribbonPoints.Count;
		}

		public void Init(Color32 color, float weight)
		{
			if (!Object.HasStateAuthority)
			{
				return;
			}

			ribbonColor = color;
			ribbonWeight = weight;
		}

		public bool AddStrokePoint(Vector3 point, Quaternion rotation)
		{
			Vector3 localPoint = transform.InverseTransformPoint(point);
			ribbonPoints.Add(localPoint);
			ribbonEulerAngles.Add(rotation.eulerAngles);

			//SetDirty();
			UpdateStrokeRenderer();
			return true;
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
