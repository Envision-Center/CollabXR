using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollabXR
{
	[CreateAssetMenu(fileName = "Lighting Config", menuName = "CollabXR/Lighting Config")]
	public class LightingConfig : ScriptableObject
	{
		public Material skyboxMat;
		public SphericalHarmonicsL2 lightingHarmonics;
		public Texture reflectionTexture;

		public void Activate()
		{
			if (skyboxMat != null)
				RenderSettings.skybox = skyboxMat;

			if (!lightingHarmonics.Equals(default))
				RenderSettings.ambientProbe = lightingHarmonics;

			if (reflectionTexture != null)
			{
				RenderSettings.customReflectionTexture = reflectionTexture;
				RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
			}
		}

#if UNITY_EDITOR

		[MenuItem("CollabXR/Create Lighting Config from Current Scene")]
		private static void CreateLightingConfig()
		{
			LightingConfig config = CreateInstance<LightingConfig>();
			config.lightingHarmonics = RenderSettings.ambientProbe;
			config.reflectionTexture = ReflectionProbe.defaultTexture;

			string path = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (path == "")
			{
				path = "Assets";
			}
			else if (Path.GetExtension(path) != "")
			{
				path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
			}

			path += string.Format("/{1} Light Config.asset", path, SceneManager.GetActiveScene().name);

			AssetDatabase.CreateAsset(config, path);
		}

#endif
	}
}
