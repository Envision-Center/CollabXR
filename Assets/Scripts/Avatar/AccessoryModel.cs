using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Avatar
{
	public class AccessoryModel : MonoBehaviour
	{
		public Material baseMat;
		public List<MeshRenderer> meshesToColorMatch;

		// Start is called before the first frame update
		public void SetColor(Color c)
		{
			foreach (MeshRenderer renderer in meshesToColorMatch)
			{
				renderer.materials[0].color = c;
			}
		}
	}
}
