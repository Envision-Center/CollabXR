using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollabXR.Development
{
	[CreateAssetMenu(fileName = "DeveloperPreferences", menuName = "CollabXR/DeveloperPreferences")]
	public class DeveloperPreferences : ScriptableObject
	{
		[HideInInspector, SerializeField]
		public string defaultRoom = "";
		public bool useCustomAppID;
		public string fusionAppID, voiceAppID;
		public List<string> repositoryURLs;

		public void GenerateNewRoomFixed()
		{
			defaultRoom = System.Environment.UserName;
		}

		public void GenerateNewRoomRandom()
		{
			defaultRoom = Random.Range(100000, 999999).ToString();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(DeveloperPreferences))]
	public class DeveloperPreferencesEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DeveloperPreferences prefs = (DeveloperPreferences)target;

			EditorGUILayout.LabelField("Developer Preferences", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			prefs.defaultRoom = EditorGUILayout.TextField("Default Room:", prefs.defaultRoom);
			if (EditorGUI.EndChangeCheck())
			{
				EditorUtility.SetDirty(target);
			}

			if (GUILayout.Button("QR Code", GUILayout.Height(20)))
			{
				System.Diagnostics.Process.Start($"https://api.qrserver.com/v1/create-qr-code/?size=1000x1000&data={prefs.defaultRoom}");
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Generate Fixed Room", GUILayout.Height(30)))
			{
				prefs.GenerateNewRoomFixed();
				EditorUtility.SetDirty(target);
				AssetDatabase.SaveAssets();
			}
			if (GUILayout.Button("Generate Random Room", GUILayout.Height(30)))
			{
				prefs.GenerateNewRoomRandom();
				EditorUtility.SetDirty(target);
				AssetDatabase.SaveAssets();
			}
			GUILayout.EndHorizontal();
		}
	}
#endif
}
