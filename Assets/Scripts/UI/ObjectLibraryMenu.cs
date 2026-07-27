using System;
using CollabXR.Objects;
using CollabXR.Tools;
using CollabXR.ModLoader;
using CollabXR.ModPackager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class ObjectLibraryMenu : MonoBehaviour
	{
		//[Header("Prefabs and references")] [SerializeField]
		//private ObjectLibrary library;

		[Header("UI references")]
		[SerializeField]
		private Button pageBackButton;

		[SerializeField]
		private Button pageForwardButton;

		[SerializeField]
		private TMP_Text pageCountText;

		[SerializeField]
		private TMP_Text categoryTitleLabel;

		[SerializeField]
		private Image selectedObjectSpawnToolThumbnail;

		[SerializeField]
		private Transform objectButtonParent;

		[SerializeField]
		private CategoryButtonList categoryButtonList;

		[SerializeField]
		private Sprite defaultThumbnailSprite;

		[SerializeField]
		private Sprite invalidSpriteOverlay;

		[SerializeField]
		private ObjectLibraryInfoPanel infoPanel;

		private int pageIndex;
		private ObjectButton[] objectButtons;
		private ObjectCategory activeCategory;
		private CollabObjectData activeObjectData;

		private GameObject gameMenuObj;

		private void Awake()
		{
			objectButtons = objectButtonParent.GetComponentsInChildren<ObjectButton>();
			gameMenuObj = GetComponentInParent<GameMenu>().gameObject;
		}

		private void Start()
		{
			activeCategory = MainLibraryRef.Instance.library.categories[0];
			SetPage(0);
			UpdateObjectButtons();

			InstantiateCategoryButtons();

			MainLibraryRef.Instance.onNewCategoryCreation.AddListener(InstantiateCategoryButtons);

			// setup button events
			for (int i = 0; i < objectButtons.Length; i++)
			{
				int iCaptured = i;

				objectButtons[i]
					.OnClick.AddListener(
						delegate
						{
							OnObjectButtonPress(iCaptured);
						}
					);
				objectButtons[i]
					.OnHoverEnter.AddListener(
						delegate
						{
							OnObjectButtonHover(iCaptured, true);
						}
					);
				objectButtons[i]
					.OnHoverExit.AddListener(
						delegate
						{
							OnObjectButtonHover(iCaptured, false);
						}
					);
				objectButtons[i].HandedButton.onClickIsRight.AddListener(OnObjectButtonPressHanded);
			}

			// setup page controls
			pageBackButton?.onClick.AddListener(
				delegate
				{
					TurnPage(-1);
				}
			);

			pageForwardButton?.onClick.AddListener(
				delegate
				{
					TurnPage(1);
				}
			);
		}

		private void OnObjectButtonPress(int buttonIndex)
		{
			int dataIndex = pageIndex * objectButtons.Length + buttonIndex;

			SetSelectedObjectData(activeCategory, dataIndex);

			if (gameMenuObj)
				gameMenuObj.SetActive(false);
		}

		private void OnObjectButtonPressHanded(bool isRight)
		{
			ToolPalette.Get(isRight).GetComponentInChildren<ObjectSpawner>(true).gameObject.SetActive(true);
		}

		private void OnObjectButtonHover(int buttonIndex, bool entered)
		{
			int dataIndex = pageIndex * objectButtons.Length + buttonIndex;
			bool withinDatasetBounds = dataIndex < activeCategory.objectData.Count;

			if (!withinDatasetBounds)
			{
				Debug.LogError("Attempting to hover over button out of data bounds at index " + dataIndex + " when size is " + activeCategory.objectData.Count);
				return;
			}

			if (entered)
			{
				CollabObjectData objData = activeCategory.objectData[dataIndex];
				
				if (!ModManager.Instance.indexedMods.ContainsKey(objData.modGUID))
				{
					Debug.LogError($"Mod with GUID {objData.modGUID} not found in indexed mods. Cannot display info panel.");
					return;
				}
				Tuple<ModMetadata, string> targetModRecord = ModManager.Instance.indexedMods[objData.modGUID];
				ModMetadata metadata = targetModRecord.Item1;
				string repoName = RepositoryManager.Instance.loadedRepositories[targetModRecord.Item2].RepoName;

				infoPanel.SetInfoPanel(
					assetName: objData.assetName,
					attribution: objData.attribution,
					thumbnail: objData.thumbnail,
					creator: objData.creators,
					version: metadata.BuildNumberMap[ModManager.GetPlatformString()].ToString() + ".0"
				);
				infoPanel.ToggleVisibility(true);
			}
			else
			{
				infoPanel.SetInfoPanel();
				infoPanel.ToggleVisibility(false);
			}
		}

		private void InstantiateCategoryButtons()
		{
			categoryButtonList.ResetCategoryButtons();
			for (int i = 0; i < MainLibraryRef.Instance.library.categories.Count; i++)
			{
				int iCaptured = i;

				categoryButtonList.InstantiateCategoryButton(
					MainLibraryRef.Instance.library.categories[i].name,
					delegate
					{
						SetActiveCategoryAndUpdateObjectButtons(iCaptured);
					}
				);
			}

			categoryButtonList.Select(0);
		}

		private void SetActiveCategoryAndUpdateObjectButtons(int index)
		{
			activeCategory = MainLibraryRef.Instance.library.categories[index];
			categoryTitleLabel.text = activeCategory.name;
			SetPage(0);
			UpdateObjectButtons();
		}

		private void SetSelectedObjectData(ObjectCategory category, int index)
		{
			activeObjectData = category.objectData[index];
			selectedObjectSpawnToolThumbnail.sprite = activeObjectData.thumbnail;

			ObjectSpawner[] spawners = FindObjectsOfType<ObjectSpawner>(true);

			foreach (ObjectSpawner spawner in spawners)
			{
				spawner.DataToSpawn.Set(activeObjectData);
			}
		}

		private void UpdateObjectButtons()
		{
			int pageFirstObjectIndex = pageIndex * objectButtons.Length;

			for (int buttonIndex = 0; buttonIndex < objectButtons.Length; buttonIndex++)
			{
				int dataIndex = buttonIndex + pageFirstObjectIndex;

				bool withinDatasetBounds = dataIndex < activeCategory.objectData.Count;

				objectButtons[buttonIndex].SetShown(withinDatasetBounds);

				if (withinDatasetBounds)
				{
					Sprite thumbnail = activeCategory.objectData[dataIndex].thumbnail;

					if (thumbnail == null)
						thumbnail = defaultThumbnailSprite;

					objectButtons[buttonIndex].SetThumbnail(thumbnail);
					objectButtons[buttonIndex].SetOverlay(!activeCategory.objectData[dataIndex].availableOnThisPlatform, invalidSpriteOverlay, Color.red);
				}
				else
				{
					objectButtons[buttonIndex].ClearOverlay();
				}
			}

			OnDisable(); // failsafe for disabling info panel on page turn
		}

		public void TurnPage(int delta) => SetPage(pageIndex + delta);

		public void SetPage(int index)
		{
			pageIndex = index;

			int maxPageIndex = Mathf.CeilToInt(activeCategory.objectData.Count / (float)objectButtons.Length - 1);

			pageIndex = activeCategory.objectData.Count > 0 ? Math.Clamp(pageIndex, 0, maxPageIndex) : 0;

			UpdateObjectButtons();

			pageCountText.SetText("Page " + (pageIndex + 1) + "/" + (maxPageIndex + 1));
			pageBackButton.interactable = pageIndex != 0;
			pageForwardButton.interactable = pageIndex != maxPageIndex;
		}

		private void OnDisable()
		{
			infoPanel.SetInfoPanel();
			infoPanel.ToggleVisibility(false); // reset info panel on disable
		}

		/*
		 * Helpful code contribution from my cat:
		 *
		 * |}PO;lok                                                         pp\
		 * ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;ppppppppi  iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii
		 * iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii
		 * iiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiiii
		 *
		 * petition to put your cat on the payroll
		 */
	}
}
