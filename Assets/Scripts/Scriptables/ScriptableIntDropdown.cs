using TMPro;
using UnityEngine;

namespace CollabXR.Scriptables
{
	public class ScriptableIntDropdown : MonoBehaviour
	{
		[SerializeField]
		private ScriptableInt scriptableInt;
		private TMP_Dropdown dropdown;

		private void Awake()
		{
			dropdown = GetComponent<TMP_Dropdown>();

			dropdown.value = scriptableInt.Value;
			dropdown.onValueChanged.AddListener(scriptableInt.Set);
			scriptableInt.AddChangeListenerAndCheck(dropdown.SetValueWithoutNotify);
		}

		private void OnDestroy()
		{
			scriptableInt.onChange -= dropdown.SetValueWithoutNotify;
		}
	}
}
