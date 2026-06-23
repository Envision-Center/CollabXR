using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace CollabXR.VR
{
	public class HardwareRigEvents : MonoBehaviour
	{
		[FormerlySerializedAs("onRigInstantiated")]
		public UnityEvent<Transform> onInstantiateHead;

		public UnityEvent<Transform> onInstantiateHandLeft;
		public UnityEvent<Transform> onInstantiateHandRight;

		private void Awake()
		{
			if (HardwareRig.Instance != null)
				OnInstantiate();
			else
				HardwareRig.OnHardwareRigStart += OnInstantiate;
		}

		private void OnDestroy()
		{
			HardwareRig.OnHardwareRigStart -= OnInstantiate;
		}

		private void OnInstantiate()
		{
			onInstantiateHead.Invoke(HardwareRig.Instance.actualHead);
			onInstantiateHandLeft.Invoke(HardwareRig.Instance.actualLeftHand);
			onInstantiateHandRight.Invoke(HardwareRig.Instance.actualRightHand);
		}
	}
}
