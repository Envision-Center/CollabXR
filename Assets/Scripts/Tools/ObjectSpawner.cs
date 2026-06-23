using System;
using System.Linq;
using CollabXR.Networking;
using CollabXR.Objects;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	public class ObjectSpawner : MonoBehaviour
	{
		[SerializeField]
		private GameObject simpleModelContainer;

		public Vector3 defaultSpawnPos;

		public EventVariable<CollabObjectData> DataToSpawn = new();

		private bool previewObjectDoesExist;
		private GameObject previewObject;
		private Bounds previewBounds;

		private void Awake()
		{
			DataToSpawn.AddListenerAndCheck(OnDataChange);
		}

		private void OnDataChange(CollabObjectData data)
		{
			DestroyPreviewObject();

			if (DataToSpawn.Value != null)
			{
				previewObject = InstantiateObjectAsPreview(DataToSpawn.Value);
				CollabObjectHelper.GetOrientedBoundingBoxFromMeshes(previewObject, out previewBounds, out Matrix4x4 orietation);
				previewObjectDoesExist = true;

				SetPreviewVisibility(isActiveAndEnabled);
			}
		}

		private void SetPreviewVisibility(bool visible)
		{
			if (!previewObjectDoesExist)
				return;

			previewObject.SetActive(visible);
		}

		private void OnEnable()
		{
			SetPreviewVisibility(isActiveAndEnabled);
		}

		private void OnDisable()
		{
			SetPreviewVisibility(isActiveAndEnabled);
		}

		public void DestroyPreviewObject()
		{
			if (!previewObjectDoesExist)
				return;

			previewObjectDoesExist = false;

			Destroy(previewObject);
		}

		private static readonly Type[] blacklistedPreviewComponents =
		{
			typeof(MonoBehaviour),
			typeof(NetworkRigidbody3D),
			typeof(NetworkObject),
			typeof(CollabObject),
			typeof(Animator),
			typeof(Collider),
			typeof(Rigidbody),
		};

		private static GameObject InstantiateObjectAsPreview(CollabObjectData data)
		{
			GameObject source = data.prefab == null ? MainLibraryRef.Instance.placeholderPrefab : data.prefab;
			GameObject preview = Instantiate(source);
			preview.name = source.name;
			preview.SetActive(false);

			if (data.prefab == null)
			{
				preview.GetComponent<CollabObjectPreview>().LoadData(data);
			}
			else
			{
				RemoveBlacklistedComponents(preview);
			}

			DontDestroyOnLoad(preview);
			preview.SetActive(true);

			return preview;
		}

		public static void RemoveBlacklistedComponents(GameObject obj)
		{
			Component[] components = obj.GetComponentsInChildren<Component>(true);

			for (int i = 0; i < 100; i++)
			{
				// Because you can't force destroy components that others depend on >:(
				bool allBlacklistedDestroyed = true;

				foreach (var c in components)
				{
					Type componentType = c.GetType();

					if (c == null)
						continue;

					foreach (Type t in blacklistedPreviewComponents)
					{
						if (componentType == t || componentType.IsSubclassOf(t))
						{
							DestroyImmediate(c, false);
							if (c != null)
								allBlacklistedDestroyed = false;
							break;
						}
					}
				}

				if (allBlacklistedDestroyed)
					break;
			}
		}

		public void SetDefaultSpawnPos(Vector3 pos)
		{
			defaultSpawnPos = pos;

			if (previewObjectDoesExist)
			{
				defaultSpawnPos += new Vector3(0, previewBounds.extents.y - previewBounds.center.y, 0);

				//Matrix4x4 targetTrans = Matrix4x4.TRS(defaultSpawnPos, GetDefaultSpawnRot(), Vector3.one);

				//Vector3 boxTestPos = targetTrans.MultiplyPoint(previewBounds.center);
				//Vector3 initialBoxTestPos = boxTestPos;

				//penetrationTestCollider.size = previewBounds.size;
				//Collider[] cols = Physics.OverlapBox(boxTestPos, previewBounds.extents * 2, targetTrans.rotation);

				//for(int i = 0; i < cols.Length; i++)
				//{
				//	var col = cols[i];
				//	bool hit = Physics.ComputePenetration(penetrationTestCollider, boxTestPos, targetTrans.rotation,
				//		col, col.transform.position, col.transform.rotation,
				//		out Vector3 direction, out float distance);

				//	if (hit)
				//	{
				//		boxTestPos += direction * distance;
				//	}
				//}

				//defaultSpawnPos += (boxTestPos - initialBoxTestPos);

				previewObject.transform.SetPositionAndRotation(defaultSpawnPos, GetDefaultSpawnRot());
			}
		}

		public Quaternion GetDefaultSpawnRot()
		{
			Vector3 flatForward = transform.forward;
			flatForward.y = 0;
			flatForward.Normalize();
			return Quaternion.LookRotation(flatForward, Vector3.up);
		}

		public void SpawnObject()
		{
			if (enabled)
				SpawnObject(defaultSpawnPos, GetDefaultSpawnRot());
		}

		public void SpawnObject(Vector3 position, Quaternion rotation) => SpawnObject(DataToSpawn.Value, position, rotation);

		public void SpawnObject(CollabObjectData data, Vector3 position, Quaternion rotation)
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanPlace)
				return;

			if (!data.isSimpleModel && ReferenceEquals(data.prefab, null))
				return;

			GameObject objectToSpawn = data.isSimpleModel ? simpleModelContainer : data.prefab;

			CollabObjectData spawnedData = data;

			NetworkManager.Runner.Spawn(
				objectToSpawn,
				position,
				rotation,
				PlayerRef.None,
				delegate(NetworkRunner _, NetworkObject newObject)
				{
					// Debug.Log("Setting data before spawn " + spawnedData.assetFolder + "/" + spawnedData.assetName);
					CollabObject collabObject = newObject.GetComponent<CollabObject>();
					collabObject.SetNetworkedData(spawnedData.category, spawnedData.assetName);
				}
			);
		}
	}
}
