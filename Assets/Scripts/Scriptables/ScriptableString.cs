using UnityEngine;

namespace CollabXR.Scriptables
{
	[CreateAssetMenu(fileName = "String", menuName = "Scriptable Variables/String")]
	public class ScriptableString : GenericScriptableVariable<string>
	{
		public override string GetPlayerPref()
		{
			return PlayerPrefs.GetString(playerPrefName);
		}

		public override void SetPlayerPref(string t)
		{
			PlayerPrefs.SetString(playerPrefName, t);
		}
	}
}
