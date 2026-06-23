using UnityEngine;

namespace CollabXR.Development
{
	public class DeactivateOnStart : MonoBehaviour
	{
		void Start()
		{
			gameObject.SetActive(false);
		}
	}
}
