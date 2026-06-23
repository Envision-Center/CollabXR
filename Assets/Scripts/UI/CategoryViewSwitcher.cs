using System.Collections.Generic;
using CollabXR.Tools.Palette;
using UnityEditor;
using UnityEngine;

namespace CollabXR.UI
{
	public class CategoryViewSwitcher : MonoBehaviour
	{
		[SerializeField]
		private CategoryView categoryView;

		[SerializeField]
		private List<SingleChildActivator> singleChildActivators = new();

		private void Awake()
		{
			if (singleChildActivators.Count > 0)
			{
				SetActiveSingleChildActivator(0);
			}
		}

		public void SetActiveSingleChildActivator(int i)
		{
			foreach (SingleChildActivator activator in singleChildActivators)
			{
				activator.gameObject.SetActive(false);
			}

			singleChildActivators[i].gameObject.SetActive(true);
			categoryView.RegisterNewSingleChildActivator(singleChildActivators[i]);
		}
	}

#if UNITY_EDITOR

	[InitializeOnLoad]
	static class CategoryViewSwitcherEditorHelper
	{
		static CategoryViewSwitcherEditorHelper()
		{
			Selection.selectionChanged -= OnEditorSelectionChange;
			Selection.selectionChanged += OnEditorSelectionChange;
		}

		private static void OnEditorSelectionChange()
		{
			GameObject selected = Selection.activeGameObject;
			Transform parent = selected?.transform.parent;

			if (parent?.GetComponent<CategoryViewSwitcher>() == null)
				return;

			if (selected?.GetComponent<SingleChildActivator>() == null)
				return;

			for (int i = 0; i < parent.childCount; i++)
			{
				GameObject g = parent.GetChild(i).gameObject;

				if (g.GetComponent<SingleChildActivator>() == null)
					continue;

				g.SetActive(g == selected);
			}

			if (selected != null && selected.transform.parent == parent)
			{
				for (int i = 0; i < parent.childCount; i++)
				{
					GameObject g = parent.GetChild(i).gameObject;

					if (g.GetComponent<SingleChildActivator>() == null)
						continue;

					g.SetActive(g == selected);
				}
			}
		}
	}

#endif
}
