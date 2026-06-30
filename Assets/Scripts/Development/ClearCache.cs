using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Development
{
	public class ClearCache : MonoBehaviour
	{
		public void DebugClearCache()
		{
			AssetBundle.UnloadAllAssetBundles(true);
			Debug.Log($"Attempting to clear cache, result: {Caching.ClearCache()}");
		}
	}
}
