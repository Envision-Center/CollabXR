using CollabXR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CollabXR.EnvironmentExtras;

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
		private int pageIndex;
		private int selectedIndex;
		private int selectedTeleportIndex;

		//public EnvironmentScene currentEnvironmentInstance => EnvironmentManager.Instance?.GetEnvironmentInstance();
		public EnvironmentData selectedEnvironment => EnvironmentManager.Instance?.GetEnvironmentAtIndex(selectedIndex);
		public EnvironmentTeleportInfo selectedTeleport => selectedEnvironment.teleportInfo[selectedTeleportIndex];

		public Transform ExtensionsParent
		{
			get => extensionsParent;
			set => extensionsParent = value;
		}

		protected override void Awake()
		{
			base.Awake();
			objectButtons = objectButtonParent.GetComponentsInChildren<ObjectButton>();

			// setup button events
			for (int i = 0; i < objectButtons.Length; i++)
			{
				int iCaptured = i;

				objectButtons[i]
					.OnClick.AddListener(
						delegate
						{
							SetSelectedTeleport(iCaptured);
						}
					);
			}
		}

		private bool started = false;

		private void Start()
		{
			started = true;
			InstantiateCategoryButtons();

			categoryButtonList.Select(selectedIndex);
			SetSelectedTeleport(selectedTeleportIndex);
		}

		private void OnEnable()
		{
			if (!started)
				return;

			categoryButtonList.Select(selectedIndex);
			SetSelectedTeleport(selectedTeleportIndex);
		}

		public void Teleport()
		{
			EnvironmentManager.Instance?.RequestRoomEnvironmentChange(selectedIndex, selectedTeleportIndex);
			//EnvironmentManager.Instance?.TeleportTo(selectedTeleportIndex);
		}

		private void InstantiateCategoryButtons()
		{
			for (int i = 0; i < EnvironmentManager.Instance?.environmentData.Length; i++)
			{
				int iCaptured = i;

				categoryButtonList.InstantiateCategoryButton(
					EnvironmentManager.Instance?.GetEnvironmentAtIndex(i).name,
					delegate
					{
						SetSelectedEnvironment(iCaptured);
					}
				);
			}
		}

		private void SetSelectedEnvironment(int index)
		{
			selectedIndex = index;
			SetPage(0);
			SetSelectedTeleport(0);
			SetButtons();
		}

		private void SetSelectedTeleport(int buttonIndex)
		{
			selectedTeleportIndex = buttonIndex;
			teleportName.text = selectedTeleport.name;
		}

		private void SetButtons()
		{
			for (int i = 0; i < objectButtons.Length; i++)
			{
				int teleportIndex = i + (pageIndex * objectButtons.Length);
				bool exists = selectedEnvironment.teleportInfo.Length > teleportIndex;
				objectButtons[i].SetShown(exists);
				if (exists)
				{
					objectButtons[i].SetThumbnail(selectedEnvironment.teleportInfo[teleportIndex].thumbnail);
				}
			}
		}

		public void TurnPage(int delta) => SetPage(pageIndex + delta);

		public void SetPage(int index)
		{
			pageIndex = index;

			int maxPageIndex = Mathf.CeilToInt(selectedEnvironment.teleportInfo.Length / (float)objectButtons.Length - 1);

			pageIndex = Mathf.Clamp(pageIndex, 0, maxPageIndex);

			pageCountText.SetText("Page " + (pageIndex + 1) + "/" + (maxPageIndex + 1));
			pageBackButton.interactable = pageIndex != 0;
			pageForwardButton.interactable = pageIndex != maxPageIndex;
			//pageBackButton.gameObject.SetActive(pageIndex != 0);
			//pageForwardButton.gameObject.SetActive(pageIndex < maxPageIndex);

			SetButtons();
		}
	}
}
