using System.Collections.Generic;
using CollabXR.Cycles;
using UnityEngine;
using UnityEngine.Splines;

namespace CollabXR
{
    public class SolarSystem : MonoBehaviour
    {
		public NetworkPlaybackDirector director;
		public List<CelestialBody> bodies;
        
        // Update is called once per frame
        void Update()
        {
			foreach (CelestialBody b in bodies)
			{
				b.SetNormalizedTime(director.Anchor.AnchorPercent);
			}
        }
    }
}
