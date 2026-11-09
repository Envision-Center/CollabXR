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
		/// STATES
		///

		/// <summary>
		/// Holds the loaded scene and local teleport history.
		/// </summary>
		class CurrentEnvironmentState
		{
			public EnvironmentEntry activeEnvironmentEntry;
			public EnvironmentScene currentEnvInstance;
			public Matrix4x4 previousTeleportMatrix = Matrix4x4.identity;
		}
		private CurrentEnvironmentState currState = new CurrentEnvironmentState();
		public EnvironmentScene GetCurrentEnvInstance()
		{
			return currState.currentEnvInstance;
		}

		/// <summary>
		/// Holds the requested environment state, including the requested environment entry and teleport index.
		/// </summary>
		class RequestedEnvironmentState
		{
			public EnvironmentEntry requestedEnvironmentEntry;
			public int requestedTeleportIndex;
			public void SetRequest(EnvironmentEntry entry, int teleportIndex)
			{
				requestedEnvironmentEntry = entry;
				requestedTeleportIndex = teleportIndex;
			}
		}
		private RequestedEnvironmentState request = new RequestedEnvironmentState();

		/// <summary>
		/// Holds the networked state of the environment.
		/// Primarily used to dispatch environment/teleport change requests, so
		/// that every client calls OnSelectionChanged.
		/// </summary>
		struct NetworkedEnvironmentState : INetworkStruct
		{
			public Guid currentEnvironmentGuid;
			public int currentTeleportIndex;
		}
		[Networked, OnChangedRender(nameof(OnSelectionChanged))]
		NetworkedEnvironmentState networkedState { get; set; }


		///
		/// FLAGS
		/// 

		private bool loadingScene;
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
		private static bool IsSameScene(EnvironmentEntry first, EnvironmentEntry second)
		{
			return first != null && second != null &&
				first.assetGUID == second.assetGUID &&
				first.scenePath == second.scenePath;
		}

		/// <summary>
		/// Builds built-in entries, selects the default for a new room, and passes
		/// the resolved room entry to the local scene loader.
		/// </summary>
		public override void Spawned()
		{
			base.Spawned();
			ResetEnvironmentLibrary();
			
			// if we're the first to join a new room, spawn the default environments
			if (Object.HasStateAuthority && networkedState.currentEnvironmentGuid == Guid.Empty)
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
			LoadEnvironmentSceneLocal(environmentLibrary.GetEntry(networkedState.currentEnvironmentGuid));
		}

		/// <summary>
		/// Spawns the supplied environment objects through this manager's network runner.
		/// </summary>
		/// <param name="objs">The scene's networked object prefabs to spawn.</param>
		private void SpawnNetworkedObjects(List<GameObject> objs)
		{
			foreach (GameObject obj in objs)
			{
				Runner.Spawn(obj, new Vector3(0, 0, 0), Quaternion.identity);
			}
		}

		/// <summary>
		/// Moves the supplied rig to a scene teleport while preserving its offset
		/// from the previous teleport. Updates the manager's teleport history.
		/// </summary>
		/// <param name="scene">The loaded environment containing the teleport.</param>
		/// <param name="teleportIndex">The teleport to use within the supplied scene.</param>
		/// <param name="rigRoot">The player rig transform to move.</param>
		private void Teleport(EnvironmentScene scene, int teleportIndex, Transform rigRoot)
		{
			if (scene == null || teleportIndex < 0 || teleportIndex >= scene.teleports.Length)
			{
				return;
			}
			Debug.Log($"Teleporting to {teleportIndex} in {scene.name}");

			Transform t = scene.teleports[teleportIndex].transform;

			Matrix4x4 localToPrevious = currState.previousTeleportMatrix.inverse * rigRoot.localToWorldMatrix;
			Matrix4x4 newMat = t.localToWorldMatrix * localToPrevious;

			rigRoot.SetPositionAndRotation(newMat.GetPosition(), newMat.rotation);

			currState.previousTeleportMatrix = t.localToWorldMatrix;
		}

		/// <summary>
		/// Loads a changed environment on each client, or applies a changed teleport index.
		/// When both change, scene completion applies the teleport after loading.
		/// </summary>
		/// <param name="previous">The network buffer containing the previous selection.</param>
		private void OnSelectionChanged(NetworkBehaviourBuffer previous)
		{
			NetworkedEnvironmentState before = GetPropertyReader<NetworkedEnvironmentState>(
				nameof(networkedState)
			).Read(previous);
			NetworkedEnvironmentState after = networkedState;

			if (before.currentEnvironmentGuid != after.currentEnvironmentGuid)
			{
				LoadEnvironmentSceneLocal(environmentLibrary.GetEntry(after.currentEnvironmentGuid));
			}
			else if (before.currentTeleportIndex != after.currentTeleportIndex &&
				!loadingScene && currState.currentEnvInstance != null)
			{
				Teleport(currState.currentEnvInstance, after.currentTeleportIndex, HardwareRig.Instance.root);
			}
		}

		/// <summary>
		/// Prepares and loads the supplied environment entry, ensuring its mod bundle is loaded.
		/// Rechecks the live room selection and load version after awaiting mod loading.
		/// </summary>
		/// <param name="entry">The entry to load, or null while its metadata is unavailable.</param>
		private async void LoadEnvironmentSceneLocal(EnvironmentEntry entry)
		{
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

			// Load the mod associated with the supplied environment entry, if applicable.
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
				Debug.LogError($"Failed to prepare environment scene: {exception}");
				return;
			}

			// Check if this load operation is outdated.
			if (loadVersion != sceneLoadVersion)
			{
				return;
			}

			EnvironmentEntry selectedEntry = environmentLibrary.GetEntry(networkedState.currentEnvironmentGuid);
			if (!IsSameScene(entry, selectedEntry))
			{
				loadingScene = false;
				LoadEnvironmentSceneLocal(selectedEntry);
				return;
			}

			if (currState.currentEnvInstance != null && IsSameScene(currState.activeEnvironmentEntry, entry))
			{
				loadingScene = false;
				return;
			}

			EnvironmentEntry previousEntry = null;
			if (currState.activeEnvironmentEntry != null)
			{
				previousEntry = currState.activeEnvironmentEntry;
				currState.activeEnvironmentEntry = null;
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currState.currentEnvInstance = null;
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
				Debug.LogError($"Failed to load environment scene at {entry.scenePath}: {exception}");
				return;
			}

			if (loadOperation == null)
			{
				loadingScene = false;
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
			if (currState.activeEnvironmentEntry != null)
			{
				SceneManager.UnloadSceneAsync(currState.activeEnvironmentEntry.scenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
			}
			currState.activeEnvironmentEntry = null;
			currState.currentEnvInstance = null;
		}

		/// <summary>
		/// Validates a completed load against the live room selection and load version,
		/// initializes the loaded scene, and teleports using that scene explicitly.
		/// </summary>
		/// <param name="entry">The entry associated with the completed scene load.</param>
		/// <param name="loadVersion">The generation captured when the load started.</param>
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
			EnvironmentEntry selectedEntry = environmentLibrary.GetEntry(networkedState.currentEnvironmentGuid);
			if (!IsSameScene(entry, selectedEntry))
			{
				if (loadedScene.IsValid() && loadedScene.isLoaded)
				{
					SceneManager.UnloadSceneAsync(loadedScene);
				}
				LoadEnvironmentSceneLocal(selectedEntry);
				return;
			}

			// hotswap scenes
			if (!loadedScene.IsValid())
			{
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
				return;
			}
			SceneManager.SetActiveScene(loadedScene);
			if (sceneScript.environmentData != null && sceneScript.environmentData.networkObjects.objects != null &&
				sceneScript.environmentData.networkObjects.objects.Count > 0)
			{
				Debug.Log("EnvironmentManager: Spawning networked objects");
				SpawnNetworkedObjects(sceneScript.environmentData.networkObjects.objects);
			}
			SetEnvironmentInstance(sceneScript);
			currState.activeEnvironmentEntry = entry;
			
			Debug.Log($"EnvironmentManager: Setting skybox in passthrough to {sceneScript.skyboxOnInPassthrough}");
			PassthroughManager.Instance.SetSkyboxOnInPassthrough(sceneScript.skyboxOnInPassthrough);
			PassthroughManager.Instance.TriggerScenePassthroughEvents();
			
			Teleport(sceneScript, networkedState.currentTeleportIndex, HardwareRig.Instance.root);
			OnEnvironmentLoadComplete.Invoke();			
		}

		void SetEnvironmentInstance(EnvironmentScene instance)
		{
			Debug.Log("Setting environment instance to " + instance.name);
			currState.currentEnvInstance = instance;
		}

