using UnityEngine;

namespace CollabXR
{
	/// <summary>
	/// Estimates a held object's linear and angular velocity from its recent poses
	/// so it can be thrown with the motion it had when released.
	/// </summary>
	public class ThrowVelocityEstimator
	{
		/// <summary>
		/// Enough samples to cover a half second window at 120 Hz.
		/// </summary>
		private const int CAPACITY = 64;

		private readonly Vector3[] positions = new Vector3[CAPACITY];
		private readonly Quaternion[] rotations = new Quaternion[CAPACITY];
		private readonly float[] times = new float[CAPACITY];
		private int next;
		private int count;

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
			next = (next + 1) % CAPACITY;
			count = Mathf.Min(count + 1, CAPACITY);
		}

		/// <summary>
		/// Velocities over the last <paramref name="window"/> seconds, weighted toward the newest motion at the sampled transform's origin
		/// </summary>
		/// <returns>False if there are not enough samples to estimate from.</returns>
		public bool TryGetVelocity(float window, out Vector3 linear, out Vector3 angular)
		{
			linear = Vector3.zero;
			angular = Vector3.zero;

			if (count < 2)
				return false;

			float newestTime = times[IndexFromNewest(0)];
			Vector3 linearSum = Vector3.zero;
			Vector3 angularSum = Vector3.zero;
			float weightSum = 0f;

			for (int i = 0; i < count - 1; i++)
			{
				int to = IndexFromNewest(i);
				int from = IndexFromNewest(i + 1);
				float age = newestTime - times[to];

				// Always use the newest step
				if (i > 0 && age > window)
					break;

				float dt = times[to] - times[from];

				if (dt <= Mathf.Epsilon)
					continue;

				float weight = Mathf.Max(0.05f, 1f - age / window);
				linearSum += weight * (positions[to] - positions[from]) / dt;
				angularSum += weight * AngularVelocity(rotations[from], rotations[to], dt);
				weightSum += weight;
			}

			if (weightSum <= Mathf.Epsilon)
				return false;

			linear = linearSum / weightSum;
			angular = angularSum / weightSum;
			return true;
		}

		private int IndexFromNewest(int stepsBack)
		{
			return (next - 1 - stepsBack + CAPACITY * 2) % CAPACITY;
		}

		private static Vector3 AngularVelocity(Quaternion from, Quaternion to, float dt)
		{
			Quaternion delta = to * Quaternion.Inverse(from);
			delta.ToAngleAxis(out float angle, out Vector3 axis);

			if (angle > 180f)
				angle -= 360f;

			if (float.IsInfinity(axis.x) || float.IsNaN(axis.x))
				return Vector3.zero;

			return axis * (angle * Mathf.Deg2Rad / dt);
		}
	}
}
