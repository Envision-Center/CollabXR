using System;
using UnityEngine;

namespace CollabXR
{
	[CreateAssetMenu(fileName = "BuildInformation", menuName = "CollabXR/BuildInformation")]
	public class BuildInformation : ScriptableObject
	{
		[SerializeField]
		private string stringGuid;

		[SerializeField]
		private string buildTime;

		private const int shortenedDisplayLength = 8;

		public void SerializeBeforeBuild()
		{
			stringGuid = Guid.NewGuid().ToString();
			buildTime = DateTime.Now.ToString("MM/dd/yy HH:mm");
			Debug.Log($"Starting build with new guid = {stringGuid} at {buildTime}");
		}

		public string ShortenedGuid()
		{
			return stringGuid.Substring(0, Math.Min(shortenedDisplayLength, stringGuid.Length));
		}
	}
}
