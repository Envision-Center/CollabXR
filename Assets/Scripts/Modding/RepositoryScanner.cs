using System;
using System.Collections.Generic;
using CollabXR.Colocation;
using CollabXR.ModLoader;
using CollabXR.Networking;
using CollabXR.VR;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Modding
{
	public class RepositoryScanner : MonoBehaviour
	{
		public TMP_InputField newRepositoryField;
		public TextMeshProUGUI warningLabel;
		public Image scanButton;
		public Color scanningColor;
		private static bool isScanning;

		private Color defaultColor;

		private void Start()
		{
			defaultColor = scanButton.color;
		}

		private void OnEnable()
		{
			warningLabel.text = "";
		}

		public void AddNewRepository()
		{
			RepositoryManager.RepositoryAddResult result = RepositoryManager.AddRepository(newRepositoryField.text, true);
			newRepositoryField.text = "";
			switch (result)
			{
				case RepositoryManager.RepositoryAddResult.Success:
					warningLabel.text = "Repository added.";
					break;
				case RepositoryManager.RepositoryAddResult.Duplicate:
					warningLabel.text = "Repository already loaded.";
					break;
				case RepositoryManager.RepositoryAddResult.InvalidURL:
					warningLabel.text = "Repository must be a valid URL.";
					break;
			}
		}

		public void RefreshRepositories()
		{
			RepositoryManager.RefreshAllMods();
		}

		public void ToggleScanning()
		{
			if (isScanning)
			{
				Stop();
			}
			else
			{
				Scan();
			}
		}

		public async void Scan()
		{
			try
			{
				if (isScanning)
					return;
				isScanning = true;
				scanButton.color = scanningColor;

				MRUK mruk = MRUK.Instance;

				if (!mruk.QRCodeTrackingSupported)
					throw new Exception("QR tracking isn't supported on this device!");

				MRUK.Instance.SceneSettings.TrackerConfiguration = new OVRAnchor.TrackerConfiguration { QRCodeTrackingEnabled = true };

				List<MRUKTrackable> allTracked = new();
				MRUKTrackable trackedCode = null;

				while (trackedCode == null)
				{
					if (!isScanning)
						return;
					await Awaitable.NextFrameAsync();

					mruk.GetTrackables(allTracked);
					foreach (MRUKTrackable trackable in allTracked)
					{
						if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode)
						{
							trackedCode = trackable;
							break;
						}
					}
				}

				newRepositoryField.text = trackedCode.MarkerPayloadString;
			}
			finally
			{
				Stop();
			}
		}

		public void Stop()
		{
			if (!isScanning)
				return;
			isScanning = false;
			MRUK.Instance.SceneSettings.TrackerConfiguration = new OVRAnchor.TrackerConfiguration { QRCodeTrackingEnabled = false };
			scanButton.color = defaultColor;
		}
	}
}
