using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace CollabXR
{
    public class CelestialBody : MonoBehaviour
    {
		// mean orbital elements
		public float semimajor, periapsis; // distances in AU
		public float inclination, longitudePeriapsis, longitudeMean; // angles in degrees
		public float rightAscension, declination; // angles in degrees
		public float eccentricity; // unitless parameter
		public float orbitalPeriod; // time in days
		public float rotationalPeriod; // time in hours
		public float orbitVisScale = 1; // amount to multiply orbit scale by for visualization purposes
		public List<SplineAnimate> actors;
		public SplineContainer orbit;

		[SerializeField] private float daysSinceJ2000 = 0;

		private float startingDegreesPastPeriapsis = 0;

		private const float scaleAU = 0.5f; // AU to in-game units (meters) for visualization


		void Start()
        {
			float semiminor = semimajor * Mathf.Sqrt(1-(eccentricity*eccentricity)); // b = a*sqrt(1-e^2)
			float totalScalar = scaleAU * orbitVisScale * 2; // doubling semimajor to get full axis
			orbit.transform.localScale = new Vector3(semimajor, (semimajor+semiminor)/2, semiminor); 
			orbit.transform.localScale *= totalScalar;
			orbit.transform.localPosition = new Vector3((semimajor - periapsis)*scaleAU, 0, 0); // moving orbit to have accurate periapsis
			transform.Rotate(transform.up, -longitudePeriapsis, Space.World); // rotating orbit based on longitude of periapsis
			transform.Rotate(transform.forward, -inclination, Space.World); // moving orbit to correct inclination
			//actor.transform.Rotate(transform.forward, obliquity);
			startingDegreesPastPeriapsis = TrueModulo(longitudeMean - longitudePeriapsis, 360);
			foreach(SplineAnimate actor in actors)
			{
				actor.NormalizedTime = startingDegreesPastPeriapsis / 360;
			}
        }

		public void SetNormalizedTime(float daysSinceJ2000)
		{
			float rotationsSinceJ2000 = daysSinceJ2000 / orbitalPeriod;
			float currentDegrees = TrueModulo(rotationsSinceJ2000, 1)*360;
			foreach (SplineAnimate actor in actors)
			{
				actor.NormalizedTime = (currentDegrees + startingDegreesPastPeriapsis) / 360;
			}

		}

		private void Update()
		{
			Debug.DrawLine(transform.parent.position, actors[0].transform.position, Color.magenta);
			//periapsis debug stuff
			SplineUtility.GetNearestPoint(orbit.Spline, orbit.transform.InverseTransformPoint(transform.parent.position), out float3 nearest, out float t, resolution: 10, iterations: 5);
			nearest = orbit.transform.TransformPoint(nearest);
			Debug.DrawLine(transform.parent.position, new Vector3(nearest.x, nearest.y, nearest.z), Color.yellow);
		}

		static float TrueModulo(float a, float n)
		{
			return (a % n + n) % n;
		}
	}
}
