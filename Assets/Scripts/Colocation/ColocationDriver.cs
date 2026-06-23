using System;
using System.Collections;
using System.Collections.Generic;
using CollabXR.Networking;
using CollabXR.Oculus;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Colocation
{
	public class ColocationDriver : SingletonNetworkBehavior<ColocationDriver>, IAfterSpawned
	{
		[SerializeField]
		private GameObject anchorPrefab;

		private GameObject currentAnchorGameObject;
		private OVRSpatialAnchor currentAnchor;

		//private bool IsColocatedLocally { get; set; }
		//public static event Action<Guid> OnAnchored = delegate { };
		//public static event Action<bool> OnCurrentAnchorSharable = delegate { };
		public static event Action OnCurrentAnchorDestroyed = delegate { };

		public static EventVariable<Guid> AnchorGuid = new();
		public static EventVariable<bool> IsAnchored = new();
		public static EventVariable<bool> CanShareAnchor = new();
		public static EventVariable<bool> IsAnchoredViaVirtualCamera = new();
		public static bool IsAnchoredViaCodeTempTodoRemoveThis = false;

		public UnityEvent<bool> notBusy;

		public Guid GetCurrentAnchorGuid()
		{
			return currentAnchor == null ? Guid.Empty : currentAnchor.Uuid;
		}

		protected override void Awake()
		{
			base.Awake();

			OnCurrentAnchorDestroyed = delegate { };
		}

		public void AfterSpawned()
		{
			TryColocateWithAnyone();
		}

		private void TryColocateWithAnyone()
		{
			foreach (PlayerRef playerRef in NetworkManager.Runner.ActivePlayers)
			{
				if (playerRef == NetworkManager.Runner.LocalPlayer)
					continue;

				NetworkPlayer rig = NetworkManager.Runner.GetPlayerObject(playerRef)?.GetComponent<NetworkPlayer>();

				if (rig != null && rig.CanShareAnchor)
				{
					Debug.Log("Found colocated player " + playerRef.PlayerId + ". Requesting anchor");
					Instance?.RPC_RequestAnchor(playerRef, PlatformSetup.OculusUser.ID);
					break;
				}
			}
		}

		private OVRSpatialAnchor CreateNewCurrentAnchor(Vector3 pos, Quaternion rot)
		{
			DestroyCurrentAnchor();

			IsAnchoredViaCodeTempTodoRemoveThis = false;

			currentAnchorGameObject = Instantiate(anchorPrefab, pos, rot);
			currentAnchor = currentAnchorGameObject.AddComponent<OVRSpatialAnchor>();

			return currentAnchor;
		}

		public GameObject Anchor(Vector3 pos, Quaternion rot, Action onSuccess = null)
		{
			notBusy.Invoke(false);

			Debug.Log("Creating anchor component...");
			CreateNewCurrentAnchor(pos, rot);
			StartCoroutine(WaitForInternalAnchorCreation(onSuccess));

			return currentAnchorGameObject;
		}

		public void DestroyCurrentAnchor()
		{
			if (currentAnchorGameObject != null)
				Destroy(currentAnchorGameObject);

			currentAnchorGameObject = null;
			currentAnchor = null;

			//IsColocatedLocally = false;

			AnchorGuid.Value = Guid.Empty;
			CanShareAnchor.Value = false;
			IsAnchored.Value = false;
			OnCurrentAnchorDestroyed.Invoke();
		}

		private IEnumerator WaitForInternalAnchorCreation(Action onSuccess = null)
		{
			notBusy.Invoke(false);

			while (!currentAnchor.Created)
				yield return new WaitForFixedUpdate();

			Pose targetUnityPose = new Pose(currentAnchor.transform.position, currentAnchor.transform.rotation);
			Pose originPose = new Pose(Vector3.zero, Quaternion.identity);

			HardwareRig.Instance.TransformPlayspace(targetUnityPose, originPose);

			yield return new WaitForEndOfFrame();
			Debug.Log("Anchor component created and initialized!");

			notBusy.Invoke(true);

			onSuccess?.Invoke();

			OnColocatedLocally();
		}

		public void UploadCurrentAnchor(Action onSuccess = null)
		{
			if (currentAnchor == null)
			{
				Debug.LogError("No anchor to upload!");
				return;
			}

			notBusy.Invoke(false);

			OVRSpatialAnchor.SaveOptions saveOptions = new() { Storage = OVRSpace.StorageLocation.Cloud };

			Debug.Log("Uploading anchor...");

			// we have to create a list of anchors and use the static function because the API is silly and this is the only way to get an OperationResult :)))))
			List<OVRSpatialAnchor> anchorList = new List<OVRSpatialAnchor> { currentAnchor };
			OVRSpatialAnchor
				.SaveAsync(anchorList, saveOptions)
				.ContinueWith(
					(uploadSuccess) =>
					{
						notBusy.Invoke(true);

						Debug.Log("Anchor upload: " + uploadSuccess.ToString());
						if (uploadSuccess == OVRSpatialAnchor.OperationResult.Success)
						{
							CanShareAnchor.Value = true;
							onSuccess?.Invoke();
						}
						else
						{
							Debug.Log("Upload failed, stopping here :(");
						}
					}
				);
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_RequestAnchor([RpcTarget] PlayerRef target, ulong askerID)
		{
			Debug.Log("Oculus player " + askerID + " requested anchor.");

			ShareCurrentAnchor(new[] { new OVRSpaceUser(askerID) });
		}

		public void ShareCurrentAnchorWithAll() => ShareCurrentAnchor(NetworkManager.GetActiveOculusIDs());

		public void ShareCurrentAnchor(OVRSpaceUser[] usersToShareWith)
		{
			if (!CanShareAnchor.Value)
			{
				Debug.LogError("No anchor to share or current anchor isn't sharable!");
				return;
			}

			Debug.Log("Sharing anchor...");
			List<OVRSpatialAnchor> anchorList = new List<OVRSpatialAnchor> { currentAnchor };

			OVRSpatialAnchor
				.ShareAsync(anchorList, usersToShareWith)
				.ContinueWith(shareResult =>
				{
					notBusy.Invoke(true);

					Debug.Log("Anchor share: " + shareResult.ToString());
					if (shareResult == OVRSpatialAnchor.OperationResult.Success)
					{
						RPC_AllDownloadAnchor(currentAnchor.Uuid);
					}
					else
					{
						Debug.Log("Failed to share, stopping here :(");
					}
				});
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_AllDownloadAnchor(Guid uuid)
		{
			DownloadAnchor(OVRSpace.StorageLocation.Cloud, uuid);
		}

		private void DownloadAnchor(OVRSpace.StorageLocation location, Guid uuid)
		{
			// if anchor already downloaded, don't bother
			if (currentAnchor != null && currentAnchor.Uuid == uuid)
				return;

			notBusy.Invoke(false);

			OVRSpatialAnchor.LoadOptions loadOptions = new()
			{
				Timeout = 0,
				StorageLocation = location,
				Uuids = new[] { uuid },
			};

			Debug.Log("Downloading anchor " + uuid);

			OVRSpatialAnchor
				.LoadUnboundAnchorsAsync(loadOptions)
				.ContinueWith(loadedAnchors =>
				{
					Debug.Log("OVRSpatialAnchor callback started.");

					if (loadedAnchors == null || loadedAnchors.Length == 0)
					{
						Debug.Log("OVRSpatialAnchor load failed.");
						notBusy.Invoke(true);
						return;
					}

					// grabbing first anchor
					OVRSpatialAnchor.UnboundAnchor anchor = loadedAnchors[0];

					anchor
						.LocalizeAsync(10)
						.ContinueWith(
							delegate(bool localizeSuccess)
							{
								notBusy.Invoke(true);
								if (!localizeSuccess)
								{
									Debug.Log("Downloaded anchor but couldn't localize!");
									return;
								}

								Pose pose = anchor.Pose;

								CreateNewCurrentAnchor(pose.position, pose.rotation);

								anchor.BindTo(currentAnchor);

								Pose targetUnityPose = new Pose(currentAnchor.transform.position, currentAnchor.transform.rotation);
								Pose originPose = new Pose(Vector3.zero, Quaternion.identity);

								HardwareRig.Instance.TransformPlayspace(targetUnityPose, originPose);

								OnColocatedLocally();
							}
						);
				});
		}

		private void OnColocatedLocally()
		{
			//IsColocatedLocally = true;

			// enable mixed reality
			// TODO: decouple from ColocationDriver
			PassthroughManager.PassthroughOn.Value = true;
			AnchorGuid.Value = currentAnchor.Uuid;
			IsAnchored.Value = true;
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_AllDestroyAnchor()
		{
			DestroyCurrentAnchor();
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			IsAnchoredViaCodeTempTodoRemoveThis = false;
			DestroyCurrentAnchor();
		}

		[Rpc(RpcSources.All, RpcTargets.All)]
		public void RPC_TogglePassthrough(bool state)
		{
			Debug.Log("Received passthrough request: " + state.ToString());
			PassthroughManager.PassthroughOn.Value = state;
		}
	}
}
