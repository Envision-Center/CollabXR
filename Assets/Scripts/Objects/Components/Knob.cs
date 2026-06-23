using System;
using CollabXR.Input;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CollabXR.Objects.Components
{
	[RequireComponent(typeof(HandInput))]
	public class Knob : NetworkBehaviour
	{
		[SerializeField]
		private float minAngle;

		[SerializeField]
		private float maxAngle = 180;

		[SerializeField]
		private int initialIndex;

		[SerializeField]
		private float[] values;

		[SerializeField]
		private bool counterClockwise;

		[FormerlySerializedAs("OnValueChange")]
		public UnityEvent<float> onValueChange;

		[SerializeField]
		private Transform visualTransform;

		[SerializeField]
		private float angleOffset;

		[SerializeField]
		private Vector3 rotationAxis;

		private float targetAngle;

		[Networked, OnChangedRender(nameof(OnSelectedIndexChanged))]
		private int SelectedIndex { get; set; }

		private void OnSelectedIndexChanged()
		{
			SetSelectedIndexAndRotation(SelectedIndex);
		}

		private void Awake()
		{
			if (visualTransform == null)
				visualTransform = transform;
		}

		public override void Spawned()
		{
			if (HasInputAuthority)
			{
				SelectedIndex = initialIndex;
			}
		}

		private void Update()
		{
			Quaternion targetRot = Quaternion.Euler((targetAngle + angleOffset) * rotationAxis);

			visualTransform.localRotation = Quaternion.Slerp(visualTransform.localRotation, targetRot, Time.deltaTime * 30);
		}

		public void AxisInput(Vector2 axis)
		{
			if (axis.magnitude > 0.9f)
			{
				float stickAngle = -Mathf.Atan2(axis.x, axis.y) * 360 / (Mathf.PI * 2);
				stickAngle = Mathf.Clamp(stickAngle, minAngle, maxAngle);

				int newIndex = Mathf.RoundToInt(values.Length * ((stickAngle - minAngle) / (maxAngle - minAngle)));

				newIndex = Math.Clamp(newIndex, 0, values.Length - 1);

				if (SelectedIndex != newIndex)
					SetSelectedIndexAndRotation(newIndex);
			}
		}

		public int GetIndex()
		{
			return SelectedIndex;
		}

		public void SetSelectedIndexAndRotation(int newIndex)
		{
			SelectedIndex = newIndex;

			int index = counterClockwise ? values.Length - 1 - SelectedIndex : SelectedIndex;

			onValueChange.Invoke(values[index]);

			targetAngle = SelectedIndex / ((float)values.Length - 1) * (maxAngle - minAngle) + minAngle;
		}
	}
}
