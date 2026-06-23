using CollabXR.VR;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	[RequireComponent(typeof(Toggle))]
	public class OcclusionMethodToggle : MonoBehaviour
	{
		private Toggle toggle;

		private void Awake()
		{
			toggle = GetComponent<Toggle>();
		}

		private void Start()
		{
			toggle.onValueChanged.AddListener(EnableExperimentalDepthOcclusion);
		}

		private void EnableExperimentalDepthOcclusion(bool b)
		{
			PassthroughManager.OcclusionMethod.Value = b ? OcclusionMethods.LiveDepth : OcclusionMethods.Basic;
		}

		private void OnDepthOcclusionMethodChange(OcclusionMethods method) => toggle.SetIsOnWithoutNotify(PassthroughManager.OcclusionMethod.Value == OcclusionMethods.LiveDepth);

		private void OnEnable()
		{
			OnDepthOcclusionMethodChange(PassthroughManager.OcclusionMethod.Value);
			PassthroughManager.OcclusionMethod.AddListener(OnDepthOcclusionMethodChange);
		}

		private void OnDisable()
		{
			PassthroughManager.OcclusionMethod.RemoveListener(OnDepthOcclusionMethodChange);
		}
	}
}
