using System;
using System.Collections.Generic;
using CollabXR.ModExtras;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using CollabXR.ModPackager;
using CollabXR.ModPackager.EnvironmentExtras;
using CollabXR.ModLoader;

namespace CollabXR.Environments
{
	public struct EnvironmentSelection : INetworkStruct
	{
		public int CategoryIndex;
		public int EntryIndex;
	}

	public class EnvironmentManager : SingletonNetworkBehavior<EnvironmentManager>, IStateAuthorityChanged, IDespawned
	{
		///
		/// DATA STORAGE
		///

		/// <summary>
		/// Primary source of environment entry data
		/// </summary>
		EnvironmentLibrary environmentLibrary = new EnvironmentLibrary();
		
		/// <summary>
		/// Legacy built-in environment data scriptable objects
		/// </summary>
		[SerializeField] public EnvironmentData[] localEnvironmentData;

		///
		/// STATE VARIABLES
		///

		[Networked, OnChangedRender(nameof(LoadEnvironmentSceneLocal))]
		public EnvironmentSelection currentEnvironmentSelection { get; set; }
		private string currentEnvScenePath = "";
		private Guid currentEnvModGUID;
		[SerializeField] public EnvironmentScene currentEnvInstance;

		[Networked, OnChangedRender(nameof(Teleport))]
		public int currentTeleportIndex { get; set; }
		
		private Matrix4x4 previousTeleportMatrix = Matrix4x4.identity;
		private int requestedCategoryIndex;
		private int requestedEntryIndex;
		private int requestedTeleportIndex;
		private EnvironmentEntry selectedEntryBeforeRefresh;
		private bool loadingScene;
		private bool retryEnvironmentLoad;

		///
		/// EVENTS
		/// 

		public UnityEvent OnEnvironmentLoadComplete;
		public event Action OnEnvironmentLibraryChanged;
		public IReadOnlyList<EnvironmentCategory> EnvironmentCategories
		{
			get
			{
				return environmentLibrary.categories;
			}
		}
		public EnvironmentEntry GetEnvironmentEntry(int categoryIndex, int entryIndex)
		{
			return environmentLibrary.GetEntry(categoryIndex, entryIndex);
		}
		public EnvironmentEntry GetEnvironment()
		{
			int categoryIndex = currentEnvironmentSelection.CategoryIndex;
			int entryIndex = currentEnvironmentSelection.EntryIndex;
			if (categoryIndex < 0 || categoryIndex >= EnvironmentCategories.Count ||
				entryIndex < 0 || entryIndex >= EnvironmentCategories[categoryIndex].entries.Count)
			{
				return null;
			}
			return GetEnvironmentEntry(categoryIndex, entryIndex);
		}

		/// <summary>
		/// Overriding singleton initialization function
		/// Builds built-in entries and loads the selected environment.
		/// </summary>
		public override void Spawned()
		{
			base.Spawned();
			ResetEnvironmentLibrary();
			LoadEnvironmentSceneLocal();
		}

		private void SpawnNetworkedObjects(List<GameObject> objs)
		{
			foreach (GameObject obj in objs)
			{
				Runner.Spawn(obj, new Vector3(0, 0, 0), Quaternion.identity);
			}
		}

		/// <summary>
		/// Teleports player to location of current teleport index
		/// </summary>
		private void Teleport()
		{
			if (loadingScene || currentEnvInstance == null || currentTeleportIndex < 0 ||
				currentTeleportIndex >= currentEnvInstance.teleports.Length)
			{
				return;
			}
			Debug.Log($"Teleporting to {currentTeleportIndex} in {currentEnvInstance.name}");

			Transform t = currentEnvInstance.teleports[currentTeleportIndex].transform;

			Matrix4x4 localToPrevious = previousTeleportMatrix.inverse * HardwareRig.Instance.root.localToWorldMatrix;
			Matrix4x4 newMat = t.localToWorldMatrix * localToPrevious;

			HardwareRig.Instance.root.SetPositionAndRotation(newMat.GetPosition(), newMat.rotation);

			previousTeleportMatrix = t.localToWorldMatrix;
		}

