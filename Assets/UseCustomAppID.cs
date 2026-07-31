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
			int isOnPref = Convert.ToInt32(isOn);
			PlayerPrefs.SetInt("custom_app_id", isOnPref);
			Debug.Log($"serializing custom_app_id={isOn}, pref={isOnPref}");
		}

		public void DeserializeCustomAppIDToggle()
		{
			int isOnPref = PlayerPrefs.GetInt("custom_app_id");
			useCustomIDToggle.isOn = Convert.ToBoolean(isOnPref);
			Debug.Log($"deserialized custom_app_id={useCustomIDToggle.isOn}, pref={isOnPref}");
		}
	}
}
