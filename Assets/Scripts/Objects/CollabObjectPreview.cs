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
			if (state == PreviewState.Loading && data != null)
			{
				if (ModManager.Instance.indexedMods.ContainsKey(data.modGUID))
				{
					UnityWebRequest request = ModManager.Instance.TryGetUnityWebRequest(data.modGUID);
					if (request != null)
					{
						bool successful_progress = request.result == UnityWebRequest.Result.Success || request.result == UnityWebRequest.Result.InProgress;
						if (!successful_progress)
						{
							EnableLoadingAnimation(true, false);
						}
						else
						{
							loadingBar.fillAmount = request.downloadProgress;
						}
					}
				}
				else
				{
					EnableLoadingAnimation(true, false);
				}
			}
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
