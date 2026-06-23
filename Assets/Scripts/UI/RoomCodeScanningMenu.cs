using CollabXR.Colocation;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class RoomCodeScanningMenu : MonoBehaviour
	{
		[SerializeField]
		private Button cancelButton;

		[SerializeField]
		private GameObject cancelMenuSuccessor;

		private void Start()
		{
			RoomCodeScanner.Toggled += OnScanToggled;

			cancelButton.onClick.AddListener(RoomCodeScanner.Stop);
		}

		private void OnDestroy()
		{
			RoomCodeScanner.Toggled -= OnScanToggled;
		}

		public void Scan() => RoomCodeScanner.Scan();

		private void OnScanToggled(bool on)
		{
			if (on)
				gameObject.SetActive(true);
			else
				cancelMenuSuccessor.SetActive(true);
		}
	}
}
