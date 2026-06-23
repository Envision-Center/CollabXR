using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class ToggleUI : MonoBehaviour
	{
		public Toggle toggle;
		public Slider slider;
		public List<Toggle> priorities;
		public GameObject prioritiesList;
		public TextMeshProUGUI text;

		private Action<bool> onToggleEvent;
		private Action<float> onSlideEvent;
		private Action<int> onPriorityEvent;

		public void InitializeUI(string name, bool isEnabled, float currentTransparency, int currentPriority, Action<bool> onToggle, Action<float> onSlide, Action<int> onPriority)
		{
			onToggleEvent = onToggle;
			onSlideEvent = onSlide;
			onPriorityEvent = onPriority;

			text.text = name;
			toggle.isOn = isEnabled;
			slider.value = currentTransparency;
			UpdatePriorityUI(currentPriority);
		}

		public void OnToggle(bool enabled)
		{
			onToggleEvent.Invoke(enabled);
		}

		public void OnSlide(float value)
		{
			onSlideEvent.Invoke(value);
		}

		public void OnPriority(int priority)
		{
			onPriorityEvent.Invoke(priority);
		}

		/// <summary>
		/// Updates the Sorting Group priority list to display the given priority as active.
		/// </summary>
		/// <param name="priority">Priority to mark as active.</param>
		public void UpdatePriorityUI(int priority)
		{
			for (int i = 0; i < priorities.Count; i++)
			{
				priorities[i].isOn = i == priority;
			}
		}
	}
}
