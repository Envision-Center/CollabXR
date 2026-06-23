using CollabXR.Avatar;
using UnityEngine;
using UnityEngine.UI;
using AvatarBuilder = CollabXR.Avatar.AvatarBuilder;

namespace CollabXR.Avatar
{
	public class AvatarDataPicker : MonoBehaviour
	{
		public AvatarBuilder avatarBuilder;
		public AvatarDataType type;
		public AvatarDataOption options;
		public GameObject buttonPrefab;
		private Vector2 cellSize;
		private AvatarDataButton selected;

		private void Start()
		{
			cellSize = GetComponent<GridLayoutGroup>().cellSize;
			for (int i = 0; i < (IsColor() ? options.colors.Count : options.sprites.Count); ++i)
			{
				AvatarDataButton newButton = Instantiate(buttonPrefab).GetComponent<AvatarDataButton>();
				if (IsColor())
					newButton.Setup(this, options.colors[i], cellSize);
				else
					newButton.Setup(this, i, cellSize);
				newButton.transform.SetParent(transform);
				newButton.transform.localPosition = Vector3.zero;
				newButton.GetComponent<RectTransform>().localScale = Vector3.one;
				newButton.GetComponent<RectTransform>().localRotation = Quaternion.identity;
				bool isSelected = IsColor() ? avatarBuilder.GetColor(type) == options.colors[i] : avatarBuilder.GetStyle(type) == i;
				if (isSelected)
				{
					selected = newButton;
					newButton.Select();
				}
			}
		}

		public void Pick(AvatarDataButton button)
		{
			if (selected != null)
				selected.Unselect();
			selected = button;
			selected.Select();
			if (IsColor())
			{
				Color c = button.myColor;
				avatarBuilder.SetColor(type, c);
			}
			else
			{
				int index = button.myIndex;
				avatarBuilder.SetStyle(type, index);
			}
		}

		private bool IsColor()
		{
			return type == AvatarDataType.ShirtColor || type == AvatarDataType.SkinColor || type == AvatarDataType.HairColor;
		}
	}
}
