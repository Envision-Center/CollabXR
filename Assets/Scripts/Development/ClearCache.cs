using UnityEngine;

namespace CollabXR.Development
{
	public class ClearCache : MonoBehaviour
	{
		// Toggled false while in room, will break object loading otherwise.
		public void DebugClearCache(bool unloadAllObjects)
		{
			AssetBundle.UnloadAllAssetBundles(unloadAllObjects);
			Debug.Log($"Attempting to clear cache, result: {Caching.ClearCache()}");
		}
	}
}
