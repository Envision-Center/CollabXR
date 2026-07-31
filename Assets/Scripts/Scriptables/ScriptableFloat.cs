using UnityEngine;

namespace CollabXR.Scriptables
{
	[CreateAssetMenu(fileName = "Float", menuName = "Scriptable Variables/Float")]
	public class ScriptableFloat : GenericScriptableVariable<float>
	{
		public override float GetPlayerPref()
		{
			return PlayerPrefs.GetFloat(playerPrefName);
		}

		public override void SetPlayerPref(float t)
		{
			PlayerPrefs.SetFloat(playerPrefName, t);
		}
	}
}
