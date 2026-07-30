using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR
{
    public class AppIDScanner : InputFieldScanner
	{
		public string filename;
		public Toggle useCustomIDToggle;
		private string appIDFilePath;

		private void Awake()
		{
			appIDFilePath = Path.Combine(Application.persistentDataPath, filename + ".json");
			DeserializeCustomAppIDToggle();
		}

		private void OnEnable()
		{
			DeserializeAppID();
		}

		public void SerializeAppID()
		{
			string encodedID = Convert.ToBase64String(Encoding.UTF8.GetBytes(targetField.text));
			File.WriteAllText(appIDFilePath, encodedID);
		}

		public void DeserializeAppID()
		{
			string encodedID = File.ReadAllText(appIDFilePath);
			string decodedID = Encoding.UTF8.GetString(Convert.FromBase64String(encodedID));
			targetField.text = decodedID;
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
