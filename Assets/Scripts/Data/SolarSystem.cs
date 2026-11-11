using System;
using System.Collections.Generic;
using CollabXR.Cycles;
using TMPro;
using UnityEngine;
using UnityEngine.Splines;

namespace CollabXR
{
    public class SolarSystem : MonoBehaviour
    {
		public NetworkPlaybackDirector director;
		public List<CelestialBody> bodies;
		public TextMeshProUGUI timeDisplay;

		private DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0);
		private const float daysInAnimation = 90560; // arbitrary cap to the orbital animation representing one orbit of Pluto

		// Update is called once per frame
		void Update()
		{
			float daysSinceJ2000 = director.Anchor.AnchorPercent * daysInAnimation;
			foreach (CelestialBody b in bodies)
			{

				b.SetNormalizedTime(daysSinceJ2000);
			}
			DateTime currentTime = J2000.AddDays(daysSinceJ2000);
			timeDisplay.text = currentTime.ToString("f");
        }
    }
}
