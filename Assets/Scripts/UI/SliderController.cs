using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class SliderController : MonoBehaviour
	{
		public Slider slider;
		public float slideSpeed = 1f;

		// Start is called before the first frame update
		void Start() { }

		// Update is called once per frame
		void Update() { }

		public void Slide(float velocity)
		{
			slider.value += velocity * Time.deltaTime * slideSpeed;
		}

		public void SetValue(float value)
		{
			slider.value = value;
		}
	}
}
