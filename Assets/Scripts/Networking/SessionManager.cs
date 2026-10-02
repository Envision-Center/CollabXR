using System;
using CollabXR.Networking;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollabXR
{
	public class SessionManager : SingletonNetworkBehavior<SessionManager>
	{
		public override void Spawned()
		{
			Debug.Log($"[SESSION MANAGER] Session Manager has spawned!");
			SessionConfig.Instance.sessionManagerSpawned.Value = true;
		}

		public void KickEveryoneElse()
		{
			RPC_Kick();
		}

		// InvokeLocal is false so the caller doesnt gets kicked
		[Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
		private void RPC_Kick()
		{
			Debug.Log("[SESSION MANAGER] Kicked from the room by another player.");

			if (NetworkManager.Instance != null)
				NetworkManager.Instance.DisconnectFromRoom();
			else
				SceneManager.LoadSceneAsync("Menu");
		}
	}
}
