using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CollabXR.Networking;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects
{
	// this is where all the shared parenting and deletion code between CollabObject and BrushSubStroke lives
	public class SpawnableObject : NetworkBehaviour
	{
		public ObjectState state = ObjectState.Alive;
		private SpawnableObject myParent;
		public bool IsIndepedentObject => (transform.parent == null || transform.parent.GetComponentInParent<SpawnableObject>() == null);

		private void FixedUpdate()
		{
			//if (!initialized) InitializeStroke();
			if (Object.HasStateAuthority && (state == ObjectState.ShouldBeDeleted || state == ObjectState.WaitingForAuthority))
			{
				Debug.Log($"I have state authority and am deleting {gameObject.name}");
				state = ObjectState.WillBeDeleted;
				DespawnObject();
			}
			else if (state == ObjectState.ShouldBeDeleted)
			{
				Debug.Log($"I don't have state authority and want to delete {gameObject.name}");
				state = ObjectState.WaitingForAuthority;
				Object.RequestStateAuthority();
			}
		}

		public virtual void MarkForDeletion()
		{
			if (state == ObjectState.Alive)
			{
				state = ObjectState.ShouldBeDeleted;
				OnFirstMarkedForDeletion();
			}
		}

		public virtual void OnFirstMarkedForDeletion() {
			foreach (SpawnableObject nestedSpawnable in transform.GetComponentsInChildren<SpawnableObject>())
			{
				nestedSpawnable?.MarkForDeletion();
			}
		}

		public override void Spawned()
		{
			base.Spawned();
			if (myParent != null)
			{
				transform.parent = myParent.transform;
			}
		}

		public void DespawnObject()
		{
			NetworkManager.Runner.Despawn(Object);
		}

		public void ParentToOtherCollabObject(CollabObject obj) // for attaching brush containers to objects
		{
			myParent = obj;
			if (Object.IsValid)
			{
				transform.parent = myParent.transform;
			}
		}

		IEnumerator DespawnAfterDelay() // not used
		{
			yield return new WaitForSeconds(0.5f);
			NetworkManager.Runner.Despawn(Object);
		}
	}
}
