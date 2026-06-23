using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Customization/Prefab Option")]
	public class AccessoryOption : ScriptableObject
	{
		public List<AvatarAccessory> accessories;
	}

	[Serializable]
	public class AvatarAccessory
	{
		public GameObject prefab;
		public bool hideHairModel,
			hideHeadModel,
			hideBodyModel,
			hideHandsModel;
		public Vector3 offset;
	}
}
