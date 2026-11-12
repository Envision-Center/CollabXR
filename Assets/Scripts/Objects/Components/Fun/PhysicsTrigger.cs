using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Components.Fun
{
	/// <summary>
	/// Listens for physics intersections with the given trigger.
	/// </summary>
	public class PhysicsTrigger : MonoBehaviour
	{
		[Tooltip("Emitted when a CollabObject enters the trigger.")]
		public UnityEvent<CollabObject> objectEntered;

		private void Awake()
		{
			if (objectEntered == null)
			{
				objectEntered = new UnityEvent<CollabObject>();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			CollabObject obj = SearchForCollabObject(other.transform);
			if (obj != null)
			{
				objectEntered.Invoke(obj);
			}
		}

		private CollabObject SearchForCollabObject(Transform item)
		{
			// See if we have a CollabObject here
			CollabObject obj;
			if (item.TryGetComponent(out obj))
			{
				return obj;
			}

			// If at end of hiearchy, return nothing
			if (item.parent == null)
			{
				return null;
			}

			// Otherwise, keep searching
			return SearchForCollabObject(item.parent);
		}
	}
}
