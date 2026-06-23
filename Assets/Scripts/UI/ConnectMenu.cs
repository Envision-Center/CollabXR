using CollabXR.Colocation;
using CollabXR.Development;
using CollabXR.Networking;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

namespace CollabXR.UI
{
	public class ConnectMenu : MonoBehaviour
	{
		[SerializeField]
		private TMP_InputField roomNameField;

		[SerializeField]
		private Button devModeButton;

		[SerializeField]
		private Button scanRoomCodeButton;

		[SerializeField]
		private string defaultRoom = "default room";

		[SerializeField]
		private Button manualConnectButton;

		private void Start()
		{
			devModeButton.onClick.AddListener(ConnectToDevRoom);

			scanRoomCodeButton.onClick.AddListener(RoomCodeScanner.Scan);
			manualConnectButton.onClick.AddListener(ConnectToRoom);
		}

		public void ConnectToRoom()
		{
			string roomName = roomNameField.text;
#if UNITY_EDITOR
			roomName = DevelopmentConfig.Instance.GetDeveloperPreferences()?.defaultRoom;
#endif
			if (roomName.IsNullOrEmpty())
				roomName = defaultRoom;

			NetworkManager.Instance?.Connect(roomName);
		}

		public void ConnectToDevRoom()
		{
			NetworkManager.Instance.defaultMode = GameMode.Single;

			ConnectToRoom();
		}
	}
}
