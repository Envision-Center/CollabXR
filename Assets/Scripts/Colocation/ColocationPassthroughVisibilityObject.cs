using System;
using CollabXR.VR;
using Fusion;
using UnityEngine;

namespace CollabXR.Colocation
{
	public class ColocationPassthroughVisibilityObject : NetworkBehaviour
	{
		[SerializeField]
		private Networking.NetworkPlayer networkRig;

		[SerializeField]
		private GameObject[] objectsVisibleInColocatedPassthrough;

		[SerializeField]
		private GameObject[] objectsInvisibleInColocatedPassthrough;

		[SerializeField]
		private GameObject[] objectsUsedAsColocationMask;

		public override void Spawned()
		{
			UpdateVisibility();

			PassthroughManager.PassthroughOn.AddListener(OnTogglePassthrough);
			PassthroughManager.OcclusionMethod.AddListener(OnOcclusionMethodChange);
			ColocationDriver.IsAnchored.AddListener(OnLocalColocate);
			ColocationDriver.IsAnchoredViaVirtualCamera.AddListener(OnLocalColocate);
			networkRig.OnColocated += OnLocalColocationChange;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			PassthroughManager.PassthroughOn.RemoveListener(OnTogglePassthrough);
			PassthroughManager.OcclusionMethod.RemoveListener(OnOcclusionMethodChange);
			ColocationDriver.IsAnchored.RemoveListener(OnLocalColocate);
			networkRig.OnColocated -= OnLocalColocationChange;
		}

		private void OnTogglePassthrough(bool passthroughOn) => UpdateVisibility();

		private void OnOcclusionMethodChange(OcclusionMethods method) => UpdateVisibility();

		private void OnLocalColocate(bool colocated) => UpdateVisibility();

		private void OnLocalColocationChange(Guid anchorUuid) => UpdateVisibility();

		private void UpdateVisibility()
		{
			Guid currentAnchorGuid = ColocationDriver.Instance == null ? Guid.Empty : ColocationDriver.Instance.GetCurrentAnchorGuid();
			bool colocatedLocally = networkRig.ColocatedAnchorUuid != Guid.Empty && networkRig.ColocatedAnchorUuid == currentAnchorGuid;
			bool colocatedViaSameCode = ColocationDriver.IsAnchoredViaCodeTempTodoRemoveThis && networkRig.IsColocatedViaCode;
			bool colocatedViaVirtualCamera = ColocationDriver.IsAnchoredViaVirtualCamera.Value && networkRig.IsColocatedViaCode; // todo: don't assume cameras and codes are always together

			bool validColocationPassthrough = (colocatedLocally || colocatedViaSameCode || colocatedViaVirtualCamera) && PassthroughManager.PassthroughOn.Value;
			bool objectShouldBeVisible = !validColocationPassthrough;
			Debug.Log($"passthrough debug: {validColocationPassthrough} {colocatedViaVirtualCamera} {networkRig.IsColocatedViaCode}");

			foreach (GameObject g in objectsInvisibleInColocatedPassthrough)
			{
				g.SetActive(objectShouldBeVisible);
			}

			foreach (GameObject g in objectsVisibleInColocatedPassthrough)
			{
				g.SetActive(!objectShouldBeVisible);
			}

			foreach (GameObject g in objectsUsedAsColocationMask)
			{
				g.SetActive(!objectShouldBeVisible && PassthroughManager.OcclusionMethod.Value == OcclusionMethods.Basic);
			}
		}
	}
}
