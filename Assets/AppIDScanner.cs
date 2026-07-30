using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace CollabXR
{
    public class AppIDScanner : InputFieldScanner
	{
		public string filename;
		private string appIDFilePath;

		private void Awake()
		{
			appIDFilePath = Path.Combine(Application.persistentDataPath, filename + ".json");
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
	}
}
