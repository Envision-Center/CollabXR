using System.Collections.Generic;
using System.Linq;
using CollabXR.ModExtras;
using CollabXR.ModExtras.Annotation;
using CollabXR.ModLoader;
using CollabXR.Networking;
using CollabXR.Objects.Linker.Sockets;
using CollabXR.VR;
using Cysharp.Threading.Tasks;
using Fusion;
using SaintsField.Playa;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

namespace CollabXR.Objects
{
	public enum ObjectState
	{
		Alive,
		ShouldBeDeleted,
		WaitingForAuthority,
		WillBeDeleted,
	}

	public class CollabObject : SpawnableObject
	{
		public CollabObjectData Data { get; private set; }

		/// <summary>
		/// Catalogue category this object belongs to. Used to identify a prefab for spawning.
		/// </summary>
		[Networked]
		public NetworkString<_32> Category { get; set; }

		/// <summary>
		/// Dataset or prefab name this object belongs to. Used to identify a prefab for spawning.
		/// </summary>
		[Networked]
		public NetworkString<_32> DataName { get; set; }

		[Networked]
		public bool HasData { get; set; }
		public UnityEvent FinishedLoading;

		/// <summary>
		/// An array of local sockets that can be indexed for network synchronization.
		/// </summary>
		[ShowInInspector]
		private SocketBase[] sockets = null;

		private const int SOCKET_CAPACITY = 4;

		[Networked, Capacity(SOCKET_CAPACITY), OnChangedRender(nameof(SetSocketLinks)), ShowInInspector]
		public NetworkLinkedList<NetworkLinkerSocketConnection> socketLinks => default;

		private GameObject dataRoot;
		private AssetReference<GameObject> prefabReference;

		public void SetNetworkedData(string folder, string asset)
		{
			Category = folder;
			DataName = asset;
			HasData = true;
			if (folder != Category || asset != DataName)
			{
				Debug.LogWarning("String folder/name are too long! Truncated to " + Category + "/" + DataName);
			}
		}

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		private void OnDestroy()
		{
			if (sockets != null)
			{
				// Free up event listeners for garbage collection
				foreach (SocketBase socket in sockets)
				{
					if (socket != null)
					{
						// Whenever a socket gets connected/disconnected, propogate updates to network peers
						socket.eventConnected.RemoveListener(UpdateSocketLinks);
						socket.eventDisconnected.RemoveListener(UpdateSocketLinks);
					}
				}
			}

			TryReleaseData();
		}

		public override void Spawned()
		{
			base.Spawned();
			InitializePrefab();
			DepthMask mask = GetComponentInChildren<DepthMask>();
			if (mask != null)
			{
				PassthroughManager.Instance.AddDepthMask(mask);
			}
		}

		private CollabObjectData FindDataset()
		{
			return MainLibraryRef.Instance.FindData(Category.ToString(), DataName.ToString());
		}

		private void InitializePrefab()
		{
			Debug.Log("Called InitializePrefab");
			Data = FindDataset();

			// This is a built-in object??
			if (Data == null)
			{
				Debug.Log(string.Format("InitializePrefab: data was null ({0}:{1})", Category, DataName.Value));
				// only spawn placeholder if this Object has Data (aka isn't a drawing)
				if (!Category.ToString().IsNullOrEmpty() && DataName.Value.ToString().IsNullOrEmpty())
				{
					MainLibraryRef.Instance.onNewDataLoad.AddListener(CheckIfDataLoaded);
					FinalizeModPrefab(MainLibraryRef.Instance.placeholderPrefab);
				}
				else
				{
					Debug.Log("no-op because no category or data name");
					EnumerateSockets();
				}
				return;
			}

			if (Data.prefab == null) // This is a mod, data must be streamed in
			{
				Debug.Log("InitializePrefab: prefab was null");
				dataRoot = Instantiate(MainLibraryRef.Instance.placeholderPrefab, transform);
				dataRoot.name = MainLibraryRef.Instance.placeholderPrefab.name;
				CollabObjectPreview preview = dataRoot.GetComponent<CollabObjectPreview>();
				preview.LoadData(Data);
				dataRoot.GetComponent<CollabObjectPreview>()?.EnableLoadingAnimation(true, Data.availableOnThisPlatform);
				dataRoot.GetComponent<CollabObjectPreview>()?.SpawnWithCollider();
				LoadModPrefab().Forget(); // Finalizes mod as soon as it finishes loading
				return;
			}

			// This is a built-in object
			Debug.Log("InitializePrefab: fallback");
			FinalizeModPrefab(prefabReference.Value);
		}

