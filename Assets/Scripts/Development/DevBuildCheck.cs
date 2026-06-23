using System.Collections;
using System.Collections.Generic;
using Fusion.Analyzer;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Development
{
	public class DevBuildCheck : MonoBehaviour
	{
		[SerializeField]
		UnityEvent<bool> onDevBuildCheck = new();

		[SerializeField]
		UnityEvent onIsDevBuild = new();

		private void Awake()
		{
			bool b = Debug.isDebugBuild;

			onDevBuildCheck.Invoke(b);
			if (b)
			{
				onIsDevBuild.Invoke();
			}
		}
	}
}
