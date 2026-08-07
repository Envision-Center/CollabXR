using System;
using System.Collections.Generic;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Linker.Sockets
{
	// https://doc.photonengine.com/fusion/v2/manual/fusion-types/network-collections
	[Serializable]
	public struct NetworkLinkerSocketConnection : INetworkStruct
	{
		public ushort fromSocketIndex; // TODO: could be compressed as a byte?
		public NetworkId toObject;
		public ushort toSocketIndex;
	}

	/// <summary>
	/// How data is piped through this socket.
	/// </summary>
	public enum SocketFlowDirection
	{
		/// <summary>
		/// Data is pushed OUT of this socket. Can only be connected to inputs.
		/// </summary>
		Output = 0,

		/// <summary>
		/// Data is pushed INTO this socket. Can only be connected to outputs.
		/// </summary>
		Input = 1,
	}

	///// <summary>
	///// What kind of data is being passed through this socket.
	///// </summary>
	//public enum SocketDataType
	//{
	//	/// <summary>
	//	/// Used for real-time graphing, describing states.
	//	/// Placeholder.
	//	/// </summary>
	//	Integer = 0,

	//	/// <summary>
	//	/// Used for real-time graphing, processing, etc.
	//	/// Placeholder.
	//	/// </summary>
	//	Float = 1,

	//	/// <summary>
	//	/// Pass a 3D vector. Useful for 3D computation.
	//	/// Placeholder.
	//	/// </summary>
	//	Vector3 = 2,

	//	/// <summary>
	//	/// Pass a world-space 4x4 transformation matrix. Useful for handheld tools.
	//	/// Placeholder.
	//	/// </summary>
	//	Matrix = 3,

	//	/// <summary>
	//	/// Used for static images, video feeds, etc.
	//	/// Placeholder.
	//	/// </summary>
	//	Texture2D = 4,

	//	/// <summary>
	//	/// Used for volumetric data visualization, like volume slicing.
	//	/// We may want to pass additional data with this, like a transform and volume boundaries.
	//	/// Placeholder.
	//	/// </summary>
	//	Texture3D = 5,

	//	/// <summary>
	//	/// Used for real-time audio data (that can maybe be processed?).
	//	/// Placeholder.
	//	/// </summary>
	//	AudioStream = 6,

	//	/// <summary>
	//	/// Pass a generic data structure, such as metadata.
	//	/// </summary>
	//	ScriptableObject = 7,
	//}

	/// <summary>
	/// Base class for sockets.
	/// A socket is used to pipe data from one Prefab to another, or within a single prefab.
	/// Sockets can be connected via the Linker tool.
	///
	/// Most often, the base socket will need to be inherited by another socket to add functionality.
	/// </summary>
	public class SocketBase : MonoBehaviour
	{
		[Tooltip("The direction that data flows through this socket. Most mods will require data to flow outward, rather than inward.")]
		public SocketFlowDirection flow = SocketFlowDirection.Output;

		/// <summary>
		/// List of sockets we are connected to.
		/// If this list is populated before startup, and this is an input socket,
		/// the socket will automatically attempt to form those connections.
		/// </summary>
		[Tooltip("List of sockets we are connected to. Can be preset to connect at startup.")]
		public List<SocketBase> connections = new List<SocketBase>();

		[NonSerialized]
		public UnityEvent eventConnected = new UnityEvent();

		[NonSerialized]
		public UnityEvent eventDisconnected = new UnityEvent();

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			if (flow == SocketFlowDirection.Input && connections.Count > 0)
			{
				// Swap out connection list so it does not appear that we have any connections initially
				List<SocketBase> oldConnections = connections;
				connections = new List<SocketBase>();

				// Now attempt to connect to each socket
				foreach (SocketBase socket in connections)
				{
					if (CanConnect(socket))
					{
						Connect(socket);
					}
				}
			}

			// Ensure sockets are on the correct layer
			gameObject.SetLayerRecursively(LayerMask.NameToLayer("LinkSocket"));

			// Ensure sockets have a corresponding collider for linking with
			SphereCollider collider;
			if (!TryGetComponent(out collider))
			{
				collider = gameObject.AddComponent<SphereCollider>();
			}
			collider.radius = 0.2f;
			collider.isTrigger = false;
			collider.providesContacts = true;

			//MeshRenderer mesh;
			//if (!TryGetComponent(out mesh))
			//{
			//	mesh = gameObject.AddComponent<MeshRenderer>();
			//}

			//MeshFilter meshFilter;
			//if (!TryGetComponent(out meshFilter))
			//{
			//	meshFilter = gameObject.AddComponent<MeshFilter>();
			//}
			//meshFilter.mesh = mesh.res;

			GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			primitive.transform.parent = transform;
			primitive.transform.localPosition = Vector3.zero;
			primitive.transform.localScale = new Vector3(collider.radius, collider.radius, collider.radius);
			primitive.GetComponent<SphereCollider>().enabled = false;
		}

		// Update is called once per frame
		void Update() { }

		private void OnDestroy()
		{
			// If we're an input socket, disconnect all attached outputs
			if (flow == SocketFlowDirection.Input)
			{
				foreach (SocketBase connection in connections)
				{
					Disconnect(connection);
				}
			}
			else
			{ // If we're an output, just disconnect ourselves from our inputs
				foreach (SocketBase input in connections)
				{
					input.Disconnect(this);
				}
			}

			Debug.Log("SOCKET BASE: OnDestroy finished");
		}

		/// <summary>
		/// Determines whether this socket can accept input from the given socket.
		/// This can be overridden for subclasses like monitors.
		/// </summary>
		/// <param name="otherSocket">The socket to test against.</param>
		/// <returns>Whether or not this socket can receive data from the other socket.</returns>
		public virtual bool CanConnect(SocketBase otherSocket)
		{
			if (otherSocket == null)
			{
				Debug.LogError("Called CanConnect with a null reference!!!!!!!");
				return false;
			}
			// Make sure we support the data type,
			// that the data flows in the right direction,
			// and that the connection does not already exist
			Debug.Log(string.Format("CanConnect: {0}, {1}, {2}", otherSocket != null, otherSocket?.flow != flow, !connections.Contains(otherSocket)));
			return otherSocket != null && otherSocket.flow != flow && !connections.Contains(otherSocket);
		}

		/// <summary>
		/// Determines whether this socket is connected to the other one.
		/// </summary>
		/// <param name="otherSocket"></param>
		/// <returns>True if outputting to that socket.</returns>
		public bool IsConnected(SocketBase otherSocket)
		{
			return connections.Contains(otherSocket);
		}

		/// <summary>
		/// Attempts to connect the output socket to this input one.
		/// <br/>
		/// THIS DOES NOT VALIDATE WHETHER THE SOCKETS ARE CONNECTABLE BEFOREHAND.
		/// Please call CanConnectTo beforehand to determine whether this connection attempt should even be permitted.
		/// </summary>
		/// <param name="otherSocket">Socket to perform the connection to.</param>
		public void Connect(SocketBase dataProvider)
		{
			OnConnect(dataProvider);
			dataProvider.OnConnect(this);

			eventConnected.Invoke();

			Debug.Log("SOCKET CONNECTED!!!");
		}

		/// <summary>
		/// Disconnects this socket from the other one.
		/// </summary>
		/// <param name="dataProvider"></param>
		/// <returns></returns>
		public bool Disconnect(SocketBase dataProvider)
		{
			// Ensure it was not already connected
			if (!connections.Contains(dataProvider))
			{
				return false;
			}

			dataProvider.OnDisconnect(this);
			OnDisconnect(dataProvider);

			eventDisconnected.Invoke();

			dataProvider.connections.Remove(this);
			connections.Remove(dataProvider);

			return true;
		}

		/// <summary>
		/// Emitted when this socket is connected to another.
		/// </summary>
		public virtual void OnConnect(SocketBase otherSocket) { }

		/// <summary>
		/// Emitted when this socket is disconnected from another.
		/// </summary>
		public virtual void OnDisconnect(SocketBase otherSocket) { }

		/// <summary>
		/// Returns the NetworkID of the parent NetworkObject, if any.
		/// </summary>
		/// <returns></returns>
		public NetworkObject GetNetworkObject()
		{
			return _GetNetworkObject(transform);
		}

		/// <summary>
		/// Slowly walks up the chain of transforms to find the NetworkId.
		/// </summary>
		/// <param name="from"></param>
		/// <returns>The NetworkId of the ancestor NetworkObject, or a blank NetworkId</returns>
		private NetworkObject _GetNetworkObject(Transform from)
		{
			if (from == null)
			{
				// Invalid network ID
				Debug.LogError("No network object found for the given socket!");
				return null;
			}

			NetworkObject obj;
			if (from.TryGetComponent(out obj))
			{
				return obj;
			}
			return _GetNetworkObject(from.parent);
		}
	}
}
