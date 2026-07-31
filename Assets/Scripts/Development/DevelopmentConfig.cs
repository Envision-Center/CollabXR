using System.Collections.Generic;
using CollabXR.ModLoader;
using UnityEditor;
using UnityEngine;

namespace CollabXR.Development
{
	public class DevelopmentConfig : SingletonBehavior<DevelopmentConfig>
	{
		[SerializeField]
		private DeveloperPreferences prefs;

		[SerializeField]
		private BuildInformation buildInfo;

		public const string DeveloperPreferencesResource = "Preferences/DeveloperPreferences";
		public const string BuildInformationResource = "Preferences/BuildInformation";

		public const string DeveloperPreferencesTemplatePath = "Assets/Templates/DeveloperPreferences.asset";
		public const string BuildInformationTemplatePath = "Assets/Templates/BuildInformation.asset";

		public const string DeveloperPreferencesAbsolutePath = "Assets/Resources/Preferences/DeveloperPreferences.asset";
		public const string BuildInformationAbsolutePath = "Assets/Resources/Preferences/BuildInformation.asset";

		protected override void Awake()
		{
			base.Awake();
			AttemptLoadConfiguration();
		}

		public void AttemptLoadConfiguration()
		{
			prefs = AttemptLoadDeveloperPreferences();
			buildInfo = AttemptLoadBuildInformation();
		}

		public static DeveloperPreferences AttemptLoadDeveloperPreferences()
		{
			DeveloperPreferences asset = Resources.Load<DeveloperPreferences>(DeveloperPreferencesResource);
#if UNITY_EDITOR
			if (asset == null)
			{
				AssetDatabase.CopyAsset(DeveloperPreferencesTemplatePath, DeveloperPreferencesAbsolutePath);
				asset = Resources.Load<DeveloperPreferences>(DeveloperPreferencesResource);
			}

			foreach (string repo in asset.repositoryURLs)
			{
				RepositoryManager.AddRepository(repo, false);
			}
#endif
			return asset;
		}

		public static BuildInformation AttemptLoadBuildInformation()
		{
			BuildInformation asset = Resources.Load<BuildInformation>(BuildInformationResource);
#if UNITY_EDITOR
			if (asset == null)
			{
				AssetDatabase.CopyAsset(BuildInformationTemplatePath, BuildInformationAbsolutePath);
				asset = Resources.Load<BuildInformation>(BuildInformationResource);
			}
#endif
			return asset;
		}

		public DeveloperPreferences GetDeveloperPreferences()
		{
			return prefs;
		}

		public BuildInformation GetBuildInformation()
		{
			return buildInfo;
		}
	}
}
