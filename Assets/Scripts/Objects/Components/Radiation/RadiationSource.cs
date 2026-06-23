using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Objects.Components.Radiation
{
	public class RadiationSource : MonoBehaviour
	{
		public static readonly List<RadiationSource> AllSources = new();

		public float intensity = 1;

		// Start is called before the first frame update
		private void Start()
		{
			AllSources.Add(this);
		}

		private void OnDestroy()
		{
			AllSources.Remove(this);
		}
	}
}
