using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class CategoryButton : MonoBehaviour
	{
		[SerializeField]
		private Button button;

		[SerializeField]
		private TMP_Text label;

		[SerializeField]
		private Image selectedIndicator;

		public UnityEvent OnClick => button.onClick;

		private void Awake()
		{
			SetSelectedVisual(false);
		}

		public void SetText(string str)
		{
			label.text = str;
		}

		public void SetSelectedVisual(bool b)
		{
			selectedIndicator.enabled = b;
		}

		public void SetSelectedVisual() => SetSelectedVisual(true);
	}
}
