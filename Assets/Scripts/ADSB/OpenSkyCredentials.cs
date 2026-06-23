using UnityEngine;

namespace CollabXR.ADSB
{
	[CreateAssetMenu(fileName = "OpenSkyCredentials", menuName = "CollabXR/OpenSky Credentials")]
	public class OpenSkyCredentials : ScriptableObject
	{
		[SerializeField]
		public string openSkyClientId;

		[SerializeField]
		public string openSkyClientSecret;
	}
}