		/// <summary>
		/// Main function that loads the current environment at "currentEnvScenePath".
		/// </summary>
		private async void LoadEnvironmentSceneLocal()
		{
			// flag to indicate if a scene is currently loading
			if (loadingScene)
			{
				return;
			}
			loadingScene = true;
			
			// 
			EnvironmentEntry entry;
			try
			{
				entry = GetEnvironment();
				if (entry == null || !entry.availableOnThisPlatform || string.IsNullOrEmpty(entry.scenePath))
				{
					throw new InvalidOperationException("Selected environment is unavailable or has no scene path.");
				}
				if (entry.modGUID != Guid.Empty)
				{
					await new ModLoadTask(entry.modGUID);
				}
			}
			catch (Exception exception)
			{
				loadingScene = false;
				retryEnvironmentLoad = true;
				Debug.LogError($"Failed to prepare environment scene: {exception}");
				return;
			}

			retryEnvironmentLoad = false;
			if (!ReferenceEquals(entry, GetEnvironment()))
			{
				loadingScene = false;
				if (GetEnvironment() != null)
				{
					LoadEnvironmentSceneLocal();
				}
				else
				{
					retryEnvironmentLoad = true;
				}
				return;
			}
			if (currentEnvInstance != null && currentEnvScenePath == entry.scenePath && currentEnvModGUID == entry.modGUID)
			{
				loadingScene = false;
				return;
			}

			if (!currentEnvScenePath.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvScenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currentEnvInstance = null;
			}

			currentEnvScenePath = entry.scenePath;
			currentEnvModGUID = entry.modGUID;
			Debug.Log($"Initializing loading of: {currentEnvScenePath}");
			AsyncOperation loadOperation;
			try
			{
				loadOperation = SceneManager.LoadSceneAsync(currentEnvScenePath, LoadSceneMode.Additive);
			}
			catch (Exception exception)
			{
				loadingScene = false;
				Debug.LogError($"Failed to load environment scene at {currentEnvScenePath}: {exception}");
				currentEnvScenePath = "";
				return;
			}
			if (loadOperation == null)
			{
				loadingScene = false;
				Debug.LogError($"Failed to start loading environment scene at {currentEnvScenePath}.");
				currentEnvScenePath = "";
				return;
			}
			loadOperation.completed += OnSceneLoadComplete;
		}

		public void DisconnectFromEnvironment()
		{
			if (!currentEnvScenePath.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvScenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currentEnvInstance = null;
				currentEnvScenePath = "";
			}
		}

		private void OnSceneLoadComplete(AsyncOperation op)
		{
			loadingScene = false;

			// hotswap scenes
			Scene loadedScene = SceneManager.GetSceneByPath(currentEnvScenePath);
			if (!loadedScene.IsValid())
			{
				Debug.LogError($"Failed to find loaded environment scene at {currentEnvScenePath}.");
				currentEnvScenePath = "";
				return;
			}
			SceneManager.SetActiveScene(loadedScene);
			
			// initialize depth masks for all root gameobjects
			GameObject[] rootObjects = loadedScene.GetRootGameObjects();
			foreach (GameObject obj in rootObjects) // initialize anything that needs it
			{
				DepthMask[] masks = obj.GetComponentsInChildren<DepthMask>();
				foreach (DepthMask mask in masks)
				{
					PassthroughManager.Instance.AddDepthMask(mask);
				}
			}
		
			// set currentScene reference and initialize networked objects
			// NOTE: currently no environment uses networked objects, but I'm keeping it just for consistency
			EnvironmentScene sceneScript = null;
			foreach (GameObject root in rootObjects)
			{
				sceneScript = root.GetComponentInChildren<EnvironmentScene>();
				if (sceneScript != null)
				{
					break;
				}
			}
			if (sceneScript == null)
			{
				Debug.LogError("Failed to find EnvironmentScene in loaded scene.");
				currentEnvScenePath = "";
				return;
			}
			if (sceneScript.environmentData != null && sceneScript.environmentData.networkObjects.objects != null &&
				sceneScript.environmentData.networkObjects.objects.Count > 0)
			{
				Debug.Log("EnvironmentManager: Spawning networked objects");
				EnvironmentManager.Instance.SpawnNetworkedObjects(sceneScript.environmentData.networkObjects.objects);
			}
			SetEnvironmentInstance(sceneScript);
			
			Debug.Log($"EnvironmentManager: Setting skybox in passthrough to {currentEnvInstance.skyboxOnInPassthrough}");
			PassthroughManager.Instance.SetSkyboxOnInPassthrough(currentEnvInstance.skyboxOnInPassthrough);
			PassthroughManager.Instance.TriggerScenePassthroughEvents();
			
			Teleport();
			OnEnvironmentLoadComplete.Invoke();			
		}

		void SetEnvironmentInstance(EnvironmentScene instance)
		{
			Debug.Log("Setting environment instance to " + instance.name);
			currentEnvInstance = instance;
		}

#region AUTHORITY TRANSFER

		///
		/// AUTHORITY TRANSFER LOGIC
		///

