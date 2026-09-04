using System;
using CollabXR.Networking;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CollabXR
{
	public class SessionManager : SingletonNetworkBehavior<SessionManager>, IStateAuthorityChanged
	{
		[Networked] public int brushSubStrokeCount { get; set; }
		[SerializeField] private int maximumBrushStrokeCount = 100;
		private int strokeCountBuffer = 0;
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

		// adds to local buffer and begins authority request
		private void TryEditBrushStrokeCount(int diff)
		{
			strokeCountBuffer += diff;
			Object.RequestStateAuthority();
			TryApplyStrokeBuffer();
		}

		public void AddBrushStroke()
		{
			TryEditBrushStrokeCount(1);
		}

		public void RemoveBrushStroke()
		{
			TryEditBrushStrokeCount(-1);
		}

		public bool CanAddBrushStroke()
		{
			return brushSubStrokeCount < maximumBrushStrokeCount;
		}

		private void TryApplyStrokeBuffer()
		{
			if (Object.HasStateAuthority)
			{
				brushSubStrokeCount += strokeCountBuffer;
				strokeCountBuffer = 0;
			}
		}

		public void StateAuthorityChanged()
		{
			if (Object.HasStateAuthority)
			{
				TryApplyStrokeBuffer();
			}
		}
	}
}
