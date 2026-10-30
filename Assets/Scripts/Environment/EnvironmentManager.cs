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
		/// Built-in environments.
		/// </summary>
		[SerializeField] public EnvironmentData[] localEnvironmentData;
		[SerializeField] public EnvironmentData defaultEnvironmentEntry;

		///
		/// STATE VARIABLES
		///

		[Networked, OnChangedRender(nameof(LoadEnvironmentSceneLocal))]
		public Guid currentEnvironmentGuid { get; set; }
		private EnvironmentEntry activeEnvironmentEntry;
		[SerializeField] public EnvironmentScene currentEnvInstance;

		[Networked, OnChangedRender(nameof(Teleport))]
		public int currentTeleportIndex { get; set; }
		
		///
		/// TELEPORT FIELDS
		/// 

		private Matrix4x4 previousTeleportMatrix = Matrix4x4.identity;
		private EnvironmentEntry requestedEnvironmentEntry;
		private int requestedTeleportIndex;

		///
		/// FLAGS
		/// 

		private bool loadingScene;
		private bool retryEnvironmentLoad;
		private int sceneLoadVersion; // generation index

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
		public EnvironmentEntry GetSelectedEnvironmentEntry()
		{
			return environmentLibrary.GetEntry(currentEnvironmentGuid);
		}

		private static bool IsSameScene(EnvironmentEntry first, EnvironmentEntry second)
		{
			return first != null && second != null &&
				first.assetGUID == second.assetGUID &&
				first.scenePath == second.scenePath;
		}

		/// <summary>
		/// Overriding singleton initialization function
		/// Builds built-in entries and loads the selected environment.
		/// </summary>
		public override void Spawned()
		{
			base.Spawned();
			ResetEnvironmentLibrary();
			if (Object.HasStateAuthority && currentEnvironmentGuid == Guid.Empty)
			{
				if (environmentLibrary.defaultEnvironmentEntry == null)
				{
					Debug.LogError("No default environment entry is configured.");
				}
				else
				{
					ChangeEnvironmentWithAuthority(environmentLibrary.defaultEnvironmentEntry, 0);
				}
			}
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
		/// Loads the environment selected by the networked identifiers.
		/// </summary>
		private async void LoadEnvironmentSceneLocal()
		{
			EnvironmentEntry entry = GetSelectedEnvironmentEntry();
			if (entry == null)
			{
				return;
			}

			// flag to indicate if a scene is currently loading
			if (loadingScene)
			{
				return;
			}
			loadingScene = true;
			int loadVersion = ++sceneLoadVersion;

			try
			{
				if (!entry.availableOnThisPlatform || string.IsNullOrEmpty(entry.scenePath))
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
				if (loadVersion != sceneLoadVersion)
				{
					return;
				}
				loadingScene = false;
				retryEnvironmentLoad = true;
				Debug.LogError($"Failed to prepare environment scene: {exception}");
				return;
			}
			if (loadVersion != sceneLoadVersion)
			{
				return;
			}

			retryEnvironmentLoad = false;
			if (!IsSameScene(entry, GetSelectedEnvironmentEntry()))
			{
				loadingScene = false;
				if (GetSelectedEnvironmentEntry() != null)
				{
					LoadEnvironmentSceneLocal();
				}
				else
				{
					retryEnvironmentLoad = true;
				}
				return;
			}
			if (currentEnvInstance != null && IsSameScene(activeEnvironmentEntry, entry))
			{
				loadingScene = false;
				return;
			}

			EnvironmentEntry previousEntry = null;
			if (activeEnvironmentEntry != null)
			{
				previousEntry = activeEnvironmentEntry;
				activeEnvironmentEntry = null;
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currentEnvInstance = null;
			}

			Debug.Log($"Initializing loading of: {entry.scenePath}");
			AsyncOperation loadOperation;
			try
			{
				loadOperation = SceneManager.LoadSceneAsync(entry.scenePath, LoadSceneMode.Additive);
			}
			catch (Exception exception)
			{
				loadingScene = false;
				retryEnvironmentLoad = true;
				Debug.LogError($"Failed to load environment scene at {entry.scenePath}: {exception}");
				return;
			}

			if (loadOperation == null)
			{
				loadingScene = false;
				retryEnvironmentLoad = true;
				Debug.LogError($"Failed to start loading environment scene at {entry.scenePath}.");
				return;
			}
			loadOperation.completed += (e) =>
			{
				if (previousEntry != null)
				{
					SceneManager.UnloadSceneAsync(previousEntry.scenePath);
				}
				OnSceneLoadComplete(entry, loadVersion);
			};
		}

		public void DisconnectFromEnvironment()
		{
			sceneLoadVersion++;
			loadingScene = false;
			retryEnvironmentLoad = false;
			if (activeEnvironmentEntry != null)
			{
				SceneManager.UnloadSceneAsync(activeEnvironmentEntry.scenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
			}
			activeEnvironmentEntry = null;
			currentEnvInstance = null;
		}

		private void OnSceneLoadComplete(EnvironmentEntry entry, int loadVersion)
		{
			Scene loadedScene = SceneManager.GetSceneByPath(entry.scenePath);
			if (loadVersion != sceneLoadVersion)
			{
				if (loadedScene.IsValid() && loadedScene.isLoaded)
				{
					SceneManager.UnloadSceneAsync(loadedScene);
				}
				return;
			}
			loadingScene = false;
			if (!IsSameScene(entry, GetSelectedEnvironmentEntry()))
			{
				if (loadedScene.IsValid() && loadedScene.isLoaded)
				{
					SceneManager.UnloadSceneAsync(loadedScene);
				}
				if (GetSelectedEnvironmentEntry() != null)
				{
					LoadEnvironmentSceneLocal();
				}
				else
				{
					retryEnvironmentLoad = true;
				}
				return;
			}

			// hotswap scenes
			if (!loadedScene.IsValid())
			{
				retryEnvironmentLoad = true;
				Debug.LogError($"Failed to find loaded environment scene at {entry.scenePath}.");
				return;
			}
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
				SceneManager.UnloadSceneAsync(loadedScene);
				retryEnvironmentLoad = true;
				return;
			}
			SceneManager.SetActiveScene(loadedScene);
			if (sceneScript.environmentData != null && sceneScript.environmentData.networkObjects.objects != null &&
				sceneScript.environmentData.networkObjects.objects.Count > 0)
			{
				Debug.Log("EnvironmentManager: Spawning networked objects");
				EnvironmentManager.Instance.SpawnNetworkedObjects(sceneScript.environmentData.networkObjects.objects);
			}
			SetEnvironmentInstance(sceneScript);
			activeEnvironmentEntry = entry;
			
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
		public void RequestRoomEnvironmentChange(EnvironmentEntry entry, int teleportIndex)
		{
			if (loadingScene)
			{
				return;
			}
			if (entry == null || !ReferenceEquals(environmentLibrary.GetEntry(entry.assetGUID), entry))
			{
				return;
			}
			if (!entry.availableOnThisPlatform || teleportIndex < 0 || teleportIndex >= entry.teleports.Count)
			{
				return;
			}

			if (Object.HasStateAuthority)
			{
				ChangeEnvironmentWithAuthority(entry, teleportIndex);
			}
			else
			{
				requestedEnvironmentEntry = entry;
				requestedTeleportIndex = teleportIndex;
				Object.RequestStateAuthority();
			}
		}

		/// <summary>
		/// Updates the selected environment and teleport with authority.
		/// </summary>
		private void ChangeEnvironmentWithAuthority(EnvironmentEntry entry, int teleport)
		{
			currentEnvironmentGuid = entry.assetGUID;
			currentTeleportIndex = teleport;
		}

		public void StateAuthorityChanged()
		{
			if (Object.HasStateAuthority && requestedEnvironmentEntry != null)
			{
				EnvironmentEntry entry = requestedEnvironmentEntry;
				requestedEnvironmentEntry = null;
				RequestRoomEnvironmentChange(entry, requestedTeleportIndex);
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
			requestedEnvironmentEntry = null;
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
			OnEnvironmentLibraryChanged?.Invoke();
		}

		/// <summary>
		/// Builds environment entries from the local environment data and adds them to the environment library.
		/// </summary>
		void BuildEntriesFromLocalData()
		{
			foreach (EnvironmentData data in localEnvironmentData)
			{
				if ((data == null) ||
					(!Guid.TryParseExact(data.sceneGuid, "N", out Guid sceneGuid)) ||
					(sceneGuid == Guid.Empty)
				) {
					Debug.LogError($"Built-in environment {data?.name} has no valid scene GUID.");
					continue;
				}

				EnvironmentEntry newEntry = new EnvironmentEntry();
				newEntry.InitializeFromLocalData(data, sceneGuid);
				environmentLibrary.AddEnvironmentEntry(newEntry);
			
				if (data == defaultEnvironmentEntry)
				{
					environmentLibrary.defaultEnvironmentEntry = newEntry;
				}
			}
		}
#endregion

	}
}
