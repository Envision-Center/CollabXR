using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Scriptables
{
	public class ScriptableBoolToggle : MonoBehaviour
	{
		[SerializeField]
		private ScriptableBool scriptableBool;
		private Toggle toggle;

		private void Awake()
		{
			toggle = GetComponent<Toggle>();

			toggle.onValueChanged.AddListener(scriptableBool.Set);
			scriptableBool.AddChangeListenerAndCheck(toggle.SetIsOnWithoutNotify);
		}

		private void OnDestroy()
		{
			scriptableBool.onChange -= toggle.SetIsOnWithoutNotify;
		}
	}
}
