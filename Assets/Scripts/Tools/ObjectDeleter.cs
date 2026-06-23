using CollabXR.Networking;
using CollabXR.Objects;
using UnityEngine;
using UnityEngine.Events;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	public class CollabObjectDeleter : MonoBehaviour
	{
		public CollabObject objectToDestroy;

		public UnityEvent<bool> onCanDelete;

		public void SetObjectToDestroy(GameObject obj)
		{
			CollabObject previousObjToDestroy = objectToDestroy;
			objectToDestroy = obj?.GetComponentInParent<CollabObject>();

			if (previousObjToDestroy == null && objectToDestroy != null)
			{
				onCanDelete.Invoke(true);
			}
			else if (previousObjToDestroy != null && objectToDestroy == null)
			{
				onCanDelete.Invoke(false);
			}
		}

		private void Start()
		{
			onCanDelete.Invoke(false);
		}

		public void DestroyCollabObject()
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDelete)
				return;

			onCanDelete.Invoke(false);
			DestroyCollabObject(objectToDestroy);
		}

		public static void DestroyCollabObject(CollabObject obj)
		{
			obj?.MarkForDeletion();
		}
	}
}
