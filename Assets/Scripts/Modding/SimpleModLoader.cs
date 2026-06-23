using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CollabXR.ModPackager;
using CollabXR.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CollabXR.ModLoader
{
	public class SimpleModLoader : SingletonBehavior<SimpleModLoader>
	{
		private const string DEBUG_LOG_HEADER = "<color=#aaffaa>[Simple Mod Loader]</color>";

		private void Start()
		{
			RepositoryManager.Instance.repositoriesRefreshed += RefreshSimpleMods;
		}

		public void RefreshSimpleMods()
		{
			Debug.Log($"{DEBUG_LOG_HEADER} Refreshing simple mod loader!");
			MainLibraryRef.Instance.ClearData();
			LoadArbitraryMods();
		}

		private void LoadArbitraryMods()
		{
			foreach (KeyValuePair<Guid, Tuple<ModMetadata, string>> mod in ModManager.Instance.indexedMods)
			{
				try
				{
					ModMetadata metadata = mod.Value.Item1;
					string platform = ModManager.GetPlatformString();
					int availableVersion = metadata.BuildNumberMap.ContainsKey(platform) ? metadata.BuildNumberMap[platform] : 0;
					Debug.Log($"{DEBUG_LOG_HEADER} Checking mod {mod.Key} @v{availableVersion}");
					bool bundleExists = availableVersion > 0;
					foreach (KeyValuePair<Guid, ModPrefab> prefab in metadata.PrefabMap)
					{
						Debug.Log($"{DEBUG_LOG_HEADER} Found asset {prefab.Key}: {prefab.Value.Category}/{prefab.Value.FormattedName}");
						MainLibraryRef.Instance.AddData(mod.Key, prefab.Key, prefab.Value, metadata, bundleExists);
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"{DEBUG_LOG_HEADER} Error loading arbitrary mod: {e}");
				}
			}
		}
	}
}
