using System;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.Objects.Components;
using CollabXR.Tools;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace CollabXR
{
	/// <summary>
	/// Networked <see cref="IPhysicsBody"/> for a CollabObject. When the mod loads, this adapter takes over the Rigidbody on the
	/// mod's root, routes physics commands to the state authority, and handles holding and throwing
	/// </summary>
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(20)] // After TransformFollow and NetworkGrabbable move the held object in LateUpdate
	[RequireComponent(typeof(NetworkRigidbody3D))]
	public class NetworkRigidbodyAdapter : ModNetworkBehaviour, IPhysicsBody, IStateAuthorityChanged
	{
		[SerializeField]
		[Tooltip("Number of recent frames averaged to get the velocity an object is thrown with.")]
		private int throwSampleFrames = 5;

		[SerializeField]
		[Tooltip("Minimum impact speed for this body to take authority over an unheld body it hits.")]
		private float contactAuthoritySpeed = 0.1f;

		/// <summary>
		/// Replicated separately because NetworkRigidbody3D does not restore gravity when authority changes hands
		/// </summary>
		[Networked]
		public NetworkBool NetworkedUseGravity { get; set; }

		[Networked]
		public NetworkBool NetworkedIsHeld { get; set; }

		public event Action<Collision> CollisionEntered = delegate { };

		/// <summary>
		/// True once the mod's Rigidbody has been taken over and its settings applied
		/// </summary>
		public bool IsReady { get; private set; }

		public bool HasAuthority => Object != null && Object.IsValid && Object.HasStateAuthority;

		public bool IsHeld => IsReady && Object != null && Object.IsValid && NetworkedIsHeld;

		private Rigidbody body;
		private NetworkGrabbable grabbable;
		private NetworkScalable scalable;
		private ThrowVelocityEstimator throwEstimator;
		private PhysicsScaleMode scaleMode;
		private bool authoredIsKinematic;
		private bool heldLocally;
		private float lastVolume;

		public float Mass
		{
			get => body.mass;
			set
			{
				if (HasAuthority)
					body.mass = value;
				else
					RPC_SetMass(value);
			}
		}

		public float LinearDamping
		{
			get => body.linearDamping;
			set
			{
				if (HasAuthority)
					body.linearDamping = value;
				else
					RPC_SetLinearDamping(value);
			}
		}

		public float AngularDamping
		{
			get => body.angularDamping;
			set
			{
				if (HasAuthority)
					body.angularDamping = value;
				else
					RPC_SetAngularDamping(value);
			}
		}

		public bool UseGravity
		{
			get => NetworkedUseGravity;
			set
			{
				if (HasAuthority)
				{
					NetworkedUseGravity = value;
					body.useGravity = value;
				}
				else
				{
					RPC_SetUseGravity(value);
				}
			}
		}

		/// <summary>
		/// while held the body is kinematic regardless
		/// </summary>
		public bool IsKinematic
		{
			get => authoredIsKinematic;
			set
			{
				if (HasAuthority)
				{
					authoredIsKinematic = value;
					if (!heldLocally)
						body.isKinematic = value;
				}
				else
				{
					RPC_SetIsKinematic(value);
				}
			}
		}

		public Vector3 LinearVelocity
		{
			get => body.linearVelocity;
			set
			{
				if (HasAuthority)
					body.linearVelocity = value;
				else
					RPC_SetLinearVelocity(value);
			}
		}

		public Vector3 AngularVelocity
		{
			get => body.angularVelocity;
			set
			{
				if (HasAuthority)
					body.angularVelocity = value;
				else
					RPC_SetAngularVelocity(value);
			}
		}

		protected override void Awake()
		{
			base.Awake();
			body = GetComponent<Rigidbody>();
			grabbable = GetComponent<NetworkGrabbable>();
			scalable = GetComponent<NetworkScalable>();
			throwEstimator = new ThrowVelocityEstimator(throwSampleFrames);

			if (grabbable != null)
			{
				grabbable.onGrabLocal.AddListener(OnGrabbedLocal);
				grabbable.onLetGoLocal.AddListener(OnReleasedLocal);
			}
		}

		private void OnDestroy()
		{
			if (grabbable != null)
			{
				grabbable.onGrabLocal.RemoveListener(OnGrabbedLocal);
				grabbable.onLetGoLocal.RemoveListener(OnReleasedLocal);
			}

			if (scalable != null)
				scalable.OnScaleChanged -= OnScaleChanged;
		}

		private void LateUpdate()
		{
			if (heldLocally)
				throwEstimator.AddSample(transform.position, transform.rotation, Time.time);
		}

		public override void FixedUpdateNetwork()
		{
			if (!IsReady || !HasAuthority)
				return;

			NetworkedIsHeld = heldLocally;

			// Authority can arrive after the grab, carrying the previous owner's dynamic state with it
			if (heldLocally && !body.isKinematic)
				body.isKinematic = true;
		}

		private void OnCollisionEnter(Collision collision)
		{
			CollisionEntered(collision);

			if (HasAuthority)
				TryClaimContact(collision);
		}

		public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
		{
			if (HasAuthority)
				body.AddForce(force, mode);
			else
				RPC_AddForce(force, mode);
		}

		public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force)
		{
			if (HasAuthority)
				body.AddForceAtPosition(force, position, mode);
			else
				RPC_AddForceAtPosition(force, position, mode);
		}

		public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
		{
			if (HasAuthority)
				body.AddTorque(torque, mode);
			else
				RPC_AddTorque(torque, mode);
		}

		public void StateAuthorityChanged()
		{
			if (IsReady && HasAuthority)
			{
				body.useGravity = NetworkedUseGravity;
			}
		}

		/// <summary>
		/// Requests authority over this body because a body simulated here hit it.
		/// </summary>
		public void ClaimFromContact()
		{
			if (!IsReady || HasAuthority || IsHeld)
				return;

			Object.RequestStateAuthority();
		}

		protected override void CheckForScripts()
		{
			if (IsReady)
				return;

			GameObject modRoot = obj.DataRoot;

			if (modRoot == null || !modRoot.TryGetComponent(out Rigidbody source))
			{
				Debug.LogWarning($"{name} has no Rigidbody on its mod root, leaving it kinematic.");
				return;
			}

			scaleMode = modRoot.TryGetComponent(out PhysicsBodySettings settings) ? settings.ScaleMode : PhysicsScaleMode.ScaleMassWithVolume;

			TakeOver(source);
			SetUpScaling();
			IsReady = true;
		}

		/// <summary>
		/// Copies the mod's Rigidbody settings onto the networked body, then removes the mod's Rigidbody.
		/// </summary>
		private void TakeOver(Rigidbody source)
		{
			RigidbodyBasicConfig basic = RigidbodyBasicConfig.Capture(source);
			RigidbodyCollisionConfig collision = RigidbodyCollisionConfig.Capture(source);
			RigidbodyAdvancedConfig advanced = RigidbodyAdvancedConfig.Capture(source);
			ConvertToBodySpace(source.transform, ref advanced);

			// Colliders attach to the nearest Rigidbody above them, so the mod's body has to go for them to join ours
			if (source.TryGetComponent(out PhysicsBodySettings settings))
				DestroyImmediate(settings);
			DestroyImmediate(source);

			authoredIsKinematic = basic.IsKinematic;

			// NetworkRigidbody3D keeps every copy without authority kinematic
			if (!HasAuthority || heldLocally)
				basic.IsKinematic = true;

			basic.Apply(body);
			collision.Apply(body);
			advanced.Apply(body);

			if (HasAuthority)
				NetworkedUseGravity = basic.UseGravity;
		}

		/// <summary>
		/// Moves manually set center of mass and inertia values from the mod root's space into this body's space.
		/// </summary>
		private void ConvertToBodySpace(Transform source, ref RigidbodyAdvancedConfig advanced)
		{
			if (!advanced.AutomaticCenterOfMass)
				advanced.CenterOfMass = transform.InverseTransformPoint(source.TransformPoint(advanced.CenterOfMass));

			if (!advanced.AutomaticInertiaTensor)
				advanced.InertiaTensorRotation = Quaternion.Inverse(transform.rotation) * source.rotation * advanced.InertiaTensorRotation;
		}

		private void SetUpScaling()
		{
			if (scalable == null)
				return;

			scalable.ScalingLocked = scaleMode == PhysicsScaleMode.BlockScaling;
			lastVolume = GetVolume();
			scalable.OnScaleChanged += OnScaleChanged;
		}

		private void OnScaleChanged()
		{
			float volume = GetVolume();

			// Every client tracks volume so a new authority starts from the right baseline
			if (scaleMode == PhysicsScaleMode.ScaleMassWithVolume && HasAuthority && lastVolume > Mathf.Epsilon)
				body.mass *= volume / lastVolume;

			lastVolume = volume;
		}

		private float GetVolume()
		{
			Vector3 scale = transform.localScale;
			return Mathf.Abs(scale.x * scale.y * scale.z);
		}

		private void OnGrabbedLocal(Grabber grabber)
		{
			if (heldLocally)
				return;

			heldLocally = true;
			throwEstimator.Clear();

			// Proxies are already kinematic so authority catches up in FixedUpdateNetwork if it arrives later
			if (IsReady)
				body.isKinematic = true;
		}

		private void OnReleasedLocal(Grabber grabber)
		{
			if (!heldLocally || grabbable.Holding != NetworkGrabbable.HoldState.None)
				return;

			heldLocally = false;

			if (!IsReady)
				return;

			throwEstimator.TryGetVelocity(out Vector3 linear, out Vector3 angular);

			// The estimate is for the transform's origin; the body moves about its center of mass
			linear += Vector3.Cross(angular, body.worldCenterOfMass - transform.position);

			if (HasAuthority)
				Release(linear, angular);
			else
				RPC_Release(linear, angular);
		}

		private void Release(Vector3 linear, Vector3 angular)
		{
			NetworkedIsHeld = false;
			body.isKinematic = authoredIsKinematic;

			if (authoredIsKinematic)
				return;

			body.linearVelocity = linear;
			body.angularVelocity = angular;
			body.WakeUp();
		}

		private void TryClaimContact(Collision collision)
		{
			if (body.isKinematic || collision.relativeVelocity.sqrMagnitude < contactAuthoritySpeed * contactAuthoritySpeed)
				return;

			Rigidbody other = collision.rigidbody;

			if (other != null && other != body && other.TryGetComponent(out NetworkRigidbodyAdapter otherBody))
				otherBody.ClaimFromContact();
		}

		#region RPCS

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_Release(Vector3 linear, Vector3 angular) => Release(linear, angular);

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetMass(float value) => Mass = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetLinearDamping(float value) => LinearDamping = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetAngularDamping(float value) => AngularDamping = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetUseGravity(NetworkBool value) => UseGravity = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetIsKinematic(NetworkBool value) => IsKinematic = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetLinearVelocity(Vector3 value) => LinearVelocity = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_SetAngularVelocity(Vector3 value) => AngularVelocity = value;

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_AddForce(Vector3 force, ForceMode mode) => AddForce(force, mode);

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode) => AddForceAtPosition(force, position, mode);

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_AddTorque(Vector3 torque, ForceMode mode) => AddTorque(torque, mode);

		#endregion
	}
}
