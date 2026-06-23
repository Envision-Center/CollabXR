using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Avatar
{
	public class AvatarDataButton : MonoBehaviour
	{
		public Image fgImg,
			bgImg;
		public Color myColor;
		public int myIndex;
		protected AvatarDataPicker picker;

		public void Setup(AvatarDataPicker p, Vector2 cellSize)
		{
			picker = p;
			float border = picker.options.borderSize;
			fgImg.GetComponent<RectTransform>().anchorMin = new Vector2(border, border);
			fgImg.GetComponent<RectTransform>().anchorMax = new Vector2(1 - border, 1 - border);
		}

		public void Setup(AvatarDataPicker p, Color c, Vector2 cellSize)
		{
			Setup(p, cellSize);
			myColor = c;
			fgImg.color = myColor;
			Unselect();
		}

		public void Setup(AvatarDataPicker p, int i, Vector2 cellSize)
		{
			Setup(p, cellSize);
			myColor = Color.white;
			myIndex = i;
			fgImg.color = myColor;
			fgImg.sprite = picker.options.sprites[myIndex];
			Unselect();
		}

		public void Unselect()
		{
			bgImg.color = picker.options.unselected;
		}

		public void Select()
		{
			bgImg.color = picker.options.selected;
		}

		public void Click()
		{
			picker.Pick(this);
		}
	}
}
