#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;

namespace CollabXR.Scenes
{
	public abstract class SceneMenu
	{
		[MenuItem("CollabXR/Main Scene", priority = 1)]
		private static void Menu()
		{
			EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
		}
	}
}

#endif
