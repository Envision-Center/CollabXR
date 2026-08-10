using System;
using CollabXR.Networking;
using Fusion;
using UnityEngine;

namespace CollabXR
{
    public class SessionManager : SingletonNetworkBehavior<SessionManager>
    {
		public static EventVariable<bool> sessionManagerSpawned = new();
		public override void Spawned()
		{
			Debug.Log($"[SESSION MANAGER] Session Manager has spawned!");
			sessionManagerSpawned.Value = true;
		}
    }
}
