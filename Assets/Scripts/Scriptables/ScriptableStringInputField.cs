using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Scriptables
{
	public class ScriptableStringInputField : MonoBehaviour
	{
		[SerializeField]
		private ScriptableString scriptableString;
		private TMP_InputField field;

		private void Awake()
		{
			field = GetComponent<TMP_InputField>();

			field.onValueChanged.AddListener(scriptableString.Set);
			scriptableString.AddChangeListenerAndCheck(field.SetTextWithoutNotify);
		}

		private void OnDestroy()
		{
			scriptableString.onChange -= field.SetTextWithoutNotify;
		}
	}
}