		private async UniTaskVoid LoadModPrefab()
		{
			Debug.Log("load mod prefab async");
			// If this is a mod and not a built-in CollabXR object...
			if (Data.modGUID != null)
			{
				// Waits for mod data to finish loading
				prefabReference = await ModManager.LoadAsset<GameObject>(Data.modGUID, Data.assetGUID);
				// End the loading animation
				dataRoot.GetComponent<CollabObjectPreview>()?.EnableLoadingAnimation(false, Data.availableOnThisPlatform);
				GameObject.Destroy(dataRoot);
				FinalizeModPrefab(prefabReference.Value);
			}
		}

		/// <summary>
		/// Instantiates the final prefab and applies any necessary build steps (such as sockets).
		/// </summary>
		/// <param name="prefab"></param>
		private void FinalizeModPrefab(GameObject prefab)
		{
			Debug.Log("finalize mod prefab: " + prefab.name);
			if (prefab != null)
			{
				dataRoot = Instantiate(prefab, transform);
				dataRoot.name = prefab.name;
				BuildSockets(dataRoot);
			}

			EnumerateSockets();
			if (!HasStateAuthority)
			{
				SetSocketLinks(); // Initial socket link state before other changes are replicated
			}
			FinishedLoading.Invoke();
		}

		public void CheckIfDataLoaded()
		{
			Data = FindDataset();
			if (Data != null)
			{
				MainLibraryRef.Instance.onNewDataLoad.RemoveListener(CheckIfDataLoaded);
				GameObject.Destroy(dataRoot);
				InitializePrefab();
				FinishedLoading.Invoke();
			}
		}

		public string GetWristText()
		{
			return Data.formattedName;
		}

		public CollabObjectData GetData()
		{
			return Data;
		}

		public void TryReleaseData()
		{
			if (prefabReference != null)
			{
				ModManager.ReleaseAsset<GameObject>(prefabReference);
			}
		}

		/// <summary>
		/// Creates a corresponding Socket Output for every Socket Annotation on the given object.
		/// </summary>
		/// <param name="root">Collab Object to build sockets for.</param>
		private void BuildSockets(GameObject root)
		{
			SocketAnnotation[] annotations = root.GetComponentsInChildren<SocketAnnotation>(true);
			foreach (SocketAnnotation annotation in annotations)
			{
				if (!annotation.enabled)
				{
					continue; // Skip instancing annotations that are disabled
				}

				GameObject annotationObject = annotation.gameObject;
				SocketOutput socket = annotationObject.AddComponent<SocketOutput>();
				socket.Initialize(annotation);
			}
		}

		/// <summary>
		/// Walks through all sockets in the object and builds a list of them for network synchronization.
		/// </summary>
		private void EnumerateSockets()
		{
			sockets = gameObject.GetComponentsInChildren<SocketBase>(false);
			foreach (SocketBase socket in sockets)
			{
				// Whenever a socket gets connected/disconnected, propogate updates to network peers
				socket.eventConnected.AddListener(UpdateSocketLinks);
				socket.eventDisconnected.AddListener(UpdateSocketLinks);
				Debug.Log("Added a socket listener " + socket.name);
			}
			Debug.Log($"Sockets enumerated ({sockets.Length}): {sockets}");
		}

