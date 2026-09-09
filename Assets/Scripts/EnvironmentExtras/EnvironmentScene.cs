using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CollabXR.EnvironmentExtras
{
	[RequireComponent(typeof(EnvironmentPassthroughEvents))]
	public class EnvironmentScene : MonoBehaviour
	{
		public EnvironmentData environmentData;
		public EnvironmentTeleport[] teleports;
		public EnvironmentPassthroughEvents passthroughEvents;
		public bool skyboxOnInPassthrough;

		private Material sceneSkyboxMaterial;

		private void Awake()
		{
			passthroughEvents = GetComponent<EnvironmentPassthroughEvents>();
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
