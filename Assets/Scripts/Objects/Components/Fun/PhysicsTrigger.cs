using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Components.Fun
{
	/// <summary>
	/// Listens for physics intersections with the given trigger.
	/// </summary>
	public class PhysicsTrigger : MonoBehaviour
	{
		[Tooltip("Emitted when a CollabObject enters the trigger. Also returns the attached Rigidbody, if found.")]
		public UnityEvent<CollabObject, Rigidbody> objectEntered;

		private void Awake()
		{
			if (objectEntered == null)
			{
				objectEntered = new UnityEvent<CollabObject, Rigidbody>();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			CollabObject obj = SearchForComponent<CollabObject>(other.transform);

			if (obj != null)
			{
				objectEntered.Invoke(obj, SearchForComponent<Rigidbody>(other.transform));
			}
		}

		private T SearchForComponent<T>(Transform item) where T : Component
		{
			// See if we have a CollabObject here
			T obj;
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
			return SearchForComponent<T>(item.parent);
		}
	}
}
