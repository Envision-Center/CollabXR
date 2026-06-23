using UnityEngine;

namespace CollabXR.Objects.Components
{
	public class GaugeNeedle : MonoBehaviour
	{
		public float value;
		public float scale = 1;
		public float minValue;
		public float maxValue = 100;

		public float minAngle;
		public float maxAngle = 90;
		public float lerp = 0.5f;

		[SerializeField]
		private Transform visualTransform;

		[SerializeField]
		private Vector3 rotationAxis = new(0, 0, 1);

		private float _needleAngle;

		private void Awake()
		{
			if (visualTransform == null)
				visualTransform = transform;
		}

		private void Update()
		{
			float scaledValue = Mathf.Clamp(scale * value, minValue, maxValue);
			float targetAngle = (scaledValue - minValue) / (maxValue - minValue) * (maxAngle - minAngle) + minAngle;

			_needleAngle = Mathf.Lerp(_needleAngle, targetAngle, lerp * Time.deltaTime * 60);

			visualTransform.localRotation = Quaternion.AngleAxis(_needleAngle, rotationAxis);
		}

		public void SetValue(float f)
		{
			value = f;
		}

		public void SetScale(float f)
		{
			scale = f;
		}
	}
}
