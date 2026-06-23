using CollabXR.Networking;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

namespace CollabXR.Objects.Components
{
	public class Emitter : MonoBehaviour
	{
		[SerializeField]
		private NetworkObject prefab;

		[FormerlySerializedAs("initialVelocity")]
		[Header("Local to emitter gameobject")]
		[SerializeField]
		private Vector3 initRelativeVelocity;

		private bool hasRigidbody;

		private void Awake()
		{
			hasRigidbody = prefab.GetComponent<Rigidbody>() != null;
		}

		public void Emit()
		{
			NetworkManager.Runner.Spawn(
				prefab.gameObject,
				transform.position,
				transform.rotation,
				NetworkManager.Runner.LocalPlayer,
				(runner, o) =>
				{
					if (!hasRigidbody)
						return;

					Rigidbody rb = o.GetComponent<Rigidbody>();

					rb.linearVelocity = transform.TransformDirection(initRelativeVelocity);
				}
			);
		}
	}
}
