using UnityEngine;

namespace CollabXR
{
	/// <summary>
	/// Estimates a held object's linear and angular velocity from its recent poses
	/// so it can be thrown with the motion it had when released.
	/// </summary>
	public class ThrowVelocityEstimator
	{
		private readonly Vector3[] positions;
		private readonly Quaternion[] rotations;
		private readonly float[] times;
		private int next;
		private int count;

		public ThrowVelocityEstimator(int windowSize)
		{
			windowSize = Mathf.Max(2, windowSize);
			positions = new Vector3[windowSize];
			rotations = new Quaternion[windowSize];
			times = new float[windowSize];
		}

		public void Clear()
		{
			next = 0;
			count = 0;
		}

		public void AddSample(Vector3 position, Quaternion rotation, float time)
		{
			positions[next] = position;
			rotations[next] = rotation;
			times[next] = time;
			next = (next + 1) % positions.Length;
			count = Mathf.Min(count + 1, positions.Length);
		}

		/// <summary>
		/// Average velocities across the sampled window measured at the sampled transform's origin.
		/// </summary>
		/// <returns>False if there are not enough samples to estimate from.</returns>
		public bool TryGetVelocity(out Vector3 linear, out Vector3 angular)
		{
			linear = Vector3.zero;
			angular = Vector3.zero;

			if (count < 2)
				return false;

			int newest = (next - 1 + positions.Length) % positions.Length;
			int oldest = (next - count + positions.Length) % positions.Length;
			float elapsed = times[newest] - times[oldest];

			if (elapsed <= Mathf.Epsilon)
				return false;

			linear = (positions[newest] - positions[oldest]) / elapsed;

			Quaternion delta = rotations[newest] * Quaternion.Inverse(rotations[oldest]);
			delta.ToAngleAxis(out float angle, out Vector3 axis);

			if (angle > 180f)
				angle -= 360f;

			if (!float.IsInfinity(axis.x) && !float.IsNaN(axis.x))
				angular = axis * (angle * Mathf.Deg2Rad / elapsed);

			return true;
		}
	}
}
