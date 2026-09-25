using CollabXR.Desktop;
using CollabXR.Environments;
using CollabXR.EnvironmentExtras;
using CollabXR.ModExtras;
using Meta.XR.EnvironmentDepth;
using UnityEngine;

namespace CollabXR.VR
{
	public enum OcclusionMethods
	{
		Basic,
		LiveDepth,
	}

	public class PassthroughManager : SingletonBehavior<PassthroughManager>
	{
		[SerializeField]
		private Camera mainCamera;

		[SerializeField]
		private OVRPassthroughLayer ovrLayer;

		public EnvironmentDepthManager depthManager;

		[SerializeField]
		private bool passthroughOnInitial;

		[SerializeField]
		private OcclusionMethods occlusionMethodInitial;

		public static bool SkyboxOnInPassthrough;
		public static bool WaitingForCameraHardwareDelay;
		public static readonly EventVariable<bool> PassthroughOn = new();
		public static readonly EventVariable<OcclusionMethods> OcclusionMethod = new();
		Material storedSkybox;
		protected override void Awake()
		{
			base.Awake();

			PassthroughOn.AddListener(OnSetPassthrough);
			OcclusionMethod.AddListener(OnOcclusionMethodChange);
			if (HardwareConfig.IsMetaDevice)
			{
				ovrLayer.passthroughLayerResumed.AddListener(FinishWaitingForCameraHardwareDelay);
			}
			else if (HardwareConfig.type == HardwareType.Desktop)
			{
				EnvironmentManager.Instance?.OnEnvironmentLoadComplete.AddListener(SwapEnvironmentSkybox);
			}
		}

		private void Start()
		{			
			Debug.Log($"Starting passthrough.. {PassthroughOn.Value}");
			PassthroughOn.Value = HardwareConfig.IsMetaDevice ? OVRManager.IsPassthroughRecommended() : passthroughOnInitial;			
			OcclusionMethod.Value = occlusionMethodInitial;
			SetOcclusionLiveDepth(OcclusionMethod.Value == OcclusionMethods.LiveDepth);
			UpdateOcclusionSystem();
			OnSetPassthrough(PassthroughOn.Value);
		}

		private void OnSetPassthrough(bool b)
		{
			Debug.Log("Setting Passthrough: " + PassthroughOn.Value);
			if (HardwareConfig.IsMetaDevice)
			{
				OVRManager.instance.isInsightPassthroughEnabled = PassthroughOn.Value;
				if (PassthroughOn.Value)
					WaitingForCameraHardwareDelay = true;
			}

			UpdateSkyboxVisibility();
			SetOcclusionLiveDepth(PassthroughOn.Value);
			TriggerScenePassthroughEvents();
			UpdateOcclusionSystem();
		}

		public void TriggerScenePassthroughEvents()
		{
			EnvironmentScene scene = EnvironmentManager.Instance.currentEnvInstance;
			if (scene)
			{
				Debug.Log("PassthroughManager: Triggering environment passthrough change event");
				scene.passthroughEvents.HandlePassthroughChange(PassthroughOn.Value);
			}
		}

		public void SetSkyboxOnInPassthrough(bool b)
		{
			Debug.Log("Setting SkyboxOnInPassthrough: " + b);
			SkyboxOnInPassthrough = b;
			UpdateSkyboxVisibility();
		}

		public void FinishWaitingForCameraHardwareDelay(OVRPassthroughLayer layer)
		{
			Debug.Log("Finishing waiting for camera hardware delay");
			WaitingForCameraHardwareDelay = false;
			UpdateSkyboxVisibility();
		}

		private void UpdateSkyboxVisibility()
		{
			Debug.Log("Trying to update skybox visibility");
			if (!WaitingForCameraHardwareDelay && HardwareConfig.type != HardwareType.Desktop)
			{
				Debug.Log($"Updating skybox visibility: PassthroughOn={PassthroughOn.Value}, SkyboxOnInPassthrough={SkyboxOnInPassthrough}");
				mainCamera.clearFlags = PassthroughOn.Value && !SkyboxOnInPassthrough ? CameraClearFlags.Color : CameraClearFlags.Skybox;
				mainCamera.backgroundColor = Color.clear;
			}
		}

		private void OnOcclusionMethodChange(OcclusionMethods method)
		{
			UpdateOcclusionSystem();
		}

		private void UpdateOcclusionSystem()
		{
			Debug.Log("Updating occlusion system");
			bool liveDepthSupported = depthManager != null && EnvironmentDepthManager.IsSupported;
			bool occlusionOn = PassthroughOn.Value && OcclusionMethod.Value == OcclusionMethods.LiveDepth && liveDepthSupported;

			if (depthManager == null)
			{
				Debug.LogWarning("PassthroughManager: no EnvironmentDepthManager assigned, live depth occlusion is unavailable.");
				return;
			}

			if (occlusionOn)
			{
				depthManager.enabled = true;
				depthManager.OcclusionShadersMode = OcclusionShadersMode.SoftOcclusion;
			}
			else
			{
				depthManager.enabled = false;
				depthManager.OcclusionShadersMode = OcclusionShadersMode.None;
			}
		}

		public void SwapEnvironmentSkybox()
		{
			storedSkybox = RenderSettings.skybox;
			if (HardwareConfig.type == HardwareType.Desktop && !PassthroughOn.Value)
			{
				storedSkybox = Webcam.Instance.GetWebcamSkybox();
			}
		}

		public void TogglePassthrough() => PassthroughOn.Value = !PassthroughOn.Value;
		public void SetOcclusionLiveDepth(bool value)
		{
			OcclusionMethod.Value = value ? OcclusionMethods.LiveDepth : OcclusionMethods.Basic;
		}

		public void AddDepthMask(DepthMask mesh)
		{
			depthManager.MaskMeshFilters.AddRange(mesh.filters);
			mesh.OnMeshDestroyed.AddListener(
				delegate
				{
					foreach (MeshFilter filter in mesh.filters)
					{
						depthManager.MaskMeshFilters.Remove(filter);
					}
				}
			);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}
	}
}
