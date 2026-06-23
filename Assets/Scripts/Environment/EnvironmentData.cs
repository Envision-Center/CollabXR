using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CollabXR.Environments
{
	[CreateAssetMenu(menuName = "CollabXR/Environment Data")]
	public class EnvironmentData : ScriptableObject
	{
		public string sceneName;

		//public LightingConfig customLightingConfig;
		public EnvironmentTeleportInfo[] teleportInfo;
		public EmbeddedNetworkObjects networkObjects;
	}

	[Serializable]
	public struct EnvironmentTeleport
	{
		public Transform transform;
		public EnvironmentTeleportInfo info;
	}

	[Serializable]
	public struct EnvironmentTeleportInfo
	{
		public string name;
		public Sprite thumbnail;
	}

	[Serializable]
	public struct EmbeddedNetworkObjects
	{
		public List<GameObject> objects;
	}
}
