using CollabXR.Tools.Palette;
using UnityEngine;

namespace CollabXR.UI
{
	public class CategoryView : MonoBehaviour
	{
		[SerializeField]
		private SingleChildActivator selector;

		[SerializeField]
		private CategoryButtonList categoryButtonList;

		private void Awake()
		{
			if (selector != null)
			{
				RegisterNewSingleChildActivator(selector);
			}
		}

		public void RegisterNewSingleChildActivator(SingleChildActivator newSelector)
		{
			categoryButtonList.ResetCategoryButtons();

			selector = newSelector;

			for (int i = 0; i < selector.transform.childCount; i++)
			{
				int iCaptured = i;

				categoryButtonList.InstantiateCategoryButton(
					selector.transform.GetChild(i).gameObject.name,
					delegate
					{
						selector.SetActiveChild(iCaptured);
					}
				);
			}

			categoryButtonList.Select(0);
		}

		public void RefreshButtons()
		{
			if (selector != null)
			{
				RegisterNewSingleChildActivator(selector);
			}
		}
	}
}
