using UnityEngine;

namespace CollabXR.SceneManagement
{
	public static class GlobalObjectLoader
	{
		private const string objectName = "Global Objects";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void OnFirstSceneLoad()
		{
			Object globalObject = Object.Instantiate(Resources.Load(objectName));
			Object.DontDestroyOnLoad(globalObject);
		}
	}
}
