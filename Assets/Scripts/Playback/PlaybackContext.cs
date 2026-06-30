using System.Collections.Generic;
using CollabXR.Cycles;
using CollabXR.ModExtras;
using CollabXR.Objects;
using CollabXR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CollabXR
{
	/**
	 * <summary>
	 * Context menu tab for PlaybackDirector control. Integrates with ContextMenu system.
	 * </summary>
	 */
	public class PlaybackContext : CollabContext
	{
		[Header("Settings")]
		[SerializeField]
		private float maxSpeed = 5;

		[Header("UI Prefabs")]
		[SerializeField]
		private GameObject componentButtonPrefab;

		[SerializeField]
		private GameObject setItemPrefab;

		[SerializeField]
		private Button playButton;

		[SerializeField]
		private Button pauseButton;

		[SerializeField]
		private Button stopButton;

		[SerializeField]
		private Slider seekSlider;

		[SerializeField]
		private Slider speedSlider;

		[SerializeField]
		private TMP_Text speedLabel;

		[SerializeField]
		private TMP_Text timeLabel;

		[SerializeField]
		private TMP_Text frameLabel;

		[SerializeField]
		private Transform componentSelectorContainer;

		[SerializeField]
		private Transform dropdownContainer;

		private PlaybackViewModel _viewModel;
		private Dictionary<int, Button> _componentButtons = new();
		private Dictionary<int, Transform> _dropdownMenus = new();
		private bool _isScrubbingSeek,
			_isScrubbingSpeed;
		private bool _wasPlayingBeforeSeekScrub;

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);

			_viewModel = dataObj.GetComponentInChildren<PlaybackViewModel>();

			if (_viewModel == null)
			{
				Debug.LogWarning("[PlaybackContext] No PlaybackViewModel found!");
				return;
			}

			// If prefabs aren't assigned in inspector, error
			if (playButton == null || seekSlider == null)
			{
				Debug.LogWarning("[PlaybackContext] No seekSlider found!");
			}

			InitializeUI();
			SubscribeToViewModel();
		}

		/// <summary>
		/// Creates a simple button with text.
		/// </summary>
		private Button CreateButton(string label, Transform parent)
		{
			var btnGo = new GameObject(label + "Button");
			btnGo.transform.SetParent(parent, false);

			var rectTransform = btnGo.AddComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;

			var image = btnGo.AddComponent<Image>();
			image.color = new Color(0.2f, 0.2f, 0.2f);

			var button = btnGo.AddComponent<Button>();
			button.targetGraphic = image;

			var textGo = new GameObject("Text");
			textGo.transform.SetParent(btnGo.transform, false);
			var textRect = textGo.AddComponent<RectTransform>();
			textRect.anchorMin = Vector2.zero;
			textRect.anchorMax = Vector2.one;

			var text = textGo.AddComponent<TextMeshProUGUI>();
			text.text = label;
			text.alignment = TextAlignmentOptions.Center;

			var layout = btnGo.AddComponent<LayoutElement>();
			layout.preferredHeight = 40f;

			return button;
		}

		private void InitializeUI()
		{
			// Wire up button listeners
			if (playButton != null)
				playButton.onClick.AddListener(() =>
				{
					if (!dataObj.HasStateAuthority)
						return;
					_viewModel.RequestPlay();
				});

			if (pauseButton != null)
				pauseButton.onClick.AddListener(() =>
				{
					if (!dataObj.HasStateAuthority)
						return;
					_viewModel.RequestPause();
				});

			if (stopButton != null)
				stopButton.onClick.AddListener(() =>
				{
					if (!dataObj.HasStateAuthority)
						return;
					_viewModel.RequestStop();
				});

			// Wire up slider listeners
			if (seekSlider != null)
			{
				seekSlider.onValueChanged.AddListener(OnSeekSliderChanged);

				var trigger = seekSlider.gameObject.GetComponent<EventTrigger>()
				              ?? seekSlider.gameObject.AddComponent<EventTrigger>();

				var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
				pointerDown.callback.AddListener(_ => OnSeekSliderPointerDown());
				trigger.triggers.Add(pointerDown);

				var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
				pointerUp.callback.AddListener(_ => OnSeekSliderPointerUp());
				trigger.triggers.Add(pointerUp);
			}

			if (speedSlider != null)
			{
				speedSlider.minValue = 0f;
				speedSlider.maxValue = 1f;
				speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
			}

			// Subscribe to ViewModel state changes
			_viewModel.Percent.AddListener(OnPercentChanged);
			_viewModel.IsPlaying.AddListener(OnPlayingStateChanged);
			_viewModel.CountByComponent.AddListener(OnCountByComponentChanged);
			_viewModel.ActiveSetByComponent.AddListener(OnActiveSetByComponentChanged);
			_viewModel.Duration.AddListener(OnDurationChanged);
			_viewModel.MaxFrames.AddListener(OnMaxFramesChanged);
			_viewModel.Speed.AddListener(OnSpeedChanged);
		}

		private void SubscribeToViewModel()
		{
			// Initial state update
			OnPercentChanged(_viewModel.Percent.Value);
			OnPlayingStateChanged(_viewModel.IsPlaying.Value);
			OnCountByComponentChanged(_viewModel.CountByComponent.Value);
			OnActiveSetByComponentChanged(_viewModel.ActiveSetByComponent.Value);
			OnDurationChanged(_viewModel.Duration.Value);
			OnMaxFramesChanged(_viewModel.MaxFrames.Value);
			OnSpeedChanged(_viewModel.Speed.Value);
		}

		#region ViewModel Callbacks

		private void OnPercentChanged(float percent)
		{
			if (seekSlider != null && !_isScrubbingSeek)
			{
				seekSlider.value = percent;
			}
			UpdateTimeLabel(percent);
			UpdateFrameLabel(percent);
		}

		private void OnSpeedChanged(float speedValue)
		{
			// Convert the speed value (multiplier) to percentage for slider display
			float speedPercent = GetSpeedPercent(speedValue);

			if (speedSlider != null && !_isScrubbingSpeed)
			{
				speedSlider.value = speedPercent;
			}

			UpdateSpeedLabel(speedPercent);

			// Clear scrubbing flag after network update is processed
			_isScrubbingSpeed = false;
		}

		private float GetSpeedValue(float speedPercent)
		{
			return speedPercent * maxSpeed;
		}

		private float GetSpeedPercent(float speedValue)
		{
			return speedValue / maxSpeed;
		}

		private void OnPlayingStateChanged(bool isPlaying)
		{
			if (playButton != null)
				playButton.interactable = !isPlaying;
			if (pauseButton != null)
				pauseButton.interactable = isPlaying;
		}

		private void OnCountByComponentChanged(Dictionary<int, int> countByComponent)
		{
			// Clear existing UI
			foreach (var btn in _componentButtons.Values)
				Destroy(btn.gameObject);
			_componentButtons.Clear();

			foreach (var menu in _dropdownMenus.Values)
				Destroy(menu.gameObject);
			_dropdownMenus.Clear();

			if (componentSelectorContainer == null)
				return;

			// Create component selector buttons
			foreach (var kvp in countByComponent)
			{
				int componentId = kvp.Key;
				int setCount = kvp.Value;
				var component = _viewModel.components[componentId];

				// Create component button (use prefab if available, otherwise create default)
				Button btn;
				if (componentButtonPrefab != null)
				{
					var btnGo = Instantiate(componentButtonPrefab, componentSelectorContainer);
					btn = btnGo.GetComponent<Button>();
					var text = btnGo.GetComponentInChildren<TMP_Text>();
					if (text != null)
						text.text = component.DisplayName;
				}
				else
				{
					btn = CreateButton(component.DisplayName, componentSelectorContainer);
				}

				int capId = componentId; // Capture for closure
				btn.onClick.AddListener(() => SelectComponent(capId, setCount));
				_componentButtons[componentId] = btn;
			}
		}

		private void OnActiveSetByComponentChanged(Dictionary<int, int> activeSetByComponent)
		{
			// Update visual state based on active selections
			// This is used to refresh the dropdown if needed
		}

		private void OnDurationChanged(float duration)
		{
			UpdateTimeLabel(_viewModel.Percent.Value);
		}

		private void OnMaxFramesChanged(int frames)
		{
			UpdateFrameLabel(_viewModel.Percent.Value);
		}

		#endregion

		#region UI Event Handlers

		private void OnSeekSliderPointerDown()
		{
			if (!dataObj.HasStateAuthority) return;
			_wasPlayingBeforeSeekScrub = _viewModel.IsPlaying.Value;
			_viewModel.RequestSetScrubbing(true);
		}

		private void OnSeekSliderPointerUp()
		{
			if (!dataObj.HasStateAuthority) return;
			_viewModel.RequestSetScrubbing(false);
			_wasPlayingBeforeSeekScrub = false;
		}

		private void OnSeekSliderChanged(float value)
		{
			if (!dataObj.HasStateAuthority)
				return;

			_isScrubbingSeek = true;
			OnPercentChanged(value);
			_viewModel.RequestSeek(value);
			_isScrubbingSeek = false;
		}

		private void OnSpeedSliderChanged(float percent)
		{
			if (!dataObj.HasStateAuthority)
				return;

			_isScrubbingSpeed = true;

			// Convert percentage to speed value (multiplier)
			float speedValue = GetSpeedValue(percent);
			_viewModel.RequestSetSpeed(speedValue);

			// Update UI immediately while dragging
			UpdateSpeedLabel(percent);

			// Flag will be cleared when viewmodel listener responds
			// This prevents the slider from jumping during user interaction
		}

		private void SelectComponent(int componentId, int setCount)
		{
			if (!dataObj.HasStateAuthority)
				return;

			RebuildDropdown(componentId, setCount);
		}

		private void RebuildDropdown(int componentId, int setCount)
		{
			// Clear existing dropdown for this component
			if (_dropdownMenus.TryGetValue(componentId, out var existingMenu))
				Destroy(existingMenu.gameObject);

			if (dropdownContainer == null)
				return;

			// Create dropdown menu container
			var dropdownGo = new GameObject($"Dropdown_Component{componentId}");
			var dropdownTransform = dropdownGo.AddComponent<RectTransform>();
			dropdownTransform.SetParent(dropdownContainer, false);
			dropdownTransform.localPosition = Vector3.zero;

			var layoutGroup = dropdownGo.AddComponent<VerticalLayoutGroup>();
			layoutGroup.childForceExpandHeight = false;
			layoutGroup.childForceExpandWidth = true;
			layoutGroup.spacing = 5f;
			layoutGroup.padding = new RectOffset(5, 5, 5, 5);

			_dropdownMenus[componentId] = dropdownTransform;

			var component = _viewModel.components[componentId];

			// Create dropdown items for each animation set
			for (int i = 0; i < setCount; i++)
			{
				Button btn;
				if (setItemPrefab != null)
				{
					var itemGo = Instantiate(setItemPrefab, dropdownTransform);
					btn = itemGo.GetComponent<Button>();
					var text = itemGo.GetComponentInChildren<TMP_Text>();
					if (text != null)
						text.text = component.GetSetDisplayName(i);
				}
				else
				{
					btn = CreateButton(component.GetSetDisplayName(i), dropdownTransform);
				}

				int capIndex = i; // Capture for closure
				btn.onClick.AddListener(() =>
				{
					_viewModel.RequestSetComponentSet(componentId, capIndex);
				});
			}
		}

		private void UpdateTimeLabel(float percent)
		{
			if (timeLabel == null)
				return;

			float currentTime = percent * _viewModel.Duration.Value;
			timeLabel.text = $"{currentTime:F2}s / {_viewModel.Duration.Value:F2}s";
		}

		private void UpdateFrameLabel(float percent)
		{
			if (frameLabel == null)
				return;

			int currentFrame = Mathf.FloorToInt(percent * _viewModel.MaxFrames.Value);
			frameLabel.text = $"Frame {currentFrame} / {_viewModel.MaxFrames.Value}";
		}

		private void UpdateSpeedLabel(float percent)
		{
			if (speedLabel == null)
				return;

			speedLabel.text = $"{GetSpeedValue(percent):F1}x";
		}

		#endregion

		protected override void Update()
		{
			base.Update();
		}

		private void OnDestroy()
		{
			if (_viewModel == null)
				return;

			// Unsubscribe from ViewModel
			_viewModel.Percent.RemoveListener(OnPercentChanged);
			_viewModel.IsPlaying.RemoveListener(OnPlayingStateChanged);
			_viewModel.CountByComponent.RemoveListener(OnCountByComponentChanged);
			_viewModel.ActiveSetByComponent.RemoveListener(OnActiveSetByComponentChanged);
			_viewModel.Duration.RemoveListener(OnDurationChanged);
			_viewModel.MaxFrames.RemoveListener(OnMaxFramesChanged);
			_viewModel.Speed.RemoveListener(OnSpeedChanged);

			// Clean up UI listeners
			if (seekSlider != null)
				seekSlider.onValueChanged.RemoveListener(OnSeekSliderChanged);
		}

		public override void OnStateAuthorityChanged()
		{
			if (dataObj != null && dataObj.HasStateAuthority)
			{
				Register();
			}
			else
			{
				Deregister();
			}
		}

		private void Deregister()
		{
			if (_viewModel == null)
				return;

			_viewModel.Percent.RemoveListener(OnPercentChanged);
			_viewModel.IsPlaying.RemoveListener(OnPlayingStateChanged);
			_viewModel.CountByComponent.RemoveListener(OnCountByComponentChanged);
			_viewModel.ActiveSetByComponent.RemoveListener(OnActiveSetByComponentChanged);
			_viewModel.Duration.RemoveListener(OnDurationChanged);
			_viewModel.MaxFrames.RemoveListener(OnMaxFramesChanged);
			_viewModel.Speed.RemoveListener(OnSpeedChanged);

			_isScrubbingSeek = false;
			_isScrubbingSpeed = false;
			_wasPlayingBeforeSeekScrub = false;

			if (playButton != null)
				playButton.interactable = false;
			if (pauseButton != null)
				pauseButton.interactable = false;
			if (stopButton != null)
				stopButton.interactable = false;
			if (seekSlider != null)
				seekSlider.interactable = false;
			if (speedSlider != null)
				speedSlider.interactable = false;
		}

		private void Register()
		{
			if (_viewModel == null)
				return;

			// Remove before readding to prevent double subscription across authority swaps
			_viewModel.Percent.RemoveListener(OnPercentChanged);
			_viewModel.IsPlaying.RemoveListener(OnPlayingStateChanged);
			_viewModel.CountByComponent.RemoveListener(OnCountByComponentChanged);
			_viewModel.ActiveSetByComponent.RemoveListener(OnActiveSetByComponentChanged);
			_viewModel.Duration.RemoveListener(OnDurationChanged);
			_viewModel.MaxFrames.RemoveListener(OnMaxFramesChanged);
			_viewModel.Speed.RemoveListener(OnSpeedChanged);

			_viewModel.Percent.AddListener(OnPercentChanged);
			_viewModel.IsPlaying.AddListener(OnPlayingStateChanged);
			_viewModel.CountByComponent.AddListener(OnCountByComponentChanged);
			_viewModel.ActiveSetByComponent.AddListener(OnActiveSetByComponentChanged);
			_viewModel.Duration.AddListener(OnDurationChanged);
			_viewModel.MaxFrames.AddListener(OnMaxFramesChanged);
			_viewModel.Speed.AddListener(OnSpeedChanged);

			_isScrubbingSeek = false;
			_isScrubbingSpeed = false;
			_wasPlayingBeforeSeekScrub = false;

			if (playButton != null)
				playButton.interactable = true;
			if (pauseButton != null)
				pauseButton.interactable = true;
			if (stopButton != null)
				stopButton.interactable = true;
			if (seekSlider != null)
				seekSlider.interactable = true;
			if (speedSlider != null)
				speedSlider.interactable = true;

			// explicitly refresh so sliders reflect current ViewModel state when becoming authority
			OnSpeedChanged(_viewModel.Speed.Value);
			OnPlayingStateChanged(_viewModel.IsPlaying.Value);
		}
	}
}
