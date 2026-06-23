using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.ModLoader
{
	public class RepositoryListEntry : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI repositoryName;

		[SerializeField]
		private TextMeshProUGUI repositoryUrl;

		[SerializeField]
		private Button deleteButton;

		[SerializeField]
		private Button refreshButton;
		private string repoUrl;
		private RepositoryMetadata repositoryMetadata;

		private void Awake()
		{
			deleteButton.onClick.AddListener(DeleteClick);
			refreshButton.onClick.AddListener(RefreshClick);
		}

		private void DeleteClick()
		{
			RepositoryManager.RemoveRepository(repoUrl);
		}

		private void RefreshClick()
		{
			RepositoryManager.RefreshRepository(repoUrl);
		}

		public void SetInfo(string url, RepositoryMetadata metadata)
		{
			repoUrl = url;
			repositoryMetadata = metadata;

			repositoryUrl.text = url.Substring(0, 15) + "...";

			if (metadata == null)
			{
				repositoryName.text = "Loading...";
			}
			else
			{
				repositoryName.text = repositoryMetadata.RepoName;
			}
		}
	}
}
