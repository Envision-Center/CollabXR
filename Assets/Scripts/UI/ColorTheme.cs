using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CollabXR.UI
{
	[RequireComponent(typeof(Image))]
	public class ColorTheme : MonoBehaviour
	{
		[SerializeField]
		private ColorThemeEntry colorEntry;

		private void OnValidate()
		{
			Image image = GetComponent<Image>();

			image.color = colorEntry.color;
		}
	}
}
