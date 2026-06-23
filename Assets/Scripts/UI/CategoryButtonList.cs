using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CollabXR.UI
{
	public class CategoryButtonList : MonoBehaviour
	{
		[SerializeField]
		private CategoryButton categoryButtonPrefab;

		[SerializeField]
		private Transform buttonContainerTransform;

		private readonly List<CategoryButton> buttons = new();

		public void InstantiateCategoryButton(string label, Action onClick)
		{
			GameObject g = Instantiate(categoryButtonPrefab.gameObject, buttonContainerTransform);

			CategoryButton button = g.GetComponent<CategoryButton>();
			button.SetText(label);
			button.OnClick.AddListener(onClick.Invoke);
			button.OnClick.AddListener(ResetAllButtonSelectedVisuals);
			button.OnClick.AddListener(button.SetSelectedVisual);

			buttons.Add(button);
		}

		public void Select(int index)
		{
			if (buttons.Count > index)
				buttons[index].OnClick.Invoke();
		}

		private void ResetAllButtonSelectedVisuals()
		{
			foreach (CategoryButton button in buttons)
			{
				button.SetSelectedVisual(false);
			}
		}

		public void ResetCategoryButtons()
		{
			foreach (CategoryButton button in buttons)
			{
				Destroy(button.gameObject);
			}

			buttons.Clear();
		}
	}
}
