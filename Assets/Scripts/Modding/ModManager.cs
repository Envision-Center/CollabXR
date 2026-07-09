// using dotnow.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CollabXR.ModPackager;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using NUnit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace CollabXR.ModLoader
{
	internal class LoadedModsTableEntry
	{
		internal AssetBundle AssetBundle = null;
		internal List<ModLoadTask> ModLoadTasks = new();
		internal Dictionary<Guid, Dictionary<string, Type>> ScriptRehydrationMap = new();
	}

	/// <summary>
	/// Stores the loaded asset in memory and a list of tasks that are waiting for it to be loaded.
	/// </summary>
	/// <remarks>
	/// Asset Pointer Table Entry -> APTE
	/// Asset Pointer Load Tasks -> APLT
	/// 
	/// When a new request for an asset is made, if an APTE already exists the APLT is added to the list.
	/// Once any of the APLT's finish, the value is passed to APTE.Value and all APLT's are notified that the asset is ready.
	/// 
	/// The whole pipeline for how APTEs are used can be found in ModManager.LoadAssetFromMod().
	/// </remarks>
	internal class AssetPointerTableEntry
	{
		/// <summary>
		/// The loaded asset in memory. Is null if asset still loading, otherwise contains the loaded asset.
		/// Once Value is set, it is never changed.
		/// </summary>
		internal object Value = null;
		internal List<IAssetPointerLoadTask> AssetPointerLoadTasks = new();
	}

	public class ModManager : SingletonBehavior<ModManager>
	{
		private const string DEBUG_LOG_HEADER = "<color=#a557ff>[Mod Loader]</color>";

		/// <summary>
		/// Maintains a list of all indexed mods/asset bundles, mapping their GUIDs to their metadata and URL.
		/// Is cleared and rebuilt on RepositoryManager.RefreshAllMods().
		/// </summary>
		public Dictionary<Guid, Tuple<ModMetadata, string>> indexedMods { get; private set; } = new(); // guid - <metadata, url>

		/// <summary>
		/// Similar kind of structure to assetPointerTable below, but for mods instead of assets.
		/// Maps mod UUIDs to the loaded asset bundle and a list of tasks that are waiting for it to be loaded.
		/// Created directly in ModManager.LoadMod() and destroyed in ModManager.TryUnloadMod().
		/// </summary>
		private Dictionary<Guid, LoadedModsTableEntry> loadedMods = new();

		/// <summary>
		/// Maps asset UUIDs to the APTE for that specific asset.
		/// Main purpose is to manage pulling assets from the loaded mod asset bundle.
		/// Created during ModManager.LoadAssetFromMod() and destroyed during ModManager.TryUnloadAssetFromMod().
		/// </summary>
		private Dictionary<Guid, AssetPointerTableEntry> assetPointerTable = new();

		/// <summary>
		/// Maintains a list of all instances of an asset present in the current room.
		/// Is added to by LoadAsset, and removed from by ReleaseAsset and TryUnloadMod (but this one's only ever called under ReleaseAsset AFAIK).
		/// Often used to check if any instances of an asset are still present in the room.
		/// </summary>
		private Dictionary<Guid, IAssetReference> assetReferences = new();

		/// <summary>
		/// Maintains a list of all UnityWebRequests that are currently loading asset bundles from remote repositories.
		/// Doesn't seem like they get removed from the list even when finished, could this cause bugs?
		/// </summary>
		private Dictionary<Uri, UnityWebRequest> modLoadingRequests = new();

		protected override void Awake()
		{
			base.Awake();

			DontDestroyOnLoad(this);
		}

		public static string GetPlatformString()
		{
#if UNITY_EDITOR_WIN
			return "StandaloneWindows64";
#elif UNITY_STANDALONE_WIN
			return "StandaloneWindows64";
#elif UNITY_EDITOR_OSX
			return "StandaloneOSX";
#elif UNITY_STANDALONE_OSX
			return "StandaloneOSX";
#elif UNITY_ANDROID
			return "Android";
#elif UNITY_VISIONOS
			return "VisionOS";
#else
			throw new Exception("Modding isn't supported on the current platform!");
#endif
		}

		private Uri GetAssetBundleURI(RepositoryMetadata repository, Guid modUuid)
		{
			return new Uri(new Uri(repository.BaseURL), $"{modUuid.ToString()}.{GetPlatformString()}");
		}

		private Uri GetAssetBundleMetaURI(RepositoryMetadata repository, Guid modUuid)
		{
			return new Uri(new Uri(repository.BaseURL), $"{modUuid.ToString()}.meta.json");
		}

		private static UnityWebRequest SignAWSWebRequest(UnityWebRequest request, RepositoryMetadata repository)
		{
			if (repository.accessKey != null && repository.secretKey != null) // signing only if they were successfully parsed from uri
			{
				string date = DateTime.UtcNow.ToString("r");
				string stringToSign = $"GET\n\n\n{date}\n/{repository.S3BucketName}{request.uri.AbsolutePath}";
				string signature = HMACSHA1_Signature(stringToSign, repository.secretKey);

				request.SetRequestHeader("Date", date);
				request.SetRequestHeader("Authorization", $"AWS {repository.accessKey}:{signature}");
			}
			return request;
		}

		private static string HMACSHA1_Signature(string msg, string key)
		{
			HMACSHA1 hmac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
			return Convert.ToBase64String(hmac.ComputeHash(Encoding.ASCII.GetBytes(msg)));
		}

		private static UnityWebRequest GenerateAWSWebRequest(Uri uri, RepositoryMetadata repository)
		{
			UnityWebRequest request = UnityWebRequest.Get(uri);
			return SignAWSWebRequest(request, repository);
		}

		private static UnityWebRequest GenerateAWSWebRequestAssetBundle(Uri uri, RepositoryMetadata repository, uint version)
		{
			UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(uri, version, 0);
			return SignAWSWebRequest(request, repository);
		}

		public float TryGetAssetBundleDownloadProgress(Guid modUuid)
		{
			UnityWebRequest request = TryGetUnityWebRequest(modUuid);
			if (request != null)
			{
				return request.downloadProgress;
			}
			return 0;
		}

		public UnityWebRequest TryGetUnityWebRequest(Guid modUuid)
		{
			Uri uri = GetAssetBundleURI(RepositoryManager.Instance.loadedRepositories[indexedMods[modUuid].Item2], modUuid);
			if (modLoadingRequests.ContainsKey(uri))
			{
				return modLoadingRequests[uri];
			}
			return null;
		}

		internal async UniTask IndexMod(string repository, Guid modUuid)
		{
			await UniTask.SwitchToMainThread();

			RepositoryMetadata repoData = RepositoryManager.Instance.loadedRepositories[repository];
			UnityWebRequest repositoryRequest = GenerateAWSWebRequest(GetAssetBundleMetaURI(repoData, modUuid), repoData);

			try
			{
				await repositoryRequest.SendWebRequest();
			}
			catch (Exception ex)
			{
				Debug.Log($"Broken web request on requesting uuid: {modUuid}");
				return;
			}

			try
			{
				string downloadedText = repositoryRequest.downloadHandler.text;

				ModMetadata metadata = JsonConvert.DeserializeObject<ModMetadata>(downloadedText);

				Instance.indexedMods[modUuid] = new Tuple<ModMetadata, string>(metadata, repository);

				Debug.Log(
					$"{DEBUG_LOG_HEADER} Loaded Metadata for Mod {modUuid}: {metadata.Name} V{metadata.BuildNumberMap[GetPlatformString()]} made by: {string.Join(", ", metadata.Creators)} (has {metadata.AssetMap.Count} assets, {metadata.PrefabMap.Count} prefabs)"
				);

				List<UniTask> reloadModsTasks = new List<UniTask>();
				reloadModsTasks.Add(ReloadMod(modUuid));

				//await UniTask.SwitchToThreadPool();

				await UniTask.WhenAll(reloadModsTasks);

				//await UniTask.SwitchToMainThread();
			}
			catch (Exception e)
			{
				Uri uri = GetAssetBundleURI(RepositoryManager.Instance.loadedRepositories[indexedMods[modUuid].Item2], modUuid);
				Debug.Log($"{DEBUG_LOG_HEADER} Failed to load Metadata for Mod {modUuid}, uri: {uri}: {e}.");
			}
		}

		internal void DeIndexAllMods()
		{
			Instance.indexedMods.Clear();
		}

		// Layer 1 of Abstraction

		/// <summary>
		/// Same kind of design/purpose/structure as LoadAssetFromMod, 
		/// but for mods instead of assets, and instead of querying the loaded asset bundle for an asset,
		/// queries AWS S3 for the mod asset bundle itself.
		/// </summary>
		/// <param name="modLoadTask"></param>
		/// <exception cref="Exception"></exception>
		internal void LoadMod(ModLoadTask modLoadTask)
		{
			Guid modUuid = modLoadTask.modUuid;

			if (!indexedMods.ContainsKey(modUuid))
			{
				throw new Exception($"Mod with UUID {modUuid} not found");
			}

			if (Instance.loadedMods.ContainsKey(modUuid))
			{
				if (Instance.loadedMods[modUuid].AssetBundle == null)
				{
					// Batch pending requests
					Instance.loadedMods[modUuid].ModLoadTasks.Add(modLoadTask);
				}
				else
				{
					// Immediately confirm it's ready
					modLoadTask.NotifyModReady();
				}
			}
			else
			{
				// Create new request
				Instance.loadedMods.Add(modUuid, new LoadedModsTableEntry());
				Instance.loadedMods[modUuid].ModLoadTasks.Add(modLoadTask);

				Task.Run(async () =>
				{
					await UniTask.SwitchToMainThread();

					RepositoryMetadata repoData = RepositoryManager.Instance.loadedRepositories[indexedMods[modUuid].Item2];
					Uri uri = GetAssetBundleURI(repoData, modUuid);
					UnityWebRequest request = GenerateAWSWebRequestAssetBundle(uri, repoData, (uint)indexedMods[modUuid].Item1.BuildNumberMap[GetPlatformString()]);

					modLoadingRequests[uri] = request;

					await request.SendWebRequest();

					Instance.loadedMods[modUuid].AssetBundle = DownloadHandlerAssetBundle.GetContent(request);

					Debug.Log($"{DEBUG_LOG_HEADER} Loaded Mod {modUuid} in to memory.");

					foreach (ModLoadTask loadTask in Instance.loadedMods[modUuid].ModLoadTasks)
					{
						loadTask.NotifyModReady();
					}

					Instance.loadedMods[modUuid].ModLoadTasks.Clear();
				});
			}
		}

		/// <summary>
		/// Completely reloads the given mod for the project from the remote repository.
		/// </summary>
		/// <param name="modUuid">The UUID of the mod to reload.</param>
		/// <returns></returns>
		/// <remarks>
		/// The pipeline:
		/// 1. Completely clear the mod/asset bundle from memory.
		/// 2. Request a new load of the mod/asset bundle from the remote repository.
		/// 3. Once the mod is loaded, update the assetPointerTable with the new asset references from the mod.
		/// 
		/// It does update the mod for every active instance of that mod in memory,
		/// so existing instances of the mod will be updated to the new asset bundle (client side only).
		/// 
		/// So it is possible for the same mod instance to be different between clients 
		/// if one client updates the mod and the other doesn't.
		/// </remarks>
		internal async UniTask ReloadMod(Guid modUuid)
		{
			if (!Instance.loadedMods.ContainsKey(modUuid))
			{
				return;
			}

			Instance.loadedMods[modUuid].AssetBundle.Unload(false);
			Instance.loadedMods.Remove(modUuid);
			Caching.ClearAllCachedVersions(modUuid.ToString());

			ModLoadTask modLoadTask = new ModLoadTask(modUuid);
			Guid loadedModGuid = await modLoadTask;
			Debug.Assert(loadedModGuid == modUuid, $"Loaded mod guid {loadedModGuid} does not match requested mod guid {modUuid}");

			foreach (Guid updatingAssetPointer in Instance.assetPointerTable.Keys)
			{
				if (Instance.indexedMods[modUuid].Item1.AssetMap.ContainsKey(updatingAssetPointer))
				{
					object newAsset = await Instance.loadedMods[modUuid].AssetBundle.LoadAssetWithSubAssetsAsync(Instance.indexedMods[modUuid].Item1.AssetMap[updatingAssetPointer]);
					Instance.assetPointerTable[updatingAssetPointer].Value = newAsset;
				}
			}

			Debug.Log($"{DEBUG_LOG_HEADER} Reloaded Mod {modUuid} in memory.");
		}

		internal bool TryUnloadMod(Guid modUuid)
		{
			if (!Instance.loadedMods.ContainsKey(modUuid))
			{
				return true;
			}

			bool safeToUnload = true;

			List<IAssetReference> assetReferenceEnumerator = Instance.assetReferences.Values.ToList();

			foreach (IAssetReference assetReferenceChecking in assetReferenceEnumerator)
			{
				if (assetReferenceChecking.modUuid == modUuid)
				{
					if (assetReferenceChecking.InvokeOnRequestReleaseEvent())
					{
						Guid assetUuid = assetReferenceChecking.assetUuid;

						Instance.assetReferences.Remove(assetReferenceChecking.assetReferenceUuid);
					}
					else
					{
						safeToUnload = false;
					}
				}
			}

			List<Guid> assetPointers = Instance.assetPointerTable.Keys.ToList();

			foreach (IAssetReference checkingAssetUuid in Instance.assetReferences.Values)
			{
				if (assetPointers.Contains(checkingAssetUuid.assetUuid))
				{
					assetPointers.Remove(checkingAssetUuid.assetUuid);
				}
			}

			foreach (Guid removingAssetPointer in assetPointers)
			{
				if (Instance.assetPointerTable.ContainsKey(removingAssetPointer))
				{
					Instance.assetPointerTable.Remove(removingAssetPointer);
				}
			}

			foreach (Guid checkingAssetPointer in Instance.assetPointerTable.Keys)
			{
				if (Instance.indexedMods[modUuid].Item1.AssetMap.ContainsKey(checkingAssetPointer))
				{
					safeToUnload = false;

					break;
				}
			}

			if (safeToUnload)
			{
				Instance.loadedMods[modUuid].AssetBundle.UnloadAsync(true);
				Instance.loadedMods.Remove(modUuid);

				Debug.Log($"{DEBUG_LOG_HEADER} Unloaded Mod {modUuid} from memory.");
			}

			return safeToUnload;
		}

		// Layer 2 of Abstraction

		/// <summary>
		/// Loads the asset from the mod for the given asset pointer load task.
		/// If the mod is already loaded, the asset will be loaded immediately.
		/// </summary>
		/// <param name="assetPointerLoadTask">The task representing a new asset load request.</param>
		/// <remarks>
		/// Basically there are 3 cases being handled here:
		/// 
		/// 1. An AssetPointerTableEntry (APTE) already exists, and the asset is NOT loaded. 
		/// The task is added to the list of tasks waiting for the asset to be loaded.
		/// 
		/// 2. An APTE already exists, and the asset IS loaded. 
		/// The task is immediately notified that the asset is ready.
		/// 
		/// 3. An APTE does NOT exist. Here, a new APTE is created and the mod is loaded directly. 
		/// Once the mod is loaded, the asset is loaded and all tasks waiting for it are notified.
		/// NOTE that in the 3rd case, a ModLoadTask is created, which calls ModManager.LoadMod. 
		/// That handles the remote repository -> Asset Bundle stage.
		/// </remarks>
		internal void LoadAssetFromMod(IAssetPointerLoadTask assetPointerLoadTask)
		{
			Guid modUuid = assetPointerLoadTask.assetReference.modUuid;
			Guid assetUuid = assetPointerLoadTask.assetReference.assetUuid;

			if (!indexedMods.ContainsKey(modUuid))
			{
				throw new Exception($"Mod with UUID {modUuid} not found");
			}

			if (!indexedMods[modUuid].Item1.AssetMap.ContainsKey(assetUuid))
			{
				throw new Exception($"Asset with UUID {assetUuid} not found on Mod with UUID {modUuid}");
			}

			if (Instance.assetPointerTable.ContainsKey(assetUuid))
			{
				if (Instance.assetPointerTable[assetUuid].Value == null)
				{
					// Batch pending requests
					Instance.assetPointerTable[assetUuid].AssetPointerLoadTasks.Add(assetPointerLoadTask);
				}
				else
				{
					// Immediately send back object
					assetPointerLoadTask.assetReference.value = Instance.assetPointerTable[assetUuid].Value;

					assetPointerLoadTask.NotifyAssetReady();
				}
			}
			else
			{
				// Create new request
				Instance.assetPointerTable.Add(assetUuid, new AssetPointerTableEntry());
				Instance.assetPointerTable[assetUuid].AssetPointerLoadTasks.Add(assetPointerLoadTask);

				Task.Run(async () =>
				{
					ModLoadTask modLoadTask = new ModLoadTask(modUuid);

					Guid loadedModGuid = await modLoadTask;

					await UniTask.SwitchToMainThread();

					Instance.assetPointerTable[assetUuid].Value = await loadedMods[loadedModGuid].AssetBundle.LoadAssetWithSubAssetsAsync(indexedMods[loadedModGuid].Item1.AssetMap[assetUuid]);

					Debug.Log($"{DEBUG_LOG_HEADER} Loaded Asset {assetUuid} from Mod {modUuid} in to memory.");

					foreach (IAssetPointerLoadTask loadTask in Instance.assetPointerTable[assetUuid].AssetPointerLoadTasks)
					{
						loadTask.assetReference.value = Instance.assetPointerTable[assetUuid].Value;

						loadTask.NotifyAssetReady();
					}

					Instance.assetPointerTable[assetUuid].AssetPointerLoadTasks.Clear();
				});
			}
		}

		internal void TryUnloadAssetFromMod(Guid modUuid, Guid assetUuid)
		{
			bool safeToMarkGarbage = true;

			List<Guid> unloadableMods = Instance.loadedMods.Keys.ToList();

			foreach (IAssetReference assetReferenceChecking in Instance.assetReferences.Values)
			{
				if (assetReferenceChecking.assetUuid == assetUuid)
				{
					safeToMarkGarbage = false;

					break;
				}
			}

			if (safeToMarkGarbage)
			{
				if (Instance.assetPointerTable.ContainsKey(assetUuid))
				{
					Instance.assetPointerTable.Remove(assetUuid);
				}

				Debug.Log($"{DEBUG_LOG_HEADER} Unloaded Asset {assetUuid} from Mod {modUuid} from memory.");

				TryUnloadMod(modUuid);
			}
		}

		// Layer 3 of Abstraction

		/// <summary>
		/// This method creates a list of all the available assets inside a mod.
		/// Any asset listed by this will be valid to load with <c>LoadAsset()</c>
		/// </summary>
		public static Guid[] ListAssets(Guid modUuid)
		{
			if (Instance.indexedMods.ContainsKey(modUuid))
			{
				return Instance.indexedMods[modUuid].Item1.AssetMap.Keys.ToArray();
			}

			throw new Exception($"Mod with UUID ${modUuid} not found");
		}

		/// <summary>
		/// This method creates a dictionary of prefabs from a mod including details not available in the assets.
		/// Any prefab listed by this will be valid to load with <c>LoadAsset()</c> and is guaranteed to be of type GameObject when loaded
		/// </summary>
		public static Dictionary<Guid, ModPrefab> ListPrefabTable(Guid modUuid)
		{
			if (Instance.indexedMods.ContainsKey(modUuid))
			{
				return Instance.indexedMods[modUuid].Item1.PrefabMap;
			}

			throw new Exception($"Mod with UUID ${modUuid} not found");
		}

		/// <summary>
		/// This method creates an <c>AssetReference</c> with the type specified, loading any mods and assets as needed.
		/// Remember: be sure to call <c>ReleaseAsset()</c> with the <c>AssetReference</c> created by this method or the game will leak memory.
		/// Note: Be sure to specify the correct type otherwise some silly errors can occur.
		/// </summary>
		/// 
		/// <typeparam name="T">The type of the asset to load. Must match the type of the asset in the mod.</typeparam>
		/// <param name="modUuid">The UUID of the mod/asset bundle to load the asset from.</param>
		/// <param name="assetUuid">The UUID of the asset to load.</param>
		/// 
		/// <remarks>
		/// This is the uppermost layer of abstraction for loading assets from mods, AKA the first function to be called.
		/// Expects the newly created asset reference to return the correct type of asset when loaded.
		/// 
		/// newAssetReference.LoadSelf() creates an AssetPointerLoadTask which is passed to ModManager.LoadAssetFromMod.
		/// So the immediate next lower layer of abstraction is at ModManager.LoadAssetFromMod, which should eventually pass
		/// the actual asset back to this method through the AssetReference.
		/// </remarks>
		public static async UniTask<AssetReference<T>> LoadAsset<T>(Guid modUuid, Guid assetUuid)
		{
			AssetReference<T> newAssetReference = new AssetReference<T>
			{
				modUuid = modUuid,
				assetUuid = assetUuid,
				assetReferenceUuid = Guid.NewGuid(),
			};

			await RepositoryManager.Instance;

			Instance.assetReferences.Add(newAssetReference.assetReferenceUuid, newAssetReference);

			return await newAssetReference.LoadSelf();
		}

		/// <summary>
		/// This method releases an <c>AssetReference</c> given by the mod loader and unloads everything necessary to minimize memory footprint.
		/// </summary>
		public static void ReleaseAsset<T>(AssetReference<T> assetReference)
		{
			Guid modUuid = assetReference.modUuid;
			Guid assetUuid = assetReference.assetUuid;

			if (Instance.assetReferences.ContainsKey(assetReference.assetReferenceUuid))
			{
				Instance.assetReferences.Remove(assetReference.assetReferenceUuid);
			}

			Instance.TryUnloadAssetFromMod(modUuid, assetUuid);
		}
	}
}
