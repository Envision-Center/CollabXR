using System;
using CollabXR.Networking;
using CollabXR.Objects;
using Fusion;
using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	public class BrushSubStroke : SpawnableObject, IStrokeWordStore
	{
		private RibbonMesh strokeMesh;

		// every point's position, rotation and color lives compressed in here
		// Fusion reserves the full capacity per spawned object however little the stroke holds, so every substroke pays it
		[Networked, OnChangedRender(nameof(OnPointsChanged)), Capacity(BrushStrokeSchema.StreamWords)]
		private NetworkArray<int> packedStrokeWords => default;

		private readonly PackedStroke<BrushStrokeSchema> packedStroke = new(BrushStrokeSchema.StreamWords);

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
			packedStroke.Load(this);
			BrushStrokeSchema stroke = packedStroke.Schema;

			strokeMesh.ClearRibbon();
			int verts = stroke.PointCount;
			for (int i = strokeMesh.PointCount; i < verts; i++)
			{
				strokeMesh.AddRibbonPoint(stroke.Positions.Values[i], Quaternion.Euler(stroke.EulerAngles.Values[i]), stroke.Weight.Value, stroke.Colors.Values[i]);
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
			packedStroke.Load(this);
			return packedStroke.CapacityRemaining;
		}

		public void Init(float weight)
		{
			if (!Object.HasStateAuthority)
			{
				return;
			}

			packedStroke.Load(this);
			packedStroke.Schema.Weight.Value = weight;
			packedStroke.Save(this);
		}

		public void AddStrokePoint(Vector3 point, Quaternion rotation, Color color)
		{
			packedStroke.Load(this);
			if (packedStroke.CapacityRemaining == 0)
			{
				Debug.LogWarning($"{gameObject.name} is full, dropping stroke point");
				return;
			}

			Vector3 localPoint = transform.InverseTransformPoint(point);
			packedStroke.Schema.AddPoint(localPoint, ToLocalEulerAngles(rotation), color);
			packedStroke.Save(this);

			//SetDirty();
			UpdateStrokeRenderer();
		}

		public void SetLastPoint(Vector3 point, Quaternion rotation)
		{
			Vector3 localPoint = transform.InverseTransformPoint(point);
			packedStroke.Load(this);
			if (packedStroke.Schema.PointCount < 2)
				return;

			packedStroke.Schema.SetLastPoint(localPoint, ToLocalEulerAngles(rotation));
			packedStroke.Save(this);
		}

		private Vector3 ToLocalEulerAngles(Quaternion rotation)
		{
			return (Quaternion.Inverse(transform.rotation) * rotation).eulerAngles;
		}

		int IStrokeWordStore.Length => packedStrokeWords.Length;

		int IStrokeWordStore.Get(int index)
		{
			return packedStrokeWords.Get(index);
		}

		void IStrokeWordStore.Set(int index, int value)
		{
			packedStrokeWords.Set(index, value);
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
