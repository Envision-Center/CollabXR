using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CollabXR.UI
{
	[RequireComponent(typeof(HandedButton))]
	public class ObjectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField]
		private Image thumbnailView;

		[SerializeField]
		private Button button;

		[SerializeField]
		private Image overlay;

		public UnityEvent OnClick => button.onClick;
		public UnityEvent OnHoverEnter;
		public UnityEvent OnHoverExit;

		public RectTransform RectTransform { get; private set; }

		public HandedButton HandedButton { get; private set; }

		private void Awake()
		{
			RectTransform = button.GetComponent<RectTransform>();
			HandedButton = GetComponent<HandedButton>();
		}

		public void SetThumbnail(Sprite s)
		{
			thumbnailView.sprite = s;
		}

		public void SetOverlay(bool active, Sprite s, Color c)
		{
			overlay.gameObject.SetActive(active);
			overlay.sprite = s;
			overlay.color = c;
		}

		public void ClearOverlay()
		{
			SetOverlay(false, null, Color.white);
		}

		public void SetShown(bool shown)
		{
			thumbnailView.enabled = shown;
			button.interactable = shown;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			OnHoverEnter.Invoke();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			OnHoverExit.Invoke();
		}
	}
}
