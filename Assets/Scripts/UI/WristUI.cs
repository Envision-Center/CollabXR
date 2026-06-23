using CollabXR.Networking;
using CollabXR.Objects;
using TMPro;
using UnityEngine;
using WebSocketSharp;

namespace CollabXR.UI
{
	public class WristUI : MonoBehaviour
	{
		public Transform follow;
		public TextMeshProUGUI dataDisplay;

		private CollabObject spawnableContext;

		// Start is called before the first frame update
		private void Start() { }

		// Update is called once per frame
		private void Update()
		{
			transform.position = follow.position;
			transform.rotation = follow.rotation;
		}

		public void UpdateDataDisplay(CollabObject spawnable)
		{
			if (spawnable != null)
			{
				string dataName = spawnable.GetWristText();
				dataDisplay.text = dataName;
				dataDisplay.transform.parent.gameObject.SetActive(!dataName.IsNullOrEmpty());
				spawnableContext = spawnable;
			}
			else
			{
				dataDisplay.text = "";
				dataDisplay.transform.parent.gameObject.SetActive(false);
				spawnableContext = null;
			}
		}

		public void ToggleContextMenu() { }

		public void ToggleToolContextMenu() { }
	}
}
