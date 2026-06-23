using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Color Option")]
	public class ColorOption : ScriptableObject
	{
		public List<Color> colors;
	}
}
