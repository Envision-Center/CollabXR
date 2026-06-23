using System.Collections;
using System.Collections.Generic;
using CollabXR.Networking;
using TMPro;
using UnityEngine;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Avatar
{
	public class UsernameField : MonoBehaviour
	{
		public TMP_InputField field;

		public void OnEndEdit()
		{
			if (NetworkPlayer.LocalPlayer != null)
			{
				NetworkPlayer.LocalPlayer.UpdateNameFromPrefs();
			}
		}

		public void SetField(string name)
		{
			field.text = name;
		}
	}
}
