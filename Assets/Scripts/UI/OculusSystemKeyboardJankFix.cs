using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace CollabXR
{
	public class OculusSystemKeyboardJankFix : MonoBehaviour
	{
		private TMP_InputField field;

		private void Awake()
		{
			field = GetComponent<TMP_InputField>();

			field.onSelect.AddListener(s => ShowKeyboardDelay());
		}

		private async void ShowKeyboardDelay()
		{
			await Task.Delay(500);

			if (!TouchScreenKeyboard.visible)
			{
				field.ActivateInputField();
				TouchScreenKeyboard.Open(field.text, TouchScreenKeyboardType.ASCIICapable);
			}
		}
	}
}
