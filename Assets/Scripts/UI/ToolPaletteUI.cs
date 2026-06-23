using CollabXR.Tools;
using CollabXR.Tools.Palette;
using UnityEngine;
using UnityEngine.XR;

namespace CollabXR.UI
{
	public class ToolPaletteUI : MonoBehaviour
	{
		[SerializeField]
		private HandedButton[] buttons;

		[SerializeField]
		ToolPalette targetToolPalette;

		private void Awake()
		{
			for (int i = 0; i < buttons.Length; i++)
			{
				int iCap = i;

				buttons[i]
					.onClick.AddListener(
						delegate()
						{
							targetToolPalette?.ActivateTool(iCap);
						}
					);
			}
		}
	}
}
