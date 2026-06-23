using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Tools
{
	public class HandOverUIEvents : MonoBehaviour
	{
		private RigHand hand;

		public UnityEvent<bool> IsOverUIChange;
		public UnityEvent<bool> IsNotOverUIChange;

		public List<Behaviour> enableOverUI = new();
		public List<Behaviour> disableOverUI = new();

		private void Awake()
		{
			this.GetRigHandRef()?.Hand.AddListenerAndCheck(SetHand);
		}

		public void SetHand(RigHand hand)
		{
			this.hand?.OnHoverUI.RemoveListener(HandleHandOverUI);
			this.hand = hand;
			this.hand?.OnHoverUI.AddListener(HandleHandOverUI);
			CheckOverUI();
		}

		private void OnEnable()
		{
			CheckOverUI();
		}

		private void CheckOverUI()
		{
			bool handOverUI = hand != null && hand.TestIfOverUI();
			HandleHandOverUI(handOverUI);
		}

		private void HandleHandOverUI(bool isOverUI)
		{
			if (!HardwareConfig.IsVisionOS) // checking if the user is gazing UI on Vision OS is not possible until a click is detected
			{
				IsOverUIChange.Invoke(isOverUI);
				IsNotOverUIChange.Invoke(!isOverUI);

				foreach (var b in enableOverUI)
					b.enabled = isOverUI;

				foreach (var b in disableOverUI)
					b.enabled = !isOverUI;
			}
		}
	}
}
