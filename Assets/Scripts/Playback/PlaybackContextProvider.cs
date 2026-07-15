using System;
using CollabXR.Cycles;
using CollabXR.Objects;
using CollabXR.UI;
using UnityEngine;

namespace CollabXR
{
	/**
	 * <summary>
	 * A monobehaviour that should be added alongside NetworkPlaybackDirector on the root of CollabObjects to properly spawn the ContextMenu
	 * </summary>
	 */
	public class PlaybackContextProvider : MonoBehaviour, IContextProvider
	{
		[SerializeField]
		private GameObject contextPrefab;

		public string TabName => "Playback";

		public CollabContext AddTab(GameObject obj, CollabContextMenu menu)
		{
			if (TryGetComponent(out NetworkPlaybackDirector director))
			{
				director.RegisterMenu(menu);
			}

			// If a custom prefab is assigned, instantiate it and find the CollabContext
			if (contextPrefab != null)
			{
				var prefabInstance = Instantiate(contextPrefab, obj.transform);
				prefabInstance.transform.localPosition = Vector3.zero;
				prefabInstance.transform.localRotation = Quaternion.identity;
				prefabInstance.transform.localScale = Vector3.one;

				var context = prefabInstance.GetComponent<PlaybackContext>();
				if (context == null)
				{
					Debug.LogError($"[PlaybackContextProvider] Custom prefab doesn't have PlaybackContext component!");
					Destroy(prefabInstance);
					return null;
				}

				return context;
			}

			return null;
		}

		public GameObject GetContextPrefab() => contextPrefab;

		public GameObject SelfObject => gameObject;
	}
}
