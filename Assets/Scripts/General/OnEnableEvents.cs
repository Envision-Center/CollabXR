using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR
{
	public class OnEnableEvents : MonoBehaviour
	{
		public UnityEvent onEnable;
		public UnityEvent onDisable;
		public UnityEvent<bool> onEnableChange;

		private void OnEnable()
		{
			onEnable.Invoke();
			onEnableChange.Invoke(true);
		}

		private void OnDisable()
		{
			onDisable.Invoke();
			onEnableChange.Invoke(false);
		}
	}
}
