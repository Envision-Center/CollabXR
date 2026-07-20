using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class RadialMenuButton : MonoBehaviour
	{
		[Header("References")]
		[SerializeField]
		private RectTransform buttonTransform;

		[SerializeField]
		private Image buttonImage;

		[SerializeField]
		private Image iconImage;

		[Header("Colors")]
		[SerializeField]
		private ColorThemeEntry activeButtonColor;

		[SerializeField]
		private ColorThemeEntry activeIconColor;

		[SerializeField]
		private ColorThemeEntry inactiveButtonColor;

		[SerializeField]
		private ColorThemeEntry inactiveIconColor;

		[Header("Tween")]
		[SerializeField]
		private float tweenDuration = 0.2f;

		[SerializeField]
		private float selectedScale = 1.2f;

		[SerializeField]
		private EaseType easeType = EaseType.EaseOut;

		private void Awake()
		{
			OnDeselected();
		}

		public void OnSelected()
		{
			this.GenericTween(buttonTransform.localScale, buttonTransform.localScale, Vector3.one * selectedScale, tweenDuration, easeType, (a, b, t) => Vector3.Lerp(a, b, t));

			this.GenericTween(buttonImage.color, buttonImage.color, activeButtonColor.color, tweenDuration, easeType, (a, b, t) => Color.Lerp(a, b, t));

			this.GenericTween(iconImage.color, buttonImage.color, activeIconColor.color, tweenDuration, easeType, (a, b, t) => Color.Lerp(a, b, t));
		}

		public void OnDeselected()
		{
			this.GenericTween(buttonTransform.localScale, buttonTransform.localScale, Vector3.one, tweenDuration, easeType, (a, b, t) => Vector3.Lerp(a, b, t));

			this.GenericTween(buttonImage.color, buttonImage.color, inactiveButtonColor.color, tweenDuration, easeType, (a, b, t) => Color.Lerp(a, b, t));

			this.GenericTween(iconImage.color, iconImage.color, inactiveIconColor.color, tweenDuration, easeType, (a, b, t) => Color.Lerp(a, b, t));
		}
	}
}
