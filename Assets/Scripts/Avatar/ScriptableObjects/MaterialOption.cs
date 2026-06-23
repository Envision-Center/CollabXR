using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Customization/Material Option")]
	public class MaterialOption : ScriptableObject
	{
		public Material baseMaterial; // base material to use for skin, hair, shirt, etc.
	}
}
