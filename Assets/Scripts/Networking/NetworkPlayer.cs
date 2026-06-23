using System;
using System.Collections.Generic;
using CollabXR.Avatar;
using CollabXR.Colocation;
using CollabXR.Oculus;
using CollabXR.Scriptables;
using CollabXR.VR;
using Cysharp.Threading.Tasks;
using Fusion;
using Oculus.Platform.Models;
using TMPro;
using UnityEngine;

namespace CollabXR.Networking
{
	public class NetworkPlayer : NetworkBehaviour
	{
		public enum NetworkPlayerRole
		{
			Student = 0,
			Admin = 1,
		}

		[SerializeField]
		private PlayerCustomizationPrefs prefs;

		[SerializeField]
		private ScriptableInt role;

		public Transform head,
			leftHand,
			rightHand;

		[SerializeField]
		private List<MeshRenderer> localInvisibleMeshes;

		//[SerializeField] private List<Canvas> nametagCanvas;

		[SerializeField]
		private AvatarRig avatarRig;

		[SerializeField]
		private Transform usernameDisplayTransform;

		[SerializeField]
		private TMP_Text usernameTextDisplay;

		[SerializeField]
		private float networkLerp = 0.3f;

		[Networked]
		public bool muted { get; set; }

		[Networked]
		public bool deafened { get; set; }

		[Networked]
		public RigState State { get; set; }

		[Networked, OnChangedRender(nameof(AvatarChange))]
		public AvatarCustomizationData Avatar { get; set; }

		[Networked, OnChangedRender(nameof(UpdateDisplayName))]
		public NetworkString<_16> Name { get; set; }

		[Networked]
		public NetworkPlayerRole Role { get; set; }

		[Networked]
		public ulong OculusID { get; set; }

		[Networked]
		public string DeviceID { get; set; }

		[Networked]
		public string RuntimePlatform { get; set; }

		// TODO: make this its own event and use tag-specific index codes
		[Networked, OnChangedRender(nameof(ColocatedAnchorUuidChange))]
		public bool IsColocatedViaCode { get; set; }

		[Networked, OnChangedRender(nameof(ColocatedAnchorUuidChange))]
		public Guid ColocatedAnchorUuid { get; private set; }

		private void SetColocatedAnchorUuid(Guid anchorGuid) => ColocatedAnchorUuid = anchorGuid;

		[Networked]
		public NetworkBool CanShareAnchor { get; private set; } = false;

		private void SetCanShareAnchor(bool b) => CanShareAnchor = b;

		public event Action<Guid> OnColocated = delegate { };

		public void ColocatedAnchorUuidChange()
		{
			OnColocated.Invoke(ColocatedAnchorUuid);
		}

		public void AvatarChange()
		{
			avatarRig.SetData(Avatar);
		}

		public void UpdateDisplayName()
		{
			usernameTextDisplay.SetText(Name.Value);
		}

		public bool IsLocalNetworkRig => Object.HasStateAuthority;

		public static NetworkPlayer LocalPlayer;

		private void Awake()
		{
			DontDestroyOnLoad(gameObject);
		}

		public void Update()
		{
			if (IsLocalNetworkRig)
				LocalUserUpdate();
			else
				NetworkUserUpdate();
		}

		public override void Spawned()
		{
			base.Spawned();

			if (IsLocalNetworkRig)
			{
				LocalPlayer = this;

				RuntimePlatform = Application.platform.ToString();
				DeviceID = SystemInfo.deviceUniqueIdentifier;
				Avatar = prefs.BuiltAvatar;

				UpdateNameFromPrefs();

				Role = (NetworkPlayerRole)role.Value;

				SetOculusUser(PlatformSetup.OculusUser);

				ColocationDriver.AnchorGuid.AddListenerAndCheck(SetColocatedAnchorUuid);
				ColocationDriver.CanShareAnchor.AddListenerAndCheck(SetCanShareAnchor);
				IsColocatedViaCode = ColocationDriver.IsAnchoredViaCodeTempTodoRemoveThis || ColocationDriver.IsAnchoredViaVirtualCamera.Value;
			}

			if (IsLocalNetworkRig)
			{
				foreach (MeshRenderer mesh in localInvisibleMeshes)
					mesh.enabled = false;
				avatarRig.SetAccessoryVisibility(false);

				usernameDisplayTransform.gameObject.SetActive(false);
			}

			Debug.Log("Spawned new player.");
			avatarRig.SetData(Avatar);
			usernameTextDisplay.SetText(Name.Value);
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			ColocationDriver.AnchorGuid.RemoveListener(SetColocatedAnchorUuid);
			ColocationDriver.CanShareAnchor.RemoveListener(SetCanShareAnchor);
		}

		public void LocalUserUpdate()
		{
			State = HardwareRig.Instance.GetRigState();
			SyncRig(true);
		}

		public void NetworkUserUpdate()
		{
			SyncRig(false);
		}

		public void SyncRig(bool isLocal)
		{
			float lerpVal = isLocal ? 1.0f : networkLerp;
			head.position = Vector3.Lerp(head.position, State.headPosition, lerpVal);
			head.rotation = Quaternion.Lerp(head.rotation, State.headRotation, lerpVal);
			leftHand.position = Vector3.Lerp(leftHand.position, State.leftHandPosition, lerpVal);
			leftHand.rotation = Quaternion.Lerp(leftHand.rotation, State.leftHandRotation, lerpVal);
			rightHand.position = Vector3.Lerp(rightHand.position, State.rightHandPosition, lerpVal);
			rightHand.rotation = Quaternion.Lerp(rightHand.rotation, State.rightHandRotation, lerpVal);
		}

		public RigState GetState()
		{
			return State;
		}

		public void SetOculusUser(User user)
		{
			OculusID = user != null ? user.ID : 0;
		}

		public void SetName(string newName)
		{
			Name = newName;
			if (Name.Length == 0)
				Name = "User";
		}

		public void UpdateNameFromPrefs()
		{
			SetName(prefs.Username);
		}

		public static NetworkPlayerRole GetLocalRole()
		{
			if (LocalPlayer == null || !LocalPlayer.Object.IsValid)
				return NetworkPlayerRole.Student;
			else
				return LocalPlayer.Role;
		}

		public void OnLocalMuteStatusChanged(bool muted)
		{
			if (Object.IsValid)
			{
				NetworkManager.Instance.OnLocalMuteStatusChanged(muted);
				this.muted = muted;
			}
		}

		public void OnLocalDeafenStatusChanged(bool deafened)
		{
			if (Object.IsValid)
			{
				this.deafened = deafened;
				NetworkManager.Instance.OnLocalDeafenStatusChanged(deafened);
			}
		}
	}
}