		/// <summary>
		/// When not state authority, update socket connections to match.
		/// </summary>
		private void SetSocketLinks()
		{
			Debug.Log("SETTING socket links.");
			// Do nothing if we have state authority, or no sockets
			if (HasStateAuthority || sockets.Length == 0)
			{
				return;
			}

			// Prune existing connections
			for (int i = 0; i < sockets.Length; i++)
			{
				// Given our socket...
				SocketBase fromSocket = sockets[i];

				// For each connected socket it is connected TO...
				foreach (SocketBase toSocket in fromSocket.connections)
				{
					bool connectionDesired = false;

					// Find the respective socket owner and CollabObject...
					NetworkObject toObject = toSocket.GetNetworkObject();
					CollabObject toCollabObject;
					if (toObject != null && toObject.transform.TryGetComponent(out toCollabObject))
					{
						// Get socket index for comparison
						int toSocketId = toCollabObject.FindSocketId(toSocket);

						// ...and validate that we want the connection.
						foreach (NetworkLinkerSocketConnection link in socketLinks)
						{
							if ((int)link.fromSocketIndex == i && link.toObject == toObject.Id && (int)link.toSocketIndex == toSocketId)
							{
								connectionDesired = true;
								break;
							}
						}
					}

					if (!connectionDesired)
					{ // If we found no link indicating a desired connection, prune the actual connection
						fromSocket.Disconnect(toSocket);
						Debug.Log(string.Format("Pruned link {0}:{1} -> {2}", Id, i, toObject?.Id));
					}
				}
			}

			// Form new connections as necessary
			foreach (NetworkLinkerSocketConnection link in socketLinks)
			{
				SocketBase fromSocket = sockets[link.fromSocketIndex];

				CollabObject toObject = FindCollabObject(link.toObject);
				// Skip object if data is invalid
				if (toObject == null || toObject.sockets.Length <= link.toSocketIndex)
				{
					continue;
				}
				SocketBase toSocket = toObject.sockets[link.toSocketIndex];

				if (!fromSocket.IsConnected(toSocket) && fromSocket.CanConnect(toSocket))
				{
					Debug.Log(string.Format("Created link {0}:{1} -> {2}:{3}", Id, link.fromSocketIndex, link.toObject, link.toSocketIndex));
					fromSocket.Connect(toSocket);
				}
			}

			Debug.Log(socketLinks);
		}

		/// <summary>
		/// When a socket is changed, and we ARE state authority, update socketLinks array to match.
		/// </summary>
		private void UpdateSocketLinks()
		{
			Debug.Log("Update Socket Links call " + HasStateAuthority.ToString());
			if (!HasStateAuthority)
			{
				return;
			}

			// TODO: we could instead bind individual events for each socket forming and losing connections,
			// instead of iterating over the entire array?

			// Create a new socket list from scratch
			List<NetworkLinkerSocketConnection> connectionList = new List<NetworkLinkerSocketConnection>();

			Debug.Log("UPDATING socket links.");
			for (int i = 0; i < sockets.Length; i++)
			{
				// Given our socket...
				SocketBase fromSocket = sockets[i];

				// For each connected socket it is connected TO...
				foreach (SocketBase toSocket in fromSocket.connections)
				{
					// Find the respective socket owner and CollabObject...
					NetworkObject toObject = toSocket.GetNetworkObject();
					CollabObject toCollabObject;
					if (toObject != null && toObject.transform.TryGetComponent(out toCollabObject))
					{
						// Get socket index for comparison
						int toSocketId = toCollabObject.FindSocketId(toSocket);
						connectionList.Add(
							new NetworkLinkerSocketConnection
							{
								fromSocketIndex = (ushort)i,
								toObject = toObject.Id,
								toSocketIndex = (ushort)toSocketId,
							}
						);
					}
				}
			}

			// Remove any connections that disappeared
			foreach (NetworkLinkerSocketConnection link in socketLinks)
			{
				if (!connectionList.Contains(link))
				{
					socketLinks.Remove(link);
				}
			}

			// Add any new socket links
			foreach (NetworkLinkerSocketConnection link in connectionList)
			{
				if (!socketLinks.Contains(link))
				{
					socketLinks.Add(link);
				}
			}

			Debug.Log(socketLinks);
		}

		private int FindSocketId(SocketBase socket)
		{
			for (int i = 0; i < sockets.Length; i++)
			{
				if (sockets[i] == socket)
				{
					return i;
				}
			}
			return -1;
		}

		/// <summary>
		/// Attempts to find a CollabObject using the given NetworkID.
		/// </summary>
		/// <param name="component"></param>
		/// <returns>The found CollabObject, or null otherwise</returns>
		public static CollabObject FindCollabObject(NetworkId searchForId)
		{
			NetworkObject obj;
			if (NetworkManager.Runner.TryFindObject(searchForId, out obj))
			{
				CollabObject collabObj;
				if (obj.TryGetComponent(out collabObj))
				{
					return collabObj;
				}
			}
			return null;
		}
	}
}
