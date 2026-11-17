using System.Collections.Generic;
using UnityEngine;

namespace CollabXR
{
	/// <summary>
	/// Drives frame physics work for only the bodies that currently need it to avoid frame polling.
	/// </summary>
	[DefaultExecutionOrder(20)] // After TransformFollow and NetworkGrabbable move held objects in LateUpdate
	public class PhysicsBodyUpdater : MonoBehaviour
	{
		private static readonly List<NetworkRigidbodyAdapter> heldBodies = new();
		private static readonly List<NetworkRigidbodyAdapter> scaledGravityBodies = new();
		private static PhysicsBodyUpdater instance;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			heldBodies.Clear();
			scaledGravityBodies.Clear();
			instance = null;
		}

		public static void AddHeld(NetworkRigidbodyAdapter body) => Add(heldBodies, body);

		public static void RemoveHeld(NetworkRigidbodyAdapter body) => heldBodies.Remove(body);

		public static void AddScaledGravity(NetworkRigidbodyAdapter body) => Add(scaledGravityBodies, body);

		public static void RemoveScaledGravity(NetworkRigidbodyAdapter body) => scaledGravityBodies.Remove(body);

		private static void Add(List<NetworkRigidbodyAdapter> list, NetworkRigidbodyAdapter body)
		{
			if (list.Contains(body))
				return;

			list.Add(body);

			if (instance == null)
			{
				GameObject host = new(nameof(PhysicsBodyUpdater));
				DontDestroyOnLoad(host);
				instance = host.AddComponent<PhysicsBodyUpdater>();
			}
		}

		private void LateUpdate()
		{
			for (int i = heldBodies.Count - 1; i >= 0; i--)
				heldBodies[i].TickHeld();
		}

		private void FixedUpdate()
		{
			for (int i = scaledGravityBodies.Count - 1; i >= 0; i--)
				scaledGravityBodies[i].TickScaledGravity();
		}
	}
}
