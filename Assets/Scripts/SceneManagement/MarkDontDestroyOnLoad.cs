using UnityEngine;

namespace CollabXR.SceneManagement
{
	public class MarkDontDestroyOnLoad : MonoBehaviour
	{
		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}
	}
}
