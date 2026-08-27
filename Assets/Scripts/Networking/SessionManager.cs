using System;
using CollabXR.Networking;
using CollabXR.UI;
using Fusion;
using UnityEngine;

namespace CollabXR
{
	public class SessionManager : SingletonNetworkBehavior<SessionManager>
	{
		private GameMenu _menuToPreserve;

		public override void Spawned()
		{
			Debug.Log($"[SESSION MANAGER] Session Manager has spawned!");
			SessionConfig.Instance.sessionManagerSpawned.Value = true;
		}

		public void KickAllExcept(GameMenu menu)
		{
			_menuToPreserve = menu;
			RPC_Kick();
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		private void RPC_Kick()
		{
			GameMenu[] gameMenus = FindObjectsOfType<GameMenu>();
			foreach (GameMenu menu in gameMenus)
			{
				if (menu == _menuToPreserve)
					continue;

				menu.ActionDisconnect();
			}
		}
	}
}
