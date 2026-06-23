using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Development
{
	public class DeactivateIfNotDevBuild : MonoBehaviour
	{
		private void Start()
		{
			gameObject.SetActive(Debug.isDebugBuild);
		}
	}
}
