using System;
using System.Collections;
using System.Collections.Generic;
using CollabXR.ModLoader;
using CollabXR.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace CollabXR.Objects
{
	public class CollabObjectPreview : MonoBehaviour
	{
		public enum PreviewState
		{
			Disabled,
			Loading,
			NotAvailable,
		}

		private PreviewState state;

		private AssetReference<GameObject> assetRef;

		[SerializeField]
		private GameObject previewCube,
			notAvailableCube;

		[SerializeField]
		private GameObject loadingVisual;

		[SerializeField]
		private Image loadingBar;
		private CollabObjectData data;
		public BoxCollider objCollider;

		private void Awake()
		{
			objCollider.enabled = false;
		}

		public void LoadData(CollabObjectData data)
		{
			this.data = data;
			EnableLoadingAnimation(true, data.availableOnThisPlatform);
			LoadPrefab(data).Forget();

			SetupPreviewAsync().Forget();
		}

		private async UniTaskVoid SetupPreviewAsync()
		{
			EnableLoadingAnimation(true, true);

			Debug.Log($"CollabObjectPreview: Waiting for mod {data.modGUID} to be indexed...");

			await UniTask.WaitUntil(() => ModManager.Instance.indexedMods.ContainsKey(data.modGUID));

			Debug.Log($"CollabObjectPreview: Mod {data.modGUID} indexed, waiting for download to start...");
			loadingBar.fillAmount = 0;
			while (state == PreviewState.Loading)
			{
				if (!RequestExists(out UnityWebRequest request))
				{
					Debug.Log($"CollabObjectPreview: Mod {data.modGUID} download request not found, waiting for download to start...");
					await UniTask.Yield();
					continue;
				}

				if (request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.InProgress)
				{
					Debug.Log($"CollabObjectPreview: Mod {data.modGUID} download in progress, progress: {request.downloadProgress}");
					loadingBar.fillAmount = request.downloadProgress;
				}
				else
				{
					Debug.Log($"CollabObjectPreview: Mod {data.modGUID} download failed");
					EnableLoadingAnimation(true, false);
					break;
				}

				await UniTask.Yield();
			}
		}

		private bool RequestExists(out UnityWebRequest request)
		{
			request = ModManager.Instance.TryGetUnityWebRequest(data.modGUID);
			if (request == null)
			{
				ModLoadTask task = new(data.modGUID);
				ModManager.Instance.LoadMod(task);

				return false;
			}

			return true;
		}

		public async UniTaskVoid LoadPrefab(CollabObjectData data)
		{
			assetRef = await ModManager.LoadAsset<GameObject>(data.modGUID, data.assetGUID);
			GameObject previewInstance = Instantiate(assetRef.Value, transform);
			EnableLoadingAnimation(false, data.availableOnThisPlatform);
			ObjectSpawner.RemoveBlacklistedComponents(previewInstance);
		}

		public void EnableLoadingAnimation(bool enable, bool availableOnThisPlatform)
		{
			loadingVisual.SetActive(enable);
			if (enable)
			{
				state = availableOnThisPlatform ? PreviewState.Loading : PreviewState.NotAvailable;
			}
			else
			{
				state = PreviewState.Disabled;
			}
			previewCube.SetActive(availableOnThisPlatform);
			notAvailableCube.SetActive(!availableOnThisPlatform);
		}

		public void SpawnWithCollider()
		{
			objCollider.enabled = true;
		}

		private void Update()
		{
			
		}

		private void OnDestroy()
		{
			if (assetRef != null)
			{
				ModManager.ReleaseAsset(assetRef);
			}
		}
	}
}
