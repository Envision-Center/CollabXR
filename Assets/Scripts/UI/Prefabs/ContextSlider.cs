using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI.Prefabs
{
	public class ContextSlider : MonoBehaviour
	{
		[Header("Prefab")]
#if UNITY_EDITOR
		[SerializeField]
		private TMP_Text label;

		[SerializeField]
		private Slider slider;
#endif

		[SerializeField]
		private TMP_Text readout;

		[SerializeField]
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
