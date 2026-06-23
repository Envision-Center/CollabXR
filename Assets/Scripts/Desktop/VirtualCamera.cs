using System;
using CollabXR.Colocation;
using CollabXR.Desktop;
using CollabXR.Networking;
using CollabXR.VR;
using Fusion;
using UnityEngine;

namespace CollabXR.Desktop
{
	public class VirtualCamera : NetworkBehaviour
	{
		[Networked, OnChangedRender(nameof(UpdateIntrinsics))]
		public float fov { get; set; } = 50.0f;

		[Networked, OnChangedRender(nameof(UpdateWebcam))]
		public float quality { get; set; } = 1.0f;

		[Networked]
		public NetworkString<_32> cameraName { get; set; }

		[Networked]
		public int cameraWidth { get; set; }

		[Networked]
		public int cameraHeight { get; set; }
		public GameObject cameraModel;
		public event Action OnContextNeedsUpdate = delegate { };
		bool hasWarped; // locks intrinsics to this camera for the rest of the session

		public override void Spawned() // todo: figure out why the defaults above aren't working
		{
			base.Spawned();
			fov = 50.0f;
			quality = 1.0f;
		}

		private void Update()
		{
			if (hasWarped)
			{
				DesktopController.Instance.WarpTo(transform);
				cameraName = Webcam.Instance.name;
				cameraWidth = Webcam.Instance.currentWidth;
				cameraHeight = Webcam.Instance.currentHeight;
			}
		}

		public void ToggleCameraLock()
		{
			hasWarped = !hasWarped;
			cameraModel.SetActive(!hasWarped);
			ColocationDriver.IsAnchoredViaVirtualCamera.Value = hasWarped;
		}

		public void UpdateIntrinsics()
		{
			if (hasWarped)
			{
				Camera.main.fieldOfView = fov;
			}
			OnContextNeedsUpdate.Invoke();
		}

		public void UpdateWebcam()
		{
			if (hasWarped)
			{
				Webcam.Instance.SetQuality(quality);
			}
			OnContextNeedsUpdate.Invoke();
		}
	}
}
