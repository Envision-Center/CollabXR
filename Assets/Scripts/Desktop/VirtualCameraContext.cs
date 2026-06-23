using CollabXR.Objects;
using CollabXR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Desktop
{
	public class VirtualCameraContext : CollabContext
	{
		public TextMeshProUGUI fovText,
			qualityText,
			debugText;
		public Slider fovSlider,
			qualitySlider;
		VirtualCamera vcam;

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);
			vcam = context.GetComponent<VirtualCamera>();
			//vcam.OnContextNeedsUpdate += AdjustState;
			AdjustState();
		}

		public void ChangeFoV(float fov)
		{
			vcam.fov = fov;
			fovText.text = fov.ToString();
		}

		public void ChangeQuality(float quality)
		{
			vcam.quality = quality;
			qualityText.text = quality.ToString();
		}

		public void AdjustState()
		{
			fovSlider.value = vcam.fov;
			fovText.text = vcam.fov.ToString();
			qualitySlider.value = vcam.quality;
			qualityText.text = vcam.quality.ToString();
			debugText.text = $"{vcam.cameraName} at {vcam.cameraWidth}x{vcam.cameraHeight}";
		}
	}
}
