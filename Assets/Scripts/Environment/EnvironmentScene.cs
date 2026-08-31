using CollabXR.VR;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollabXR.Environments
{
	[RequireComponent(typeof(PassthroughEvents))]
	public class EnvironmentScene : MonoBehaviour
	{
		public EnvironmentData environmentData;
		public EnvironmentTeleport[] teleports;
		public bool skyboxOnInPassthrough;

		private Material sceneSkyboxMaterial;

		private void Awake()
		{
			PassthroughManager.Instance.SetSkyboxOnInPassthrough(skyboxOnInPassthrough);
			LoadNetworkObjects();
		}

		private void OnDestroy()
		{
			PassthroughManager.Instance.SetSkyboxOnInPassthrough(false);
		}

		private void LoadNetworkObjects()
		{
			if (environmentData.networkObjects.objects.Count > 0)
			{
				EnvironmentManager.Instance.SpawnNetworkedObjects(environmentData.networkObjects.objects);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (environmentData == null)
				return;

			environmentData.teleportInfo = new EnvironmentTeleportInfo[teleports.Length];

			for (int i = 0; i < teleports.Length; i++)
			{
				environmentData.teleportInfo[i] = teleports[i].info;
			}

			EditorUtility.SetDirty(environmentData);
		}
#endif
	}
}
