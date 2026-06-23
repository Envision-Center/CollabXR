using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Customization/Texture Option")]
	public class TextureOption : ScriptableObject
	{
		public List<Texture> textures;
	}
}
