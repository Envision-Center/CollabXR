using UnityEngine;
using UnityEngine.Rendering;

namespace CollabXR
{
	[ExecuteInEditMode]
	public class LightingConfigActivator : MonoBehaviour
	{
		public LightingConfig lightingConfig;

		public bool activateOnEnable = true;

		public void Activate() => lightingConfig?.Activate();

		private void Start()
		{
			TryActivate();
		}

		private void OnEnable()
		{
			TryActivate();
		}

		private void TryActivate()
		{
			if (activateOnEnable)
				Activate();
		}
	}
}
