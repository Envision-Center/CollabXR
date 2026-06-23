using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Development
{
	public class ClearCache : MonoBehaviour
	{
		public bool shouldClear; // just for debugging

		void Awake()
		{
			if (shouldClear)
			{
				Debug.Log("Cache clear success = " + Caching.ClearCache());
			}
		}
	}
}
