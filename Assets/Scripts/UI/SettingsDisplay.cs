using System;
using CollabXR.Development;
using CollabXR.Networking;
using Fusion;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace CollabXR.UI
{
	public class SettingsDisplay : MonoBehaviour
	{
		public TextMeshProUGUI settingsInfo;

		private void OnEnable()
		{
			string version = $"{Application.platform} v{Application.version}\nBuild ID {DevelopmentConfig.Instance.GetBuildInformation()?.ShortenedGuid()}\n";
			if (NetworkManager.Instance != null && NetworkManager.Runner.IsInSession)
			{
				version += $"Connected to Room: [{NetworkManager.Runner.SessionInfo.Name}]\nRole: [{NetworkPlayer.GetLocalRole()}]";
			}
			else
			{
				version += $"Connect to Lobby";
			}
			settingsInfo.text = version;
		}
	}
}
