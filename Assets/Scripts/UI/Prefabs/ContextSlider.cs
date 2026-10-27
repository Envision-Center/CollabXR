using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI.Prefabs
{
	public class ContextSlider : MonoBehaviour
	{
		[Header("Prefab")]
#if UNITY_EDITOR
		[SerializeField, Tooltip("Label to use for displaying name on slider.")]
		private TMP_Text label;

		[SerializeField, Tooltip("Slider to bind to.")]
		private Slider slider;
#endif

		[SerializeField, Tooltip("Label to use for number display.")]
		private TMP_Text readout;

		[SerializeField, Tooltip("Number formatting for display.")]
		private string stringFormat = "0.00u";

		public void UpdateReadout(float newValue)
		{
			readout.text = newValue.ToString(stringFormat);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (label != null)
			{
				label.text = name;
			}

			float value = 0.5f;
			if (slider != null)
			{
				value = slider.value;
			}

			if (readout != null)
			{
				UpdateReadout(value);
			}
		}
#endif
	}
}
