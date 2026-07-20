using CollabXR.Editor;
using UnityEngine;

namespace CollabXR.Tools.Palette
{
	[HideSiblingsOfSelectedChild]
	[DefaultExecutionOrder(99999999)]
	public class SingleChildActivator : MonoBehaviour
	{
		[SerializeField]
		private int initialActiveChildIndex = 0;

		[SerializeField]
		private bool shouldSetInitialActiveChild = true;

		// Keeps track of the currently active child
		private int currentActiveIndex = -1;

		private void Awake()
		{
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				GameObject g = transform.GetChild(i).gameObject;
				g.SetActive(true);
			}
		}

		private async void Start()
		{
			// await Awaitable.EndOfFrameAsync();
			int childCount = transform.childCount;
			for (int i = 0; i < childCount; i++)
			{
				GameObject g = transform.GetChild(i).gameObject;

				if (!g.TryGetComponent(out DeactivateSiblingsOnEnable _))
				{
					g.AddComponent<DeactivateSiblingsOnEnable>();
				}
			}

			if (shouldSetInitialActiveChild)
				SetActiveChild(initialActiveChildIndex);
		}

		public void DeactivateAllChildren()
		{
			currentActiveIndex = -1;
			for (int i = 0; i < transform.childCount; i++)
				transform.GetChild(i).gameObject.SetActive(false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public (Transform, Transform) SetActiveChild(int index)
		{
			Transform newActive = transform.GetChild(index);
			Transform prevActive = null;
			if (currentActiveIndex > 0)
			{
				prevActive = transform.GetChild(currentActiveIndex);
			}

			currentActiveIndex = index;
			newActive.gameObject.SetActive(true);

			return (prevActive, newActive);
		}
	}
}
