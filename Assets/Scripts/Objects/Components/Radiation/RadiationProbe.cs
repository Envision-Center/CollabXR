using System;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects.Components.Radiation
{
	public class RadiationProbe : NetworkBehaviour
	{
		public float surfaceRadius = 0.1f;

		[SerializeField]
		private Transform detectionPointTransform;

		public Action<float> OnDetect = delegate { };

		private void Awake()
		{
			if (detectionPointTransform == null)
				detectionPointTransform = transform;
		}

		public float Detect()
		{
			float countsPerMinute = 0;

			foreach (RadiationSource source in RadiationSource.AllSources)
			{
				Vector3 sourceDistance = source.transform.position - detectionPointTransform.position;

				Vector3 sourceDirection = sourceDistance.normalized;

				float facing = Vector3.Dot(detectionPointTransform.forward, sourceDirection);
				facing = Mathf.Max(facing, 0f);
				facing = Mathf.Sin(facing * Mathf.PI / 2f);

				float mag = Mathf.Max(sourceDistance.magnitude, 0.0001f);

				countsPerMinute += source.intensity / (mag * mag) * facing;
			}

			return countsPerMinute;
		}
	}
}
