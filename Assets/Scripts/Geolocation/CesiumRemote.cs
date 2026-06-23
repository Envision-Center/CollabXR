using Fusion;
using UnityEngine;

namespace CollabXR.Geolocation
{
	public class CesiumRemote : NetworkBehaviour
	{
		private CesiumMapController PairedController;

		private void Awake()
		{
			TryPairWithController();
		}

		private void TryPairWithController()
		{
			PairedController = GameObject.FindFirstObjectByType<CesiumMapController>();
		}

		private void UnpairController()
		{
			PairedController = null;
		}
	}
}
