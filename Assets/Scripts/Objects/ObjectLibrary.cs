using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CollabXR.Objects
{
	[CreateAssetMenu(menuName = "CollabXR/Builtin Object Library")]
	public class BuiltinObjectLibrary : ScriptableObject
	{
		public ObjectDictionary dictionary;
	}

	[Serializable]
	public class ObjectDictionary
	{
		public ObjectDictionary()
		{
			categories = new List<ObjectCategory>();
		}

		public List<ObjectCategory> categories;
	}

	[Serializable]
	public class ObjectCategory
	{
		public ObjectCategory()
		{
			objectData = new List<CollabObjectData>();
		}

		public string name;
		public List<CollabObjectData> objectData;
	}

#if UNITY_EDITOR
	public class DataWrangler : EditorWindow
	{
		public BuiltinObjectLibrary library;

		private void OnGUI()
		{
			// title
			GUILayout.BeginArea(new Rect(0, 0, position.width, 20));
			EditorGUILayout.LabelField("Data Wrangler", EditorStyles.boldLabel);
			GUILayout.EndArea();
			GUILayout.BeginArea(new Rect(5, 25, position.width - 5, position.height));
			library = (BuiltinObjectLibrary)EditorGUILayout.ObjectField("Collab Library", library, typeof(BuiltinObjectLibrary), true);
			if (GUILayout.Button("Build Collab Library"))
				BuildCollabLibrary();
			GUILayout.EndArea();
		}

		[MenuItem("CollabXR/Data Wrangler")]
		public static void ShowWindow()
		{
			GetWindow(typeof(DataWrangler), false, "Data Wrangler");
		}

		public void BigScreenshot()
		{
			ScreenCapture.CaptureScreenshot("Screenshots/big.png", 5);
		}

		public void BuildCollabLibrary()
		{
			library.dictionary.categories = new();

			string[] categoryFolderPaths = AssetDatabase.GetSubFolders("Assets/Data");

			foreach (string categoryFolderPath in categoryFolderPaths)
			{
				// create category
				ObjectCategory objectCategory = new();
				objectCategory.name = Path.GetFileName(categoryFolderPath);
				objectCategory.objectData = new List<CollabObjectData>();

				// iterate through assets within category
				string[] dataFileNames = AssetDatabase.FindAssets("t:" + typeof(CollabObjectData), new[] { categoryFolderPath });

				foreach (string dataFilePath in dataFileNames)
				{
					CollabObjectData objectData = BuildData(dataFilePath);
					objectCategory.objectData.Add(objectData);
					EditorUtility.SetDirty(objectData);
				}

				if (objectCategory.objectData.Count > 0)
					library.dictionary.categories.Add(objectCategory);
			}

			EditorUtility.SetDirty(library);
			AssetDatabase.SaveAssets();
			Debug.Log("Generated and saved data library.");
		}

		public CollabObjectData BuildData(string file)
		{
			string fullPath = AssetDatabase.GUIDToAssetPath(file);
			string fullDirectory = Path.GetDirectoryName(fullPath);
			string[] folderList = fullDirectory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string parentFolder = folderList[folderList.Length - 1];
			string asset = Path.GetFileName(fullPath);
			CollabObjectData loadedThing = AssetDatabase.LoadAssetAtPath<CollabObjectData>(fullPath);
			loadedThing.category = parentFolder;
			loadedThing.assetName = asset;
			// Debug.Log(fullPath + " found " + loadedThing.assetFolder + "/" + loadedThing.assetName);
			return loadedThing;
		}

		// public void OnPreprocessBuild(BuildReport report)
		// {
		//     BuildCollabLibrary();
		// }
	}
#endif
}
