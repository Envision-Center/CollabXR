using CollabXR.Desktop;
using CollabXR.Environments;
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
			PassthroughOn.Value = HardwareConfig.IsMetaDevice ? OVRManager.IsPassthroughRecommended() : passthroughOnInitial;
			OcclusionMethod.Value = occlusionMethodInitial;
			Debug.Log($"Starting passthrough.. {PassthroughOn.Value}");
		}

		private void OnSetPassthrough(bool b)
		{
			Debug.Log("Passthrough: " + PassthroughOn.Value);
			if (HardwareConfig.IsMetaDevice)
			{
				OVRManager.instance.isInsightPassthroughEnabled = PassthroughOn.Value;
				if (PassthroughOn.Value)
					WaitingForCameraHardwareDelay = true;
			}

			UpdateSkyboxVisibility();
			UpdateOcclusionSystem();
		}

		public void SetSkyboxOnInPassthrough(bool b)
		{
			SkyboxOnInPassthrough = b;
			UpdateSkyboxVisibility();
		}

		public void FinishWaitingForCameraHardwareDelay(OVRPassthroughLayer layer)
		{
			WaitingForCameraHardwareDelay = false;
			UpdateSkyboxVisibility();
		}

		private void UpdateSkyboxVisibility()
		{
			if (!WaitingForCameraHardwareDelay && HardwareConfig.type != HardwareType.Desktop)
			{
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
			bool occlusionOn = PassthroughOn.Value && OcclusionMethod.Value == OcclusionMethods.LiveDepth && HardwareConfig.type == HardwareType.DepthQuest;

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
