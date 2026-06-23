using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CIKeystoreAuth : IPreprocessBuildWithReport
{
	public int callbackOrder
	{
		get { return 0; }
	}

	public void OnPreprocessBuild(BuildReport report)
	{
		if (Application.isBatchMode)
		{
			string keystorePassword = Environment.GetEnvironmentVariable("COLLAB_KEYSTORE_PASSWORD");

			if (!string.IsNullOrEmpty(keystorePassword))
			{
				PlayerSettings.Android.keystoreName = "collab.keystore";
				PlayerSettings.Android.keystorePass = keystorePassword;

				PlayerSettings.Android.keyaliasName = "collabkey";
				PlayerSettings.Android.keyaliasPass = keystorePassword;

				Debug.Log("Loaded and Authenticated Android Keystore");
			}
			else
			{
				Debug.LogWarning("No Password Provided for Android Keystore!");
			}
		}
	}
}
