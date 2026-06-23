using System.Collections;
using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.UI
{
	public class DeactivateOnUIHover : MonoBehaviour
	{
		private RigHandRef handRef;
		public bool activateOnUIHover = false;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
			handRef.Hand.Value.OnHoverUI.AddListener(OnUIHover);
		}

		private void OnDestroy()
		{
			handRef.Hand.Value.OnHoverUI.RemoveListener(OnUIHover);
		}

		private void OnUIHover(bool isOverUI)
		{
			gameObject.SetActive(isOverUI == activateOnUIHover);
		}
	}
}
