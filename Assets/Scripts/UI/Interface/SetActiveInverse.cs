using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	/// <summary>
	/// MonoBehaviour that allows setting the active state to the inverse of a value.
	/// Useful for hiding sprites for "Off" states of button Toggles.
	/// </summary>
	public class SetActiveInverse : MonoBehaviour
	{
		[SerializeField, Tooltip("Optionally read a Toggle element to determine whether to appear off.")]
		private Toggle readToggleState;

		private void Start()
		{
			SetActiveInverted(readToggleState.isOn);
		}

		public void SetActiveInverted(bool isNotActive)
		{
			gameObject.SetActive(!isNotActive);
		}
	}
}
