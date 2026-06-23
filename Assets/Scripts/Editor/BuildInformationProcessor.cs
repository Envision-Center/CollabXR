using System;
using CollabXR.Development;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CollabXR
{
	public class BuildInformationProcessor : IPreprocessBuildWithReport
	{
		public int callbackOrder
		{
			get { return 0; }
		}

		public void OnPreprocessBuild(BuildReport report)
		{
			BuildInformation asset = DevelopmentConfig.AttemptLoadBuildInformation();
			asset?.SerializeBeforeBuild();
			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
		}
	}
}