#region AUTHORITY TRANSFER

		///
		/// AUTHORITY TRANSFER LOGIC
		///

		/// <summary>
		/// Validates a requested environment and teleport, then publishes the supplied
		/// selection or stores it while waiting for state authority.
		/// </summary>
		/// <param name="entry">The current library entry to select, including the active scene.</param>
		/// <param name="teleportIndex">The teleport to apply in the selected environment.</param>
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
				request.SetRequest(entry, teleportIndex);
				Object.RequestStateAuthority();
			}
		}

		/// <summary>
		/// Assigns the complete networked selection with authority so Fusion replicates
		/// both values and dispatches the change. The active scene is set only after loading.
		/// </summary>
		/// <param name="entry">The validated environment entry to publish.</param>
		/// <param name="teleportIndex">The teleport to publish with the environment GUID.</param>
		private void ChangeEnvironmentWithAuthority(EnvironmentEntry entry, int teleportIndex)
		{
			networkedState = new NetworkedEnvironmentState
			{
				currentEnvironmentGuid = entry.assetGUID,
				currentTeleportIndex = teleportIndex
			};
		}

		/// <summary>
		/// Captures and clears the pending request when authority is granted, then
		/// passes both request inputs through the normal validation path.
		/// </summary>
		public void StateAuthorityChanged()
		{
			if (Object.HasStateAuthority && request.requestedEnvironmentEntry != null)
			{
				EnvironmentEntry entry = request.requestedEnvironmentEntry;
				int teleportIndex = request.requestedTeleportIndex;

				request = default;
				RequestRoomEnvironmentChange(entry, teleportIndex);
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
		/// Clears the pending request and library, then explicitly supplies the
		/// configured built-in data and default to the entry builder.
		/// </summary>
		public void ResetEnvironmentLibrary()
		{
			request = default;
			environmentLibrary.ClearEnvironmentEntries();
			BuildEntriesFromLocalData(localEnvironmentData, defaultEnvironmentEntry);
		}

		public void AddEnvironmentEntry(Guid modGUID, Guid assetGUID, ModScene modScene, ModMetadata modMetadata, bool bundleExists)
		{
			EnvironmentEntry newEntry = new EnvironmentEntry();
			newEntry.Initialize(modGUID, assetGUID, modScene, modMetadata, bundleExists);
			Debug.Log($"Adding environment entry for mod {newEntry.modGUID} with asset {newEntry.assetGUID}");
			environmentLibrary.AddEnvironmentEntry(newEntry);
		}

		/// <summary>
		/// Notifies menu listeners after a library rebuild, even if the room entry is missing.
		/// Resolves the room GUID once and loads it even when no scene has loaded yet.
		/// </summary>
		public void OnSimpleModLoaderRefresh()
		{
			OnEnvironmentLibraryChanged?.Invoke();
			if (Object == null || networkedState.currentEnvironmentGuid == Guid.Empty)
			{
				return;
			}

			EnvironmentEntry entry = environmentLibrary.GetEntry(networkedState.currentEnvironmentGuid);
			if (entry == null)
			{
				// Catastrophic failure, GUID is not found in the environment library.
				// Could be caused by a client not having the same repositories as the host.
				// Since environments are global, we can't just load the default environment.
				// TODO: Look into the possibility of falling back to a default environment ONLY FOR THIS CLIENT.
				Debug.LogError($"Environment {networkedState.currentEnvironmentGuid} was not found after the library refresh.");
				return;
			}

			if (currState.currentEnvInstance == null || !IsSameScene(currState.activeEnvironmentEntry, entry))
			{
				LoadEnvironmentSceneLocal(entry);
			}
		}

		/// <summary>
		/// Builds entries from the supplied built-in data and adds them to the owned library.
		/// Records the runtime entry corresponding to the supplied default data asset.
		/// </summary>
		/// <param name="source">The built-in environment data assets to process.</param>
		/// <param name="defaultData">The data asset whose runtime entry is the default.</param>
		private void BuildEntriesFromLocalData(EnvironmentData[] source, EnvironmentData defaultData)
		{
			foreach (EnvironmentData data in source)
			{
				if (data == null ||
					!Guid.TryParseExact(data.sceneGuid, "N", out Guid sceneGuid) || sceneGuid == Guid.Empty)
				{
					Debug.LogError($"Built-in environment {data?.name} has no valid scene GUID.");
					continue;
				}

				EnvironmentEntry newEntry = new EnvironmentEntry();
				newEntry.InitializeFromLocalData(data, sceneGuid);
				environmentLibrary.AddEnvironmentEntry(newEntry);
			
				if (data == defaultData)
				{
					environmentLibrary.defaultEnvironmentEntry = newEntry;
				}
			}
		}
#endregion

	}
}
