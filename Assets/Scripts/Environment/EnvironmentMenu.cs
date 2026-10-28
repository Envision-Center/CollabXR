using CollabXR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Environments
{
	public class EnvironmentMenu : SingletonBehavior<EnvironmentMenu>
	{
		[Header("UI references")]
		[SerializeField]
		private Button pageBackButton;
		[SerializeField]
		private Button pageForwardButton;
		[SerializeField]
		private TMP_Text pageCountText;
		[SerializeField]
		private TMP_Text teleportName;
		[SerializeField]
		private CategoryButtonList categoryButtonList;
		[SerializeField]
		private Transform objectButtonParent;
		[SerializeField]
		private Transform extensionsParent;

		private ObjectButton[] objectButtons;
		private Sprite modThumbnail;
		private int pageIndex;
		private int selectedTeleportIndex;
		private bool started;

		public EnvironmentEntry selectedEnvironment { get; private set; }

		public string selectedTeleport
		{
			get
			{
				return selectedEnvironment.teleports[selectedTeleportIndex];
			}
		}

		public Transform ExtensionsParent
		{
			get => extensionsParent;
			set => extensionsParent = value;
		}

		protected override void Awake()
		{
			base.Awake();
			objectButtons = objectButtonParent.GetComponentsInChildren<ObjectButton>();
			for (int i = 0; i < objectButtons.Length; i++)
			{
				int captured = i;
				objectButtons[i].OnClick.AddListener(() =>
				{
					SetSelectedTeleport(captured);
				});
			}
		}

		private void Start()
		{
			started = true;
			EnvironmentManager.Instance.OnEnvironmentLibraryChanged += RefreshLibrary;
			RefreshLibrary();
		}

		private void OnEnable()
		{
			if (started && selectedEnvironment != null && selectedEnvironment.teleports.Count > selectedTeleportIndex)
			{
				teleportName.text = selectedTeleport;
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (EnvironmentManager.Instance != null)
			{
				EnvironmentManager.Instance.OnEnvironmentLibraryChanged -= RefreshLibrary;
			}
			ClearModThumbnail();
		}

		public void Teleport()
		{
			if (selectedEnvironment != null && selectedEnvironment.availableOnThisPlatform && selectedEnvironment.teleports.Count > 0)
			{
				EnvironmentManager.Instance.RequestRoomEnvironmentChange(selectedEnvironment, selectedTeleportIndex);
			}
		}

		private void RefreshLibrary()
		{
			ClearModThumbnail();
			selectedEnvironment = null;
			if (EnvironmentManager.Instance.EnvironmentCategories.Count > 0)
			{
				ShowCategories();
				SetSelectedEnvironment(EnvironmentManager.Instance.EnvironmentCategories[0].entries[0]);
			}
			else
			{
				categoryButtonList.ResetCategoryButtons();
				teleportName.text = "No environments";
				SetPage(0);
			}
		}

		private void ShowCategories()
		{
			categoryButtonList.ResetCategoryButtons();
			var categories = EnvironmentManager.Instance.EnvironmentCategories;
			for (int i = 0; i < categories.Count; i++)
			{
				EnvironmentCategory category = categories[i];
				categoryButtonList.InstantiateCategoryButton(category.name, () =>
				{
					ShowEnvironments(category);
				});
			}
		}

		private void ShowEnvironments(EnvironmentCategory category)
		{
			categoryButtonList.ResetCategoryButtons();
			categoryButtonList.InstantiateCategoryButton("← Categories", ShowCategories);
			foreach (EnvironmentEntry entry in category.entries)
			{
				string label = entry.formattedSceneName + (entry.availableOnThisPlatform ? "" : " (Unavailable)");
				categoryButtonList.InstantiateCategoryButton(label, () =>
				{
					SetSelectedEnvironment(entry);
				});
			}
			if (category.entries.Count > 0)
			{
				categoryButtonList.Select(1);
			}
		}

		private void SetSelectedEnvironment(EnvironmentEntry entry)
		{
			ClearModThumbnail();
			selectedEnvironment = entry;
			if (selectedEnvironment.thumbnail != null && selectedEnvironment.teleportThumbnails.Count == 0)
			{
				Texture2D texture = selectedEnvironment.thumbnail;
				modThumbnail = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			}
			selectedTeleportIndex = 0;
			SetPage(0);
			if (selectedEnvironment.teleports.Count > 0)
			{
				SetSelectedTeleport(0);
			}
			else
			{
				teleportName.text = "No teleports";
			}
		}

		private void ClearModThumbnail()
		{
			if (modThumbnail != null)
			{
				Destroy(modThumbnail);
			}
			modThumbnail = null;
		}

		private void SetSelectedTeleport(int buttonIndex)
		{
			int index = buttonIndex + pageIndex * objectButtons.Length;
			if (selectedEnvironment == null || index < 0 || index >= selectedEnvironment.teleports.Count)
			{
				return;
			}
			selectedTeleportIndex = index;
			teleportName.text = selectedTeleport;
		}

		private void SetButtons()
		{
			for (int i = 0; i < objectButtons.Length; i++)
			{
				int index = i + pageIndex * objectButtons.Length;
				bool exists = selectedEnvironment?.teleports != null && index < selectedEnvironment.teleports.Count;
				objectButtons[i].SetShown(exists);
				if (!exists)
				{
					continue;
				}

				Sprite thumbnail = index < selectedEnvironment.teleportThumbnails.Count
					? selectedEnvironment.teleportThumbnails[index] : null;
				if (thumbnail == null)
				{
					thumbnail = modThumbnail;
				}
				objectButtons[i].SetThumbnail(thumbnail);
			}
		}

		public void TurnPage(int delta) => SetPage(pageIndex + delta);

		public void SetPage(int index)
		{
			int count = selectedEnvironment?.teleports?.Count ?? 0;
			int maxPageIndex = objectButtons.Length > 0 ? Mathf.Max(0, Mathf.CeilToInt(count / (float)objectButtons.Length) - 1) : 0;
			pageIndex = Mathf.Clamp(index, 0, maxPageIndex);
			pageCountText.SetText("Page " + (pageIndex + 1) + "/" + (maxPageIndex + 1));
			pageBackButton.interactable = pageIndex != 0;
			pageForwardButton.interactable = pageIndex != maxPageIndex;
			SetButtons();
		}
	}
}
