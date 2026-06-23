using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Mesh Option")]
	public class MeshOption : ScriptableObject
	{
		public List<Mesh> options; // mesh options for hair & head styles
	}
}