		/// <summary>
		/// Requests a change to the room environment.
		/// First entrypoint for switching environments.
		/// </summary>
		public void RequestRoomEnvironmentChange(int categoryIndex, int entryIndex, int teleportIndex)
		{
			if (loadingScene)
				return;
			if (categoryIndex < 0 || categoryIndex >= EnvironmentCategories.Count ||
				entryIndex < 0 || entryIndex >= EnvironmentCategories[categoryIndex].entries.Count)
			{
				return;
			}
			EnvironmentEntry entry = GetEnvironmentEntry(categoryIndex, entryIndex);
			if (!entry.availableOnThisPlatform || teleportIndex < 0 || teleportIndex >= entry.teleports.Count)
			{
				return;
			}

			if (Object.HasStateAuthority)
			{
				ChangeEnvironmentWithAuthority(categoryIndex, entryIndex, teleportIndex);
			}
			else
			{
				Object.RequestStateAuthority();
				requestedCategoryIndex = categoryIndex;
				requestedEntryIndex = entryIndex;
				requestedTeleportIndex = teleportIndex;
			}
		}

		/// <summary>
		/// Updates the environment and teleport indices with authority.
		/// </summary>
		private void ChangeEnvironmentWithAuthority(int categoryIndex, int entryIndex, int teleport)
		{
			currentEnvironmentSelection = new EnvironmentSelection { CategoryIndex = categoryIndex, EntryIndex = entryIndex };
			currentTeleportIndex = teleport;
		}

		public void StateAuthorityChanged()
		{
			if (Object.HasStateAuthority)
			{
				ChangeEnvironmentWithAuthority(requestedCategoryIndex, requestedEntryIndex, requestedTeleportIndex);
			}
		}

#endregion
#region CLEANUP LOGIC

		///
		/// CLEANUP LOGIC
		///

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Debug.LogError($"Environment Manager has been destroyed!");
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			base.Despawned(runner, hasState);
			Debug.LogError("Environment Manager has been despawned!", this);
		}

#endregion

#region DATA MANAGEMENT


		///
		/// NEW DATA STORE METHODS
		/// 

		/// <summary>
		/// Clears all entries and rebuilds them from local data.
		/// </summary>
		public void ResetEnvironmentLibrary()
		{
			selectedEntryBeforeRefresh = Object != null ? GetEnvironment() : null;
			environmentLibrary.ClearEnvironmentEntries();
			BuildEntriesFromLocalData();
		}

		public void AddEnvironmentEntry(Guid modGUID, Guid assetGUID, ModScene modScene, ModMetadata modMetadata, bool bundleExists)
		{
			EnvironmentEntry newEntry = new EnvironmentEntry();
			newEntry.Initialize(modGUID, assetGUID, modScene, modMetadata, bundleExists);
			Debug.Log($"Adding environment entry for mod {newEntry.modGUID} with asset {newEntry.assetGUID}");
			environmentLibrary.AddEnvironmentEntry(newEntry);
		}

		public void NotifyEnvironmentLibraryChanged()
		{
			if (selectedEntryBeforeRefresh != null && Object != null && Object.HasStateAuthority)
			{
				bool found = false;
				for (int categoryIndex = 0; categoryIndex < EnvironmentCategories.Count; categoryIndex++)
				{
					for (int entryIndex = 0; entryIndex < EnvironmentCategories[categoryIndex].entries.Count; entryIndex++)
					{
						EnvironmentEntry entry = GetEnvironmentEntry(categoryIndex, entryIndex);
						if (entry.modGUID != selectedEntryBeforeRefresh.modGUID ||
							entry.assetGUID != selectedEntryBeforeRefresh.assetGUID ||
							entry.scenePath != selectedEntryBeforeRefresh.scenePath)
						{
							continue;
						}
						currentEnvironmentSelection = new EnvironmentSelection { CategoryIndex = categoryIndex, EntryIndex = entryIndex };
						found = true;
						break;
					}
					if (found)
					{
						break;
					}
				}
				if (!found)
				{
					currentEnvironmentSelection = new EnvironmentSelection();
					currentTeleportIndex = 0;
				}
			}
			selectedEntryBeforeRefresh = null;
			OnEnvironmentLibraryChanged?.Invoke();
			EnvironmentEntry selected = Object != null ? GetEnvironment() : null;
			if (Object != null && !loadingScene && selected != null &&
				(currentEnvInstance == null || retryEnvironmentLoad ||
				(Object.HasStateAuthority && (currentEnvScenePath != selected.scenePath || currentEnvModGUID != selected.modGUID))))
			{
				LoadEnvironmentSceneLocal();
			}
		}

		/// <summary>
		/// Builds environment entries from the local environment data and adds them to the environment library.
		/// </summary>
		void BuildEntriesFromLocalData()
		{
			foreach (var entry in localEnvironmentData)
			{
				EnvironmentEntry newEntry = new EnvironmentEntry();
				newEntry.InitializeFromLocalData(entry);
				Debug.Log($"Building environment entry from local data for environment {entry.name}");
				environmentLibrary.AddEnvironmentEntry(newEntry);
			}
		}
#endregion

	}
}
