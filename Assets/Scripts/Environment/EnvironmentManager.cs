using System.Collections.Generic;
using CollabXR.ModExtras;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace CollabXR.Environments
{
	public class EnvironmentManager : SingletonNetworkBehavior<EnvironmentManager>, IStateAuthorityChanged, IDespawned
	{
		[SerializeField]
		public EnvironmentData[] environmentData;

		[Networked, OnChangedRender(nameof(LoadEnvironmentSceneLocal))]
		public int currentEnvironmentIndex { get; set; }

		[Networked, OnChangedRender(nameof(Teleport))]
		public int currentTeleportIndex { get; set; }
		public UnityEvent OnEnvironmentLoadComplete;
		private Matrix4x4 previousTeleportMatrix = Matrix4x4.identity;

		private int requestedEnvIndex,
			requestedTeleportIndex;

		private string currentEnvSceneName = "";
		bool loadingScene = false;
		bool teleportAfterSceneLoad = false;

		//, needsRefresh = false, needsEnvSet = false;

		//protected override void Awake()
		//{
		//	base.Awake();
		//}

		public override void Spawned()
		{
			base.Spawned();
			LoadEnvironmentSceneLocal();
		}

		//public override void Despawned(NetworkRunner runner, bool hasState)
		//{
		//	base.Despawned(runner, hasState);
		//	SceneManager.sceneLoaded -= OnSceneLoad;
		//}

		//private void Update()
		//{
		//	if (needsRefresh)
		//	{
		//		// ignore what scene is calling this, only use the current networked environment scene
		//		// this is a hack to avoid scene loading timing issues
		//		string sceneName = GetEnvironment().sceneName;
		//		Scene targetScene = SceneManager.GetSceneByName(sceneName);
		//		if (targetScene.rootCount > 0)
		//		{
		//			SceneManager.SetActiveScene(targetScene);
		//		}

		//		SetEnvironment();
		//		waitingOnLoad = false;
		//		needsRefresh = false;
		//	}
		//	else if (needsEnvSet)
		//	{
		//		SetEnvironment();
		//		needsEnvSet = false;
		//	}
		//}

		public void SpawnNetworkedObjects(List<GameObject> objs)
		{
			foreach (GameObject obj in objs)
			{
				Runner.Spawn(obj, new Vector3(0, 0, 0), Quaternion.identity);
			}
		}

		// scufffed TODO: refactor
		private void Teleport()
		{
			if (loadingScene)
			{
				return;
			}

			var envScene = FindObjectOfType<EnvironmentScene>();

			if (envScene == null)
			{
				return;
			}

			Transform t = envScene.teleports[currentTeleportIndex].transform;

			Matrix4x4 localToPrevious = previousTeleportMatrix.inverse * HardwareRig.Instance.root.localToWorldMatrix;
			Matrix4x4 newMat = t.localToWorldMatrix * localToPrevious;

			HardwareRig.Instance.root.SetPositionAndRotation(newMat.GetPosition(), newMat.rotation);

			previousTeleportMatrix = t.localToWorldMatrix;
		}

		//public void OnSceneLoad(Scene scene, LoadSceneMode mode)
		//{
		//	if (scene.buildIndex > 1) // not the menu or game
		//	{
		//		Debug.Log("Loaded " + scene.name + " with index " + scene.buildIndex + " in mode " + mode);
		//		needsRefresh = true;
		//	}
		//}

		void LoadEnvironmentSceneLocal()
		{
			Debug.Log(currentEnvSceneName);
			Debug.Log(loadingScene);
			if (loadingScene)
				return;

			loadingScene = true;

			if (!currentEnvSceneName.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvSceneName);
			}

			currentEnvSceneName = GetEnvironment().sceneName;
			SceneManager.LoadSceneAsync(GetEnvironment().sceneName, LoadSceneMode.Additive).completed += OnSceneLoadComplete;
		}

		public void DisconnectFromEnvironment()
		{
			if (!currentEnvSceneName.Equals(""))
			{
				SceneManager.UnloadSceneAsync(currentEnvSceneName);
			}
		}

		private void OnSceneLoadComplete(AsyncOperation op)
		{
			loadingScene = false;
			Scene loadedScene = SceneManager.GetSceneByName(GetEnvironment().sceneName);
			SceneManager.SetActiveScene(loadedScene);
			GameObject[] rootObjects = loadedScene.GetRootGameObjects();
			foreach (GameObject obj in rootObjects) // initialize anything that needs it
			{
				DepthMask[] masks = obj.GetComponentsInChildren<DepthMask>();
				foreach (DepthMask mask in masks)
				{
					PassthroughManager.Instance.AddDepthMask(mask);
				}
			}
			OnEnvironmentLoadComplete.Invoke();
			Teleport();
		}

		//void TeleportLocal()
		//{
		//	if (!waitingOnLoad) {
		//		SetEnvironment();
		//	}
		//}

		//void SetEnvironment()
		//{
		//	SetLighting();
		//}

		//void SetLighting()
		//{
		//	GetEnvironment().customLightingConfig?.Activate();
		//}

		public EnvironmentData GetEnvironmentAtIndex(int index) => environmentData[index];

		public EnvironmentData GetEnvironment() => environmentData[currentEnvironmentIndex];

		//public EnvironmentScene GetEnvironmentInstance()
		//{
		//	return envInstances[currentEnvironmentIndex];
		//}

		//public EnvironmentData GetLastEnvironment()
		//{
		//    return envData[lastEnvironment];
		//}

		//public EnvironmentScene GetLastEnvironmentInstance()
		//{
		//    return envInstances[lastEnvironment];
		//}

		//public EnvironmentTeleport GetTeleport()
		//{
		//	return GetEnvironmentInstance().teleports[currentTeleportIndex];
		//}

		//public void TeleportTo(int index)
		//{
		//	if (Object.HasStateAuthority)
		//	{
		//		UpdateWithAuthority(currentEnvironmentIndex, index);
		//	}
		//	else
		//	{
		//		Object.RequestStateAuthority();
		//		tempTeleport = index;
		//	}
		//}

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
	}
}
