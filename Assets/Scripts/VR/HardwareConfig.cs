using UnityEngine;
using UnityEngine.XR.Management;

namespace CollabXR.VR
{
	public enum HardwareType
	{
		DepthQuest,
		NonDepthQuest,
		VisionOS,
		LinkMode,
		Desktop,
	}

	public class HardwareConfig : SingletonBehavior<HardwareConfig>
	{
		public static HardwareType type;
		private OVRPlugin.SystemHeadset headsetType;
		public GameObject OVRRig,
			AVPRig;
		public static bool IsMetaDevice => type == HardwareType.DepthQuest || type == HardwareType.NonDepthQuest;

		public static bool IsVisionOS => type == HardwareType.VisionOS;

		protected override void Awake()
		{
			base.Awake();
			if (XRGeneralSettings.Instance.Manager.activeLoader != null)
			{
				if (Application.platform == RuntimePlatform.VisionOS)
				{
					type = HardwareType.VisionOS;
				}
				else
				{
					headsetType = OVRPlugin.GetSystemHeadsetType();
					if (headsetType == OVRPlugin.SystemHeadset.Meta_Quest_3 || headsetType == OVRPlugin.SystemHeadset.Meta_Link_Quest_3)
					{
						type = HardwareType.DepthQuest;
					}
					else
					{
						type = HardwareType.NonDepthQuest;
					}
				}
			}
			else
			{
				type = HardwareType.Desktop;
			}

			Debug.Log("Hardware configuration determined to be " + type.ToString());

			Instantiate(IsVisionOS ? AVPRig : OVRRig, transform);
		}
	}
}
