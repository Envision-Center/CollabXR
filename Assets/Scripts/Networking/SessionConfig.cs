using System;
using System.Collections;
using System.Collections.Generic;
using CollabXR.Environments;
using CollabXR.ModLoader;
using CollabXR.Networking;
using CollabXR.Scriptables;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static CollabXR.Networking.NetworkPlayer;
using static Unity.Collections.Unicode;

namespace CollabXR.Networking
{
	public enum ConnectionState
	{
		Lobby,
		Connecting,
		Session,
		Disconnecting,
	};

	public class SessionConfig : SingletonBehavior<SessionConfig>
	{
		public UnityEvent onSessionReady; // invokes after session managers are spawned

		[SerializeField]
		private GameObject lobbyPrefab;

		[SerializeField]
		private GameObject sessionPrefab;

		[SerializeField]
		private GameObject networkManagerPrefab;

		[SerializeField]
		private GameObject sessionManager;

		private GameObject statePrefabInstance;
		private NetworkObject sessionManagerInstance;
		public ConnectionState state;
		bool sessionReady = false;
		float sessionReadyTimeout;

		public float sessionReadyTimeoutSeconds = 3.0f;

		[SerializeField]
		private ScriptableInt role;

		protected override void Awake()
		{
			base.Awake();
			role.Set((int)NetworkPlayerRole.Student);
		}

		// INTENDED SPAWNING ORDER (race conditions pose are a big problem for single scene photon)
		// Session Managers
		// Network Player (some drivers depend on the managers)
		// Session Prefab (tools depend on network player)

		private void Update()
		{
			if (NetworkManager.Instance == null) // waiting for disconnected game
			{
				SessionConfig.Instance.ChangeConnectionState(ConnectionState.Lobby);
			}
			if (state == ConnectionState.Session && !sessionReady)
			{
				if (!NetworkManager.Runner.IsSharedModeMasterClient && sessionReadyTimeout < sessionReadyTimeoutSeconds)
				{
					//sessionReadyTimeout += Time.deltaTime;
					// joining an existing room, need to find session manager instance
					List<NetworkObject> spawnedObjects = NetworkManager.Runner.GetAllNetworkObjects();
					foreach (NetworkObject obj in spawnedObjects)
					{
						// found object, checking if valid
						if (obj.GetComponent<EnvironmentManager>() != null && obj.IsValid)
						{
							sessionManagerInstance = obj;
							break;
						}
					}
				}
				if (sessionManagerInstance == null)
					return;
				onSessionReady.Invoke();
				onSessionReady.RemoveAllListeners();
				statePrefabInstance = Instantiate(sessionPrefab);
				sessionReady = true;
			}
		}

		public void ChangeConnectionState(ConnectionState newState)
		{
			Debug.Log("Switching to state " + newState);
			state = newState;
			if (statePrefabInstance != null)
			{
				GameObject.Destroy(statePrefabInstance);
			}
			if (state == ConnectionState.Lobby)
			{
				statePrefabInstance = Instantiate(lobbyPrefab);
				Instantiate(networkManagerPrefab);
			}
			else if (state == ConnectionState.Session)
			{
				RepositoryManager.RefreshAllMods();
				if (NetworkManager.Runner.IsSharedModeMasterClient)
				{
					sessionManagerInstance = NetworkManager.Runner.Spawn(sessionManager);
				}
			}
			else if (state == ConnectionState.Disconnecting)
			{
				sessionReady = false;
				sessionReadyTimeout = 0;
				sessionManagerInstance = null;
				onSessionReady.RemoveAllListeners();
				EnvironmentManager.Instance.DisconnectFromEnvironment();
				if (NetworkManager.Runner != null && !NetworkManager.Runner.IsShutdown)
				{
					NetworkManager.Runner.Shutdown(true);
					Destroy(NetworkManager.Instance);
				}
			}
		}

		public void InvokeWhenReady(UnityAction action)
		{
			if (state == ConnectionState.Session && sessionReady)
			{
				action.Invoke();
			}
			else
			{
				onSessionReady.AddListener(action);
			}
		}
	}
}
