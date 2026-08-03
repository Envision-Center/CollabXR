using UnityEngine;

namespace CollabXR.Scriptables
{
	[CreateAssetMenu(fileName = "Bool", menuName = "Scriptable Variables/Bool")]
	public class ScriptableBool : GenericScriptableVariable<bool>
	{
		public override bool GetPlayerPref()
		{
			return PlayerPrefs.GetInt(playerPrefName, 0) == 1;
		}

		public override void SetPlayerPref(bool t)
		{
			PlayerPrefs.SetInt(playerPrefName, t ? 1 : 0);
		}
	}
}
