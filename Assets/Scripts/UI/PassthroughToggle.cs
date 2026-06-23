using CollabXR.VR;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	[RequireComponent(typeof(Toggle))]
	public class PassthroughToggle : MonoBehaviour
	{
		private Toggle toggle;

		private void Awake()
		{
			toggle = GetComponent<Toggle>();
		}

		private void Start()
		{
			toggle.onValueChanged.AddListener(PassthroughManager.PassthroughOn.Set);
		}

		private void OnEnable()
		{
			toggle.SetIsOnWithoutNotify(PassthroughManager.PassthroughOn.Value);
			PassthroughManager.PassthroughOn.AddListenerAndCheck(toggle.SetIsOnWithoutNotify);
		}

		private void OnDisable()
		{
			PassthroughManager.PassthroughOn.RemoveListener(toggle.SetIsOnWithoutNotify);
		}
	}
}
