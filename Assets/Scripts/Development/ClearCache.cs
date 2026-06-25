using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Development
{
	public class ClearCache : MonoBehaviour
	{
		public void DebugClearCache()
		{
			Caching.ClearCache();
			Debug.Log("Clearing cache");
		}
	}
}
