using UnityEngine;

namespace CollabXR.Scriptables
{
	[CreateAssetMenu(fileName = "Int", menuName = "Scriptable Variables/Int")]
	public class ScriptableInt : GenericScriptableVariable<int>
	{
		public override int GetPlayerPref()
		{
			return PlayerPrefs.GetInt(playerPrefName);
		}

		public override void SetPlayerPref(int t)
		{
			PlayerPrefs.SetInt(playerPrefName, t);
		}
	}
}
