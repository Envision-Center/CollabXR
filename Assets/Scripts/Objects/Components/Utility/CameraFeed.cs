using CollabXR.ModExtras.Annotation;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects.Components.Utility
{
	public class CameraFeed : NetworkBehaviour
	{
		[Header("Prefab")]
		[SerializeField]
		private Camera cameraObject;

		[SerializeField]
		private SocketAnnotation annotation;

		[Header("Settings")]
		[Networked, OnChangedRender(nameof(ApplyCameraSettings))]
		public float FocalLength { get; set; } = 35;

		[Networked, OnChangedRender(nameof(ApplyCameraSettings))]
		public float Aperture { get; set; } = 16;

		[Networked, OnChangedRender(nameof(ApplyCameraSettings))]
		public int ISO { get; set; } = 200;

		/// <summary>
		/// Shutter speed of camera in seconds.
		/// </summary>
		[Networked, OnChangedRender(nameof(ApplyCameraSettings))]
		public float ShutterSpeed { get; set; } = 0.005f;

		/// <summary>
		/// Number of data consumers watching this socket.
		/// </summary>
		private int watchers = 0;

		public int observerCount
		{
			get { return watchers; }
		}

		private RenderTexture texture;

		private void Awake()
		{
			texture = new RenderTexture(1024, 1024, 0);
			cameraObject.enabled = false;
		}

		private void OnDestroy()
		{
			texture.Release(); // Native objects must be freed
			texture = null;
		}

		/// <summary>
		/// Socket listener was added, allow the camera to render to the texture!
		/// </summary>
		public void AddWatcher()
		{
			watchers += 1;
			if (watchers > 0)
			{
				cameraObject.targetTexture = texture; // Allow camera to start rendering to texture
				annotation.texture = texture; // Pass render texture to any attached sockets
				cameraObject.enabled = true;
			}
		}

		public void RemoveWatcher()
		{
			watchers -= 1;
			if (watchers == 0)
			{
				cameraObject.targetTexture = null; // Clear render texture so camera has no overhead
				annotation.texture = null; // Don't pass the render texture anymore!
				cameraObject.enabled = false;
			}
			else if (watchers < 0)
			{
				Debug.LogError("Camera Feed: watcher count has gone below zero!");
			}
		}

		public void ApplyCameraSettings()
		{
			cameraObject.focalLength = FocalLength;
			cameraObject.aperture = Aperture;
			cameraObject.iso = ISO;
			cameraObject.shutterSpeed = ShutterSpeed;
		}
	}
}
