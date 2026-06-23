using System;
using System.Collections.Generic;
using CollabXR.Networking;
using CollabXR.VR;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace CollabXR.Colocation
{
	public class MetaQuestRoomCodeScanner : IRoomCodeScanner
	{
		private static bool isScanning;

		public event Action<bool> Toggled = delegate { };

		// todo stop after returning

		public async void Scan()
		{
			try
			{
				if (isScanning)
					return;
				isScanning = true;

				MRUK mruk = MRUK.Instance;

				if (!mruk.QRCodeTrackingSupported)
					throw new Exception("QR tracking isn't supported on this device!");

				EnableQRTracking(true);
				Toggled.Invoke(true);

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

				// align headset
				Transform t = trackedCode.transform;
				Pose pose = new(t.position, t.rotation);
				Vector3 flatForward = pose.forward;
				if (Mathf.Abs(flatForward.y) > 0.5f)
					flatForward = pose.up;
				flatForward.y = 0;
				flatForward = flatForward.normalized;
				pose.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

				Pose desiredPose = new(Vector3.zero, Quaternion.identity);
				HardwareRig.Instance.TransformPlayspace(pose, desiredPose);
				ColocationDriver.IsAnchoredViaCodeTempTodoRemoveThis = true;

				// connect to room
				string roomCode = trackedCode.MarkerPayloadString;
				if (roomCode == null)
					throw new Exception("QR code does not contain text usable as a room code!");
				if (!NetworkManager.Runner.IsConnectedToServer)
					NetworkManager.Instance.Connect(roomCode);
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
			EnableQRTracking(false);
			Toggled.Invoke(false);
		}

		private static void EnableQRTracking(bool b)
		{
			MRUK.Instance.SceneSettings.TrackerConfiguration = new OVRAnchor.TrackerConfiguration { QRCodeTrackingEnabled = b };
		}
	}
}
