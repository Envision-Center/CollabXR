using CollabXR.Networking;
using TMPro;
using UnityEngine;

namespace CollabXR
{
    public class LoadingPopup : MonoBehaviour
    {
		public TextMeshProUGUI textField;
		string roomConnection;

		private void Update()
		{
			UpdatePopup();
		}

		private void UpdatePopup()
		{
			if(NetworkManager.Runner != null && NetworkManager.Runner.IsInSession)
			{
				string roomConnection = $"Room: {NetworkManager.Runner.SessionInfo.Name}\n";
				roomConnection += $"{NetworkManager.playerCount} players.\n";
				roomConnection += $"{NetworkManager.networkObjectCount} objects found.";
				textField.text = roomConnection;
			}
			else
			{
				textField.text = "Waiting for connection...";
			}
		}
	}
}
