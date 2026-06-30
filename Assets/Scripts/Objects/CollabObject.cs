using CollabXR.ModExtras;
using CollabXR.ModExtras.Annotation;
using CollabXR.ModLoader;
using CollabXR.Objects.Linker.Sockets;
using CollabXR.VR;
using Cysharp.Threading.Tasks;
using Fusion;
using Meta.XR.EnvironmentDepth;
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

		[Networked]
		public NetworkString<_32> Category { get; set; }

		[Networked]
		public NetworkString<_32> DataName { get; set; }

		[Networked]
		public bool HasData { get; set; }
		public UnityEvent FinishedLoading;

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

		private CollabObjectData LoadDataset()
		{
			return MainLibraryRef.Instance.FindData(Category.ToString(), DataName.ToString());
		}

		private void InitializePrefab()
		{
			Data = LoadDataset();
			if (Data == null)
			{
				if (!Category.ToString().IsNullOrEmpty() && DataName.Value.ToString().IsNullOrEmpty()) // only spawn placeholder if this Object has Data (aka isn't a drawing)
				{
					MainLibraryRef.Instance.onNewDataLoad.AddListener(CheckIfDataLoaded);
					dataRoot = Instantiate(MainLibraryRef.Instance.placeholderPrefab, transform);
					dataRoot.name = MainLibraryRef.Instance.placeholderPrefab.name;
				}
				return;
			}
			else if (Data.prefab == null)
			{
				dataRoot = Instantiate(MainLibraryRef.Instance.placeholderPrefab, transform);
				dataRoot.name = MainLibraryRef.Instance.placeholderPrefab.name;
				CollabObjectPreview preview = dataRoot.GetComponent<CollabObjectPreview>();
				preview.LoadData(Data);
				dataRoot.GetComponent<CollabObjectPreview>()?.EnableLoadingAnimation(true, Data.availableOnThisPlatform);
				dataRoot.GetComponent<CollabObjectPreview>()?.SpawnWithCollider();
				LoadModPrefab().Forget();
				return;
			}

			if (!Data.isSimpleModel)
			{
				return;
			}

			if (Data.prefab != null)
			{
				dataRoot = Instantiate(Data.prefab, transform);
				dataRoot.name = Data.prefab.name;
				FinishedLoading.Invoke();
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

		private async UniTaskVoid LoadModPrefab()
		{
			if (Data.modGUID != null)
			{
				prefabReference = await ModManager.LoadAsset<GameObject>(Data.modGUID, Data.assetGUID);
				dataRoot.GetComponent<CollabObjectPreview>()?.EnableLoadingAnimation(false, Data.availableOnThisPlatform);
				GameObject.Destroy(dataRoot);
				dataRoot = Instantiate(prefabReference.Value, transform);
				dataRoot.name = prefabReference.Value.name;
				BuildSockets(dataRoot);
				FinishedLoading.Invoke();
			}
		}

		public void CheckIfDataLoaded()
		{
			Data = LoadDataset();
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

		private void OnDestroy()
		{
			TryReleaseData();
		}
	}
}
