using System;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Tools
{
	public class DontFlip : MonoBehaviour
	{
		private void Start()
		{
			transform.localScale = new Vector3(transform.localScale.x * Mathf.Sign(transform.lossyScale.x), transform.localScale.y, transform.localScale.z);
		}
	}
}
