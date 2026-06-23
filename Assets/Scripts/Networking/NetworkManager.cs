using System;
using System.Collections.Generic;
using System.Linq;
using CollabXR.Scriptables;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollabXR.Networking
{
	public class NetworkManager : SingletonBehavior<NetworkManager>, INetworkRunnerCallbacks
	{
		public static NetworkRunner Runner { get; private set; }

		public GameMode defaultMode = GameMode.Shared;

		private string[] regionCodes = { "asia", "au", "cae", "eu", "hk", "in", "jp", "za", "sa", "kr", "tr", "uae", "us", "usw", "ussc" };

		[SerializeField]
		private ScriptableInt region;

		[SerializeField]
		private bool connectOnStart;

		[SerializeField]
		private NetworkObject networkRigPrefab;

		public event Action<bool> OnDeafenChange;

		private Recorder voiceRecorder;
		private FusionVoiceClient voiceClient;

		protected override void Awake()
		{
			Debug.Log("I HAVE BEEN SUMMONED");
			base.Awake();

			if (Runner == null)
				Runner = GetComponent<NetworkRunner>();

			// Create the Fusion runner and let it know that we will be providing user input
			if (Runner == null)
				Runner = gameObject.AddComponent<NetworkRunner>();

			voiceRecorder = GetComponent<Recorder>();
		}

		private void Start()
		{
			ConnectToLobby();
			if (connectOnStart)
			{
				defaultMode = GameMode.Single;
				Connect("Developer room " + UnityEngine.Random.Range(1000, 9999));
			}
		}

		private void ConnectToLobby()
		{
			Runner.JoinSessionLobby(SessionLobby.Shared, null, null, GetAppSettings());
		}

		public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
		{
			Debug.Log(player + " just joined the room.");
			// UpdatePlayerCount();
		}

		public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
		{
			Debug.Log(player + " just left the room.");
			// UpdatePlayerCount();
		}

		public void Connect(string roomName)
		{
			Debug.Log("Joining room: " + roomName + " at region: " + GetAppSettings().FixedRegion + " in mode " + defaultMode);
			Runner.StartGame(
				new StartGameArgs
				{
					GameMode = defaultMode,
					SessionName = roomName,
					//Scene = 1,
					//SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
					CustomPhotonAppSettings = GetAppSettings(),
					Config = NetworkProjectConfig.Global,
				}
			);
		}

		//private bool shouldInstantiatePlayerOnSceneLoad;

		public void OnConnectedToServer(NetworkRunner runner)
		{
			//shouldInstantiatePlayerOnSceneLoad = true;
			SessionConfig.Instance.ChangeConnectionState(ConnectionState.Session);
			SessionConfig.Instance.InvokeWhenReady(SpawnPlayer);
		}

		public void OnSceneLoadDone(NetworkRunner runner)
		{
			//if (!shouldInstantiatePlayerOnSceneLoad)
			//	return;

			//shouldInstantiatePlayerOnSceneLoad = false;
		}

		private void SpawnPlayer()
		{
			NetworkObject networkPlayerObject = Runner.Spawn(networkRigPrefab, transform.position, transform.rotation, Runner.LocalPlayer);

			Runner.SetPlayerObject(Runner.LocalPlayer, networkPlayerObject);
		}

		public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
		{
			Debug.LogWarning("Shutdown for reason " + shutdownReason);
			SessionConfig.Instance.ChangeConnectionState(ConnectionState.Disconnecting);
		}

		public void OnDisconnectedFromServer(NetworkRunner runner)
		{
			SessionConfig.Instance.ChangeConnectionState(ConnectionState.Disconnecting);
		}

		public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
		{
			Debug.LogWarning("Connection failed because " + reason);
			SessionConfig.Instance.ChangeConnectionState(ConnectionState.Disconnecting);
		}

		public NetworkObjectGuid GetObjectGuid(NetworkObject obj)
		{
			obj.TryGetComponent<NetworkObjectPrefabData>(out NetworkObjectPrefabData data);
			return data.Guid;
		}

		private FusionAppSettings GetAppSettings()
		{
			FusionAppSettings appSettings = PhotonAppSettings.Global.AppSettings.GetCopy();
			appSettings.FixedRegion = regionCodes[region.Value];
			return appSettings;
		}

		public static NetworkPlayer GetPlayerRig(PlayerRef player)
		{
			NetworkObject playerRig = null;
			try
			{
				Runner.TryGetPlayerObject(player, out playerRig);
			}
			catch (Exception e)
			{
				Debug.LogError(e.Message);
			}

			if (playerRig != null)
				return playerRig.GetComponent<NetworkPlayer>();
			return null;
		}

		public static OVRSpaceUser[] GetActiveOculusIDs()
		{
			int playerCount = Runner.ActivePlayers.Count();
			List<OVRSpaceUser> users = new(playerCount);
			for (int i = 0; i < playerCount; i++)
			{
				PlayerRef p = Runner.ActivePlayers.ElementAt(i);
				NetworkPlayer rig = GetPlayerRig(p);

				if (rig == null)
				{
					Debug.LogWarning("Unable to find network rig, something is wrong...");
					continue;
				}

				if (rig.OculusID == 0)
					continue;

				users.Add(new OVRSpaceUser(rig.OculusID));
				Debug.Log(p + " has ID " + rig.DeviceID + "and oculus ID " + rig.OculusID);
			}

			return users.ToArray();
		}

		public bool InLobbyScene()
		{
			return SceneManager.GetActiveScene().buildIndex == 0;
		}

		public void DisconnectFromRoom()
		{
			Runner.Shutdown();
		}

		#region Unused INetworkRunnerCallbacks

		public void OnInput(NetworkRunner runner, NetworkInput input) { }

		public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

		public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

		public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

		public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

		public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

		public void OnSceneLoadStart(NetworkRunner runner) { }

		public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
		{
			// not implemented
		}

		public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
		{
			// not implemented
		}

		public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
		{
			// not implemented
		}

		public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
		{
			// not implemented
		}

		public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
		{
			// not implemented
		}

		public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
		{
			// not implemented
		}

		public void OnLocalMuteStatusChanged(bool muted)
		{
			voiceRecorder.RecordingEnabled = !muted;
		}

		public void OnLocalDeafenStatusChanged(bool deafen)
		{
			OnDeafenChange.Invoke(deafen);
		}

		#endregion
	}
}
