using System;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CollabXR.Desktop
{
	public class Webcam : SingletonBehavior<Webcam>
	{
		public Material mat;
		public int deviceIndex;
		public string currentDeviceName;
		public int currentWidth,
			currentHeight;
		WebCamTexture webcamTexture;
		Material matInstance;
		float quality = 1.0f;
		float maxHeight = 720;

		private void Start()
		{
			matInstance = new Material(mat);
			if (HardwareConfig.type == HardwareType.Desktop)
			{
				PassthroughManager.PassthroughOn.AddListener(TogglePassthrough);
			}
		}

		private void TogglePassthrough(bool passthrough)
		{
			SetCameraFeed();
		}

		private void SetCameraFeed()
		{
			KillCamera();
			if (PassthroughManager.PassthroughOn.Value)
			{
				WebCamDevice[] devices = WebCamTexture.devices;
				foreach (WebCamDevice device in devices)
				{
					Debug.Log("Device found:" + device.name);
				}
				if (deviceIndex >= devices.Length)
					deviceIndex = 0;
				WebCamDevice target = devices[deviceIndex];
				float width = Mathf.Floor(Camera.main.pixelWidth * quality);
				float height = Mathf.Floor(Camera.main.pixelHeight * quality);
				float scaleRatio = height > maxHeight ? maxHeight / height : 1.0f;
				width *= scaleRatio;
				height *= scaleRatio;
				webcamTexture = new WebCamTexture(target.name, (int)width, (int)height, 30);
				webcamTexture.Play();
				matInstance.mainTexture = webcamTexture;
				RenderSettings.skybox = matInstance;
				currentDeviceName = target.name;
				currentWidth = (int)width;
				currentHeight = (int)height;
				Debug.Log($"Requesting device: {currentDeviceName} at resolution {currentWidth}x{currentHeight} at {webcamTexture.requestedFPS} FPS");
			}
		}

		public void OnCameraWarp(InputValue value)
		{
			try
			{
				GameObject.FindAnyObjectByType<VirtualCamera>().ToggleCameraLock();
			}
			catch (NullReferenceException e)
			{
				Debug.Log("No virtual camera found.");
			}
		}

		public void OnCameraCycle(InputValue value)
		{
			if (PassthroughManager.PassthroughOn.Value)
			{
				deviceIndex++;
				SetCameraFeed();
			}
		}

		public Material GetWebcamSkybox()
		{
			return matInstance;
		}

		public void SetQuality(float value)
		{
			quality = value;
			SetCameraFeed();
		}

		void KillCamera()
		{
			if (webcamTexture != null && webcamTexture.isPlaying)
			{
				Debug.Log("Killing camera.");
				webcamTexture.Stop();
			}
		}

		private void OnApplicationQuit()
		{
			KillCamera();
		}
	}
}
