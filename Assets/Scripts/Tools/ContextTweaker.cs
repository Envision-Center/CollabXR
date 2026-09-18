using CollabXR.Objects;
using CollabXR.UI;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Tools
{
	public class ContextTweaker : MonoBehaviour
	{
		public EventVariable<CollabObject> targetObject = new();
		public UnityEvent<bool> OnCanTweakTarget = new();

		private void Awake()
		{
			targetObject.AddListenerAndCheck(
				delegate(CollabObject c)
				{
					OnCanTweakTarget.Invoke(c != null);
				}
			);
		}

		public void SetTarget(GameObject g)
		{
			targetObject.Value = g?.GetComponentInParent<CollabObject>();
		}

		public void OpenTweakMenuForTargetObject() => OpenTweakMenuForObject(targetObject.Value);

		public void OpenTweakMenuForObject(CollabObject c)
		{
			if (c == null)
			{
				return;
			}

			GameMenu menu = FindFirstObjectByType<GameMenu>(FindObjectsInactive.Include);
			menu?.gameObject.SetActive(true);

			CollabContextMenu contextMenu = FindFirstObjectByType<CollabContextMenu>(FindObjectsInactive.Include);
			if (contextMenu != null)
			{
				contextMenu.gameObject.SetActive(true);
				contextMenu.OpenContext(c);
			}

			c.Object.RequestStateAuthority();
		}
	}
}
