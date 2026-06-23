using UnityEngine;

namespace CollabXR.Oculus
{
	public class FoveatedRenderingEnabler : MonoBehaviour
	{
		private void Start()
		{
			Debug.Log("Foveated");
			OVRManager.foveatedRenderingLevel = OVRManager.FoveatedRenderingLevel.High;
			OVRManager.useDynamicFoveatedRendering = true;

			OVRPlugin.foveatedRenderingLevel = OVRPlugin.FoveatedRenderingLevel.High;
			OVRPlugin.useDynamicFoveatedRendering = true;

			OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
			OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
		}
	}
}
