using CollabXR.ModLoader;
using UnityEngine;

namespace CollabXR.Development
{
	public class ClearCache : MonoBehaviour
	{
		// Toggled false while in room to preserve objects already loaded from assetbundles.
		public void DebugClearCache(bool unloadAllObjects)
		{
			foreach (var guid in ModManager.Instance.indexedMods.Keys)
			{
				ModManager.ClearModAssetCache(guid, unloadAllObjects);
			}
			AssetBundle.UnloadAllAssetBundles(unloadAllObjects);
			Debug.Log($"Attempting to clear cache, result: {(Caching.ClearCache() ? "Succesfful" : "Failed")}");
		}
	}
}
