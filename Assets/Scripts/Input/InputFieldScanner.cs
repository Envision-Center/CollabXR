using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR
{
    public class InputFieldScanner : MonoBehaviour
	{
		public TMP_InputField targetField;
		public Image scanButton;
		public Color scanningColor;
		private static bool isScanning;
		private Color defaultColor;

		private void Start()
		{
			defaultColor = scanButton.color;
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

				targetField.text = trackedCode.MarkerPayloadString;
				targetField.onEndEdit.Invoke(targetField.text);
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
