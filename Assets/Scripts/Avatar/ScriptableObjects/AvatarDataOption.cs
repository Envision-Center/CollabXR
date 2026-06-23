using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Picker Settings")]
	public class AvatarDataOption : ScriptableObject
	{
		public Color selected,
			unselected; // color of bg when selected
		public List<Color> colors; // color options
		public List<Sprite> sprites; // pre-rendered mesh options
		public float borderSize = 0.05f;
		public int defaultIndex;
	}
}
