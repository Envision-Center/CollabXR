using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CollabXR.ModLoader
{
	public class RepositoryList : MonoBehaviour
	{
		[SerializeField]
		private GameObject repositoryEntryPrefab;

		private Dictionary<string, RepositoryListEntry> listedRepositories = new();

		private float repositoryUpdateTimer;

		[SerializeField]
		private bool isDuringSession;

		void Update()
		{
			repositoryUpdateTimer += Time.deltaTime;

			foreach (string repoUrl in RepositoryManager.Instance.activeRepositories)
			{
				if (!listedRepositories.ContainsKey(repoUrl))
				{
					listedRepositories[repoUrl] = Instantiate(repositoryEntryPrefab, transform).GetComponent<RepositoryListEntry>();

					if (RepositoryManager.Instance.loadedRepositories.ContainsKey(repoUrl))
						listedRepositories[repoUrl].SetInfo(repoUrl, RepositoryManager.Instance.loadedRepositories[repoUrl]);
					else
						listedRepositories[repoUrl].SetInfo(repoUrl, null);

					if (isDuringSession)
					{
						listedRepositories[repoUrl].HideAllButtons();						
					}
				}
			}

			List<string> repoUrls = listedRepositories.Keys.ToList();

			// Only update every second
			if (repositoryUpdateTimer >= 1.0f)
			{
				foreach (string repoUrl in repoUrls)
				{
					if (!RepositoryManager.Instance.activeRepositories.Contains(repoUrl))
					{
						Destroy(listedRepositories[repoUrl].gameObject);

						listedRepositories.Remove(repoUrl);

						continue;
					}
					if (RepositoryManager.Instance.loadedRepositories.ContainsKey(repoUrl))
						listedRepositories[repoUrl].SetInfo(repoUrl, RepositoryManager.Instance.loadedRepositories[repoUrl]);
					else
						listedRepositories[repoUrl].SetInfo(repoUrl, null);
				}
				repositoryUpdateTimer = 0.0f;
			}
		}
	}
}
