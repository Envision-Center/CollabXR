using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace CollabXR.ModLoader
{
	public class TestPrefabLoader : MonoBehaviour
	{
		AssetReference<GameObject> externalPrefab = null;

		List<GameObject> prefabsToDestroy = new();

		[SerializeField]
		public string modUuid = "991dd7f2-443c-4e61-9cf4-321dd4737020";

		[SerializeField]
		public List<string> loadableAssets = new() { "0d66fb16-3be1-6894-896c-28b96595e2d0", "cf6b6379-b3a6-8e24-daf9-b56b45fb3b73", "19e98f8a-85a2-82f4-7a17-978af04d47a2" };

		[SerializeField]
		public float timerOffset = 0;

		void Start()
		{
			changeTimer = timerOffset % 3.0f;
			changeIndex += (int)Mathf.Floor(timerOffset / 3.0f);

			if (timerOffset <= 0)
			{
				_ = LoadModel(new Guid(modUuid), new Guid(loadableAssets[changeIndex]));
			}
		}

		int changeIndex = 0;
		float changeTimer = 0;

		void Update()
		{
			changeTimer += Time.deltaTime;

			if (changeTimer > 3)
			{
				changeTimer = 0;
				changeIndex++;

				if (changeIndex >= loadableAssets.Count)
				{
					changeIndex = 0;
				}

				_ = LoadModel(new Guid(modUuid), new Guid(loadableAssets[changeIndex]));
			}
		}

		void OnDestroy()
		{
			while (prefabsToDestroy.Count > 0)
			{
				Destroy(prefabsToDestroy[0]);
				prefabsToDestroy.RemoveAt(0);
			}
			if (externalPrefab != null)
			{
				ModManager.ReleaseAsset(externalPrefab);
			}
		}

		private async UniTask LoadModel(Guid modGuid, Guid assetGuid)
		{
			//await UniTask.SwitchToMainThread();

			while (prefabsToDestroy.Count > 0)
			{
				Destroy(prefabsToDestroy[0]);
				prefabsToDestroy.RemoveAt(0);
			}

			if (externalPrefab != null)
			{
				ModManager.ReleaseAsset(externalPrefab);
			}

			externalPrefab = await ModManager.LoadAsset<GameObject>(modGuid, assetGuid);

			prefabsToDestroy.Add(Instantiate(externalPrefab.Value, transform));
		}
	}
}
