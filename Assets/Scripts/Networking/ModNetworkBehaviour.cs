using System.Collections;
using System.Collections.Generic;
using CollabXR.Objects;
using Fusion;
using UnityEngine;

namespace CollabXR.Networking
{
	// ModNetworkBehaviour is a base network behaviour that waits for a mod to load
	public class ModNetworkBehaviour : NetworkBehaviour
	{
		protected CollabObject obj;

		protected virtual void CheckForScripts() { } // fired after prefab instantiates

		protected virtual void Awake()
		{
			obj = GetComponent<CollabObject>();
			obj.FinishedLoading.AddListener(CheckForScripts);
		}
	}
}
