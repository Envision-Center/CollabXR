using System;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Tools
{
	public class FlipForHand : MonoBehaviour
	{
		private RigHandRef handRef;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
		}

		private void Start()
		{
			if (handRef.Hand.Value.isRight)
			{
				transform.localScale = new Vector3(-1, 1, 1);
			}
		}
	}
}
