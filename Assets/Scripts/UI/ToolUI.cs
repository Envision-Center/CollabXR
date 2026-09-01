using System;
using CollabXR.Tools;
using CollabXR.Tools.Palette;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace CollabXR.UI
{
	public class ToolUI : MonoBehaviour
	{
		[SerializeField]
		private HandedButton[] buttons;

		[SerializeField]
		private Image leftHandToolIcon,
			rightHandToolIcon;

		private void Awake()
		{
			for (int i = 0; i < buttons.Length; i++)
			{
				int iCap = i;
				HandedButton b = buttons[i];

				b.onClickIsRight.AddListener(
					delegate(bool isRight)
					{
						ToolPalette.Get(isRight)?.ActivateTool(iCap);

						GetComponentInParent<GameMenu>()?.gameObject.SetActive(false);
						Image imgToChange = isRight ? rightHandToolIcon : leftHandToolIcon;
						if (imgToChange)
						{
							imgToChange.sprite = b.GetComponent<Button>().targetGraphic.GetComponent<Image>().sprite;
						}
					}
				);
			}
		}
	}
}
