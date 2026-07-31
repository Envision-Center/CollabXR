using System;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR
{
    public class UseCustomAppID : MonoBehaviour
	{
		public Toggle useCustomIDToggle;

		private void OnEnable()
		{
			DeserializeCustomAppIDToggle();
		}

		public void SerializeCustomAppIDToggle(bool isOn)
		{
			PlayerPrefs.SetInt("custom_app_id", Convert.ToInt32(isOn));
		}

		public void DeserializeCustomAppIDToggle()
		{
			useCustomIDToggle.SetIsOnWithoutNotify(Convert.ToBoolean(PlayerPrefs.GetInt("custom_app_id")));
		}
	}
}
