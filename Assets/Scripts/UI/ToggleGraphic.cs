using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR
{
	public class ToggleGraphic : MonoBehaviour
	{
		[SerializeField]
		private Sprite off;

		[SerializeField]
		private Sprite on;

		private Image image;
		private Toggle toggle;
		private bool isOn;

		private void Awake()
		{
			image = GetComponent<Image>();
			toggle = GetComponentInParent<Toggle>();
		}

		private void OnEnable()
		{
			UpdateSprite();
		}

		// unity sucks. Can't get toggle events fired by setOnWithoutNotify.
		// need to constantly poll value :(
		private void Update()
		{
			if (isOn == toggle.isOn)
				return;

			UpdateSprite();
		}

		private void UpdateSprite()
		{
			isOn = toggle.isOn;
			image.sprite = toggle.isOn ? on : off;
		}
	}
}
