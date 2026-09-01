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
		public EventVariable<bool> sessionManagerSpawned = new();

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

		[SerializeField]
		private ScriptableInt role;
		[SerializeField]
		private LoadingPopup popupPrefab;
		private LoadingPopup popup;

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
		}

		public void OnSessionReady(bool ready)
		{
			if(ready)
			{
				onSessionReady.Invoke();
				onSessionReady.RemoveAllListeners();
				statePrefabInstance = Instantiate(sessionPrefab);
				sessionReady = true;

				if (popup != null)
				{
					GameObject.Destroy(popup.gameObject);
				}
			}
		}

		public void ChangeConnectionState(ConnectionState newState)
		{
			Debug.Log("Switching to state " + newState);
			bool isActuallyNewState = state != newState;
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
			else if (state == ConnectionState.Session && isActuallyNewState)
			{
				sessionManagerSpawned.AddListenerAndCheck(OnSessionReady);
				RepositoryManager.RefreshAllMods();
				if (NetworkManager.Runner.IsSharedModeMasterClient)
				{
					sessionManagerInstance = NetworkManager.Runner.Spawn(sessionManager);
				}

				if(!sessionReady)
				{
					popup = Instantiate(popupPrefab);
				}
			}
			else if (state == ConnectionState.Disconnecting)
			{
				sessionReady = false;
				sessionManagerInstance = null;
				onSessionReady.RemoveAllListeners();
				sessionManagerSpawned = new();
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
