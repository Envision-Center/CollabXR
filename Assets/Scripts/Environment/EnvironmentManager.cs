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
		/// Legacy built-in environment data scriptable objects
		/// </summary>
		[SerializeField] public EnvironmentData[] localEnvironmentData;

		///
		/// STATE VARIABLES
		/// 

		[Networked, OnChangedRender(nameof(LoadEnvironmentSceneLocal))]
		public int currentEnvironmentIndex { get; set; }
		private string currentEnvScenePath = "";
		[SerializeField] public EnvironmentScene currentEnvInstance;

		[Networked, OnChangedRender(nameof(Teleport))]
		public int currentTeleportIndex { get; set; }
		
		private Matrix4x4 previousTeleportMatrix = Matrix4x4.identity;
		private int requestedEnvIndex;
		private int requestedTeleportIndex;
		bool loadingScene = false;
		bool teleportAfterSceneLoad = false;

		///
		/// EVENTS
		/// 

		public UnityEvent OnEnvironmentLoadComplete;

		/// <summary>
		/// Overriding singleton initialization function
		/// Loads currentEnvScenePath, which starts as empty "", so loads nothing
		/// </summary>
		public override void Spawned()
		{
			base.Spawned();
			ResetEnvironmentLibrary();
			LoadEnvironmentSceneLocal();
		}

		public void SpawnNetworkedObjects(List<GameObject> objs)
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
			Debug.Log($"Teleporting to {currentTeleportIndex} in {GetEnvironment().sceneName}");
			if (loadingScene)
			{
				return;
			}

			Transform t = currentEnvInstance.teleports[currentTeleportIndex].transform;

			Matrix4x4 localToPrevious = previousTeleportMatrix.inverse * HardwareRig.Instance.root.localToWorldMatrix;
			Matrix4x4 newMat = t.localToWorldMatrix * localToPrevious;

			HardwareRig.Instance.root.SetPositionAndRotation(newMat.GetPosition(), newMat.rotation);

			previousTeleportMatrix = t.localToWorldMatrix;
		}

		/// <summary>
		/// Main function that loads the current environment at "currentEnvScenePath".
		/// </summary>
		void LoadEnvironmentSceneLocal()
		{
			// flag to indicate if a scene is currently loading
			if (loadingScene)
			{
				return;
			}
			loadingScene = true;

			if (!currentEnvScenePath.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvScenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currentEnvInstance = null;
			}

			currentEnvScenePath = GetEnvironment().scenePath;
			Debug.Log($"Initializing loading of: {currentEnvScenePath}");
			SceneManager.LoadSceneAsync(currentEnvScenePath, LoadSceneMode.Additive).completed += OnSceneLoadComplete;
		}

		public void DisconnectFromEnvironment()
		{
			if (!currentEnvScenePath.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvScenePath);
				PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
				currentEnvInstance = null;
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
			EnvironmentScene sceneScript = FindFirstObjectByType<EnvironmentScene>();
			if (sceneScript == null)
			{
				Debug.LogError("Failed to find EnvironmentScene in loaded scene.");
				return;
			}
			if (sceneScript.environmentData.networkObjects.objects.Count > 0)
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

		public EnvironmentData GetEnvironmentAtIndex(int index) => localEnvironmentData[index];

		public EnvironmentData GetEnvironment() => localEnvironmentData[currentEnvironmentIndex];

		public void RequestRoomEnvironmentChange(int envIndex, int teleportIndex)
		{
			if (loadingScene)
				return;

			if (Object.HasStateAuthority)
			{
				UpdateWithAuthority(envIndex, teleportIndex);
			}
			else
			{
				Object.RequestStateAuthority();
				requestedEnvIndex = envIndex;
				requestedTeleportIndex = teleportIndex;
			}
		}

		private void UpdateWithAuthority(int environment, int teleport)
		{
			currentEnvironmentIndex = environment;
			currentTeleportIndex = teleport;
		}

		public void StateAuthorityChanged()
		{
			if (Object.HasStateAuthority)
			{
				UpdateWithAuthority(requestedEnvIndex, requestedTeleportIndex);
			}
		}

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

		void SetEnvironmentInstance(EnvironmentScene instance)
		{
			Debug.Log("Setting environment instance to " + instance.name);
			currentEnvInstance = instance;
		}

		///
		/// NEW DATA STORE METHODS
		/// 

		/// <summary>
		/// Clears all entries and rebuilds them from local data.
		/// </summary>
		public void ResetEnvironmentLibrary()
		{
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
	}
}
