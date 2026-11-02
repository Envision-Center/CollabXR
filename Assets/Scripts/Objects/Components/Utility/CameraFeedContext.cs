using CollabXR.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Objects.Components.Utility
{
	public class CameraFeedContext : CollabContext
	{
		private CameraFeed feed;

		[SerializeField]
		private Slider focalLength;

		[SerializeField]
		private Slider aperture;

		[SerializeField]
		private Slider iso;

		[SerializeField]
		private Slider shutterSpeedInv;

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);
			feed = context.GetComponent<CameraFeed>();
			UpdateDisplay();
		}

		public override void OnStateAuthorityChanged()
		{
			UpdateDisplay();
		}

		private void UpdateDisplay()
		{
			focalLength.value = feed.FocalLength;
			aperture.value = feed.Aperture;
			iso.value = feed.ISO;
			shutterSpeedInv.value = 1.0f / feed.ShutterSpeed;
		}

		public void TweakFocalLength(float newValue)
		{
			Debug.Log($"Tweak focal length: {newValue}, {feed.HasStateAuthority}");
			if (feed.HasStateAuthority)
			{
				feed.FocalLength = newValue;
				feed.ApplyCameraSettings();
			}
		}

		public void TweakAperture(float newValue)
		{
			if (feed.HasStateAuthority)
			{
				feed.Aperture = newValue;
				feed.ApplyCameraSettings();
			}
		}

		public void TweakISO(float newValue)
		{
			if (feed.HasStateAuthority)
			{
				feed.ISO = (int)newValue;
				feed.ApplyCameraSettings();
			}
		}

		public void TweakShutterSpeed(float newValue)
		{
			if (feed.HasStateAuthority)
			{
				feed.ShutterSpeed = 1.0f / (float)newValue;
				feed.ApplyCameraSettings();
			}
		}
	}
}
