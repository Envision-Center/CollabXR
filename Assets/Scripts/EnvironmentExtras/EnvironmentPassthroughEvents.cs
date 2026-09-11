using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.EnvironmentExtras
{
	public class EnvironmentPassthroughEvents : MonoBehaviour
	{
		public List<GameObject> disabledInPassthrough;
		public List<GameObject> enabledInPassthrough;

		public UnityEvent<bool> onPassthroughChange;
		public UnityEvent onPassthroughEnabled;
		public UnityEvent onPassthroughDisabled;

		private void OnEnable()
		{
			// PassthroughManager.PassthroughOn.AddListenerAndCheck(HandlePassthroughChange);
		}

		private void OnDisable()
		{
			// PassthroughManager.PassthroughOn.RemoveListener(HandlePassthroughChange);
		}

		public void HandlePassthroughChange(bool passthroughEnabled)
		{
			onPassthroughChange.Invoke(passthroughEnabled);

			if (passthroughEnabled)
				onPassthroughEnabled.Invoke();
			else
				onPassthroughDisabled.Invoke();

			foreach (GameObject g in disabledInPassthrough)
				g.SetActive(!passthroughEnabled);

			foreach (GameObject g in enabledInPassthrough)
				g.SetActive(passthroughEnabled);
		}
	}
}
