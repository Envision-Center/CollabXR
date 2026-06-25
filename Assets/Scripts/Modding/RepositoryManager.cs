using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CollabXR;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace CollabXR.ModLoader
{
	public class RepositoryManager : SingletonBehavior<RepositoryManager>
	{
		private const string DEBUG_LOG_HEADER = "<color=#a557ff>[Repository Manager]</color>";

		public List<string> activeRepositories { get; private set; } = new List<string>() { };

		private List<string> defaultRepositories = new List<string>() { "https://vpa33j6tuqgrwanxgjoqmxw7wy0vvwib.lambda-url.us-east-1.on.aws/" };

		public Dictionary<string, RepositoryMetadata> loadedRepositories { get; private set; } = new();

		internal bool DoneLoadingRepositories = false;

		private List<RepositoryManagerLoadingAwaiter> loadingRepositoryAwaiters = new();

		public Action repositoriesRefreshed;

		private string repositoryFilePath;

		public enum RepositoryAddResult
		{
			InvalidURL,
			Duplicate,
			Success,
		}

		internal void NotifyLoadingBegin()
		{
			DoneLoadingRepositories = false;
		}

		internal void NotifyLoadingDone()
		{
			DoneLoadingRepositories = true;

			foreach (RepositoryManagerLoadingAwaiter loadingRepositoryAwaiter in loadingRepositoryAwaiters)
			{
				loadingRepositoryAwaiter.NotifyLoadingDone();
			}

			loadingRepositoryAwaiters.Clear();
			repositoriesRefreshed.Invoke();
		}

		public RepositoryManagerLoadingAwaiter GetAwaiter()
		{
			RepositoryManagerLoadingAwaiter newAwaiter = new RepositoryManagerLoadingAwaiter();

			if (!DoneLoadingRepositories)
				loadingRepositoryAwaiters.Add(newAwaiter);

			return newAwaiter;
		}

		private async UniTask RefreshRepositories()
		{
			NotifyLoadingBegin();

			Instance.loadedRepositories.Clear();

			Debug.Log($"{DEBUG_LOG_HEADER} Starting refresh of all mod repositories...");

			List<UniTask> repositoryInfoRetrieveTasks = new();

			foreach (string repositoryUrl in Instance.activeRepositories)
			{
				repositoryInfoRetrieveTasks.Add(RetrieveRepositoryInfo(repositoryUrl));
			}

			await UniTask.WhenAll(repositoryInfoRetrieveTasks);

			NotifyLoadingDone();
		}

		private async UniTask RetrieveRepositoryInfo(string metadataUrl)
		{
			// unitywebrequests must be created on the main thread :(
			//await UniTask.SwitchToMainThread();

			try
			{
				Debug.Log($"{DEBUG_LOG_HEADER} Loading Metadata for \"{metadataUrl}\"...");

				UnityWebRequest repositoryRequest = UnityWebRequest.Get(metadataUrl);

				await repositoryRequest.SendWebRequest();

				RepositoryMetadata metadata = JsonConvert.DeserializeObject<RepositoryMetadata>(repositoryRequest.downloadHandler.text);

				string query = new Uri(metadataUrl).Query;
				NameValueCollection parameters = System.Web.HttpUtility.ParseQueryString(query);
				metadata.accessKey = parameters["access"];
				metadata.secretKey = parameters["secret"];

				Instance.loadedRepositories[metadataUrl] = metadata;

				Debug.Log($"{DEBUG_LOG_HEADER} Loaded Metadata for \"{metadataUrl}\": V{metadata.StructVersion}@{metadata.BaseURL} hosted by: {metadata.RepoOwner} ({metadata.Mods.Length} mods)");

				List<UniTask> indexModsTasks = new();

				foreach (Guid modUuid in metadata.Mods)
				{
					indexModsTasks.Add(ModManager.Instance.IndexMod(metadataUrl, modUuid));
				}

				//await UniTask.SwitchToThreadPool();

				await UniTask.WhenAll(indexModsTasks);

				//await UniTask.SwitchToMainThread();

				Debug.Log($"{DEBUG_LOG_HEADER} All mods loaded from repo: {metadataUrl}");
			}
			catch (Exception e)
			{
				Debug.Log($"{DEBUG_LOG_HEADER} Failed to load Metadata for \"{metadataUrl}\"]: {e.Message}");
			}
		}

		/// <summary>
		/// TODO: Add Summary
		/// </summary>
		public static RepositoryAddResult AddRepository(string url, bool serializeList)
		{
			if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result))
			{
				return RepositoryAddResult.InvalidURL;
			}
			if (Instance.activeRepositories.Contains(url))
			{
				return RepositoryAddResult.Duplicate;
			}

			Instance.activeRepositories.Add(url);

			Debug.Log($"{DEBUG_LOG_HEADER} Added repository \"{url}\", retrieving metadata...");

			Task.Run(async () =>
			{
				// switching to main thread for web request
				await UniTask.SwitchToMainThread();

				Instance.NotifyLoadingBegin();

				await Instance.RetrieveRepositoryInfo(url);

				Instance.NotifyLoadingDone();
			});

			if (serializeList)
			{
				SerializeRepositories();
			}
			return RepositoryAddResult.Success;
		}

		/// <summary>
		/// TODO: Add Summary
		/// </summary>
		public static void RefreshRepository(string url)
		{
			if (!Instance.activeRepositories.Contains(url))
			{
				return;
			}

			if (Instance.loadedRepositories.ContainsKey(url))
			{
				Debug.Log($"{DEBUG_LOG_HEADER} Refreshing repository \"{url}\"...");

				Task.Run(async () =>
				{
					Instance.NotifyLoadingBegin();

					await Instance.RetrieveRepositoryInfo(url);

					Instance.NotifyLoadingDone();
				});
			}
		}

		/// <summary>
		/// TODO: Add Summary
		/// </summary>
		public static bool RemoveRepository(string url)
		{
			if (Instance.loadedRepositories.ContainsKey(url))
			{
				bool safeToUnload = true;

				if (safeToUnload)
				{
					if (Instance.activeRepositories.Contains(url))
					{
						Instance.activeRepositories.Remove(url);
					}

					Instance.loadedRepositories.Remove(url);
					SerializeRepositories();

					Debug.Log($"{DEBUG_LOG_HEADER} Removed repository \"{url}\".");
				}
				else
				{
					Debug.Log($"{DEBUG_LOG_HEADER} Unable to remove repository \"{url}\", unsafe to unload.");
				}

				return safeToUnload;
			}

			if (Instance.activeRepositories.Contains(url))
			{
				Instance.activeRepositories.Remove(url);
			}

			return true;
		}

		public static void RefreshAllMods()
		{
			ModManager.Instance?.DeIndexAllMods();
			Instance.RefreshRepositories().Forget();
		}

		private static void SerializeRepositories()
		{
			Debug.Log($"{DEBUG_LOG_HEADER} Writing repository list to {Instance.repositoryFilePath}");
			string serializedList = JsonConvert.SerializeObject(Instance.activeRepositories);
			string encodedList = Convert.ToBase64String(Encoding.UTF8.GetBytes(serializedList));
			File.WriteAllText(Instance.repositoryFilePath, encodedList);
		}

		protected override void Awake()
		{
			base.Awake();

			DontDestroyOnLoad(this);

			repositoryFilePath = Path.Combine(Application.persistentDataPath, "activeRepositories.json");

			try // loading saved and default repositories
			{
				if (File.Exists(repositoryFilePath))
				{
					Debug.Log($"{DEBUG_LOG_HEADER} Reading repository list at {Instance.repositoryFilePath}");
					string encodedList = File.ReadAllText(repositoryFilePath);
					string decodedList = Encoding.UTF8.GetString(Convert.FromBase64String(encodedList));
					activeRepositories = JsonConvert.DeserializeObject<List<string>>(decodedList);
				}
				else
				{
					Debug.Log($"{DEBUG_LOG_HEADER} No file exists at {Instance.repositoryFilePath}");
					activeRepositories = new List<string>();
				}
			}
			catch (Exception e)
			{
				Debug.Log($"{DEBUG_LOG_HEADER} Failed to load saved repository list: {e.Message}");
			}

			foreach (string repo in defaultRepositories)
			{
				AddRepository(repo, false);
			}

			RefreshRepositories().Forget();
		}
	}
}
