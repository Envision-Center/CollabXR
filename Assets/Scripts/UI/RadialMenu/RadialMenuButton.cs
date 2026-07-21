using CollabXR.Tools;
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

		[Header("Selection")]
		[SerializeField]
		private int toolIndex;

		[SerializeField]
		private float minAngle;

		[SerializeField]
		private float maxAngle;

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

		public void OnSelected()
		{
			this.GenericTween(buttonTransform, buttonTransform.localScale, Vector3.one * selectedScale, tweenDuration, easeType, v => buttonTransform.localScale = v, (a, b, t) => Vector3.Lerp(a, b, t));

			this.GenericTween(buttonImage, buttonImage.color, activeButtonColor.color, tweenDuration, easeType, c => buttonImage.color = c, (a, b, t) => Color.Lerp(a, b, t));

			this.GenericTween(iconImage, iconImage.color, activeIconColor.color, tweenDuration, easeType, c => iconImage.color = c, (a, b, t) => Color.Lerp(a, b, t));
		}

		public void OnDeselected()
		{
			this.GenericTween(buttonTransform, buttonTransform.localScale, Vector3.one, tweenDuration, easeType, v => buttonTransform.localScale = v, (a, b, t) => Vector3.Lerp(a, b, t));

			this.GenericTween(buttonImage, buttonImage.color, inactiveButtonColor.color, tweenDuration, easeType, c => buttonImage.color = c, (a, b, t) => Color.Lerp(a, b, t));

			this.GenericTween(iconImage, iconImage.color, inactiveIconColor.color, tweenDuration, easeType, c => iconImage.color = c, (a, b, t) => Color.Lerp(a, b, t));
		}

		public void OnClicked(bool isRightHand)
		{
			// TODO: animation stuff
			ToolPalette.Get(isRightHand)?.ActivateTool(toolIndex);
		}

		public bool IsAngleInRange(float angle)
		{
			angle = NormalizeAngle(angle);
			float min = NormalizeAngle(minAngle);
			float max = NormalizeAngle(maxAngle);

			if (min <= max)
				return angle >= min && angle < max;

			return angle >= min || angle < max;
		}

		private static float NormalizeAngle(float angle)
		{
			angle %= 360f;
			if (angle < 0f)
				angle += 360f;
			return angle;
		}
	}
}
