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
	/// Makes a CollabObject's Rigidbody work over the network. For mods it takes over the Rigidbody on the mod's root,
	/// for built-in objects it uses the one here. It also handles holding, throwing, scaling and gravity scale.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(NetworkRigidbody3D))]
	public class NetworkRigidbodyAdapter : ModNetworkBehaviour, IStateAuthorityChanged
	{
		[SerializeField]
		[Tooltip("Minimum impact speed for this body to take authority over an unheld body it hits.")]
		private float contactAuthoritySpeed = 0.1f;

		[Networked, OnChangedRender(nameof(OnHeldChanged))]
		public NetworkBool NetworkedIsHeld { get; set; }

		/// <summary>
		/// True once the mod's Rigidbody has been taken over and its settings applied
		/// </summary>
		public bool IsReady { get; private set; }

		private bool HasAuthority => Object != null && Object.IsValid && Object.HasStateAuthority;
		private bool IsHeld => IsReady && Object != null && Object.IsValid && NetworkedIsHeld;
		private bool HeldLocally => holdCount > 0;

		private Rigidbody body;
		private NetworkGrabbable grabbable;
		private NetworkScalable scalable;
		private ThrowVelocityEstimator throwEstimator;
		private ThrowTuning throwTuning;
		private PhysicsScaleMode scaleMode;
		private float gravityScale = 1f;
		private RigidbodyBasicConfig ownConfig;
		private NetworkRigidbody3D networkBody;
		private bool authoredRenderSleep;
		private BounceTuning bounceTuning;
		private PhysicsMaterial bounceMaterial;
		private bool authoredIsKinematic;
		private bool authoredUseGravity;
		private int holdCount;
		private bool handHeld;
		private float lastVolume;

		protected override void Awake()
		{
			base.Awake();
			body = GetComponent<Rigidbody>();
			grabbable = GetComponent<NetworkGrabbable>();
			scalable = GetComponent<NetworkScalable>();

			// Captured before NetworkRigidbody3D forces nonauthority copies kinematic in Spawned
			ownConfig = RigidbodyBasicConfig.Capture(body);

			networkBody = GetComponent<NetworkRigidbody3D>();
			authoredRenderSleep = networkBody.UseRenderSleepThresholds;

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

			PhysicsBodyUpdater.RemoveHeld(this);
			PhysicsBodyUpdater.RemoveScaledGravity(this);

			if (bounceMaterial != null)
				Destroy(bounceMaterial);
		}

		public override void Spawned()
		{
			base.Spawned();
			OnHeldChanged();
		}

		/// <summary>
		/// Keeps held bodies kinematic and interpolated
		/// </summary>
		private void OnHeldChanged()
		{
			networkBody.UseRenderSleepThresholds = authoredRenderSleep && !NetworkedIsHeld;
		}

		/// <summary>
		/// Called every frame by <see cref="PhysicsBodyUpdater"/> while held on this client.
		/// </summary>
		public void TickHeld()
		{
			throwEstimator.AddSample(transform.position, transform.rotation, Time.time);

			if (!IsReady || !HasAuthority)
				return;

			if (!NetworkedIsHeld)
				NetworkedIsHeld = true;

			// Authority can arrive after the grab, carrying the previous owner's dynamic state with it
			if (!body.isKinematic)
				body.isKinematic = true;
		}

		/// <summary>
		/// Called every physics step by <see cref="PhysicsBodyUpdater"/> when the gravity scale is not 1.
		/// </summary>
		public void TickScaledGravity()
		{
			if (!HasAuthority || body.isKinematic || body.IsSleeping())
				return;

			body.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (HasAuthority)
				TryClaimContact(collision);
		}

		public void StateAuthorityChanged()
		{
			if (!IsReady)
				return;

			if (HasAuthority && !HeldLocally)
			{
				NetworkedIsHeld = false;
				body.isKinematic = authoredIsKinematic;
			}

			RefreshInterpolation();
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

			if (modRoot != null && modRoot.TryGetComponent(out Rigidbody source))
			{
				ReadSettings(modRoot.GetComponent<PhysicsBodySettings>());
				TakeOver(source);
			}
			else if (IsBuiltIn())
			{
				ReadSettings(GetComponent<PhysicsBodySettings>());
				AdoptOwnBody();
			}
			else
			{
				// A mod that is still loading which fires again once the real prefab is in place
				return;
			}

			RefreshGravity();
			SetUpScaling();
			ApplyBounce();
			IsReady = true;
			RefreshInterpolation();

			if (UsesScaledGravity())
				PhysicsBodyUpdater.AddScaledGravity(this);
		}

		/// <summary>
		/// Builds one physics material from the bounce settings and puts it on every collider of this body.
		/// </summary>
		private void ApplyBounce()
		{
			if (!bounceTuning.OverrideMaterial)
				return;

			bounceMaterial = new PhysicsMaterial($"{name} Bounce")
			{
				bounciness = bounceTuning.Bounciness,
				dynamicFriction = bounceTuning.Friction,
				staticFriction = bounceTuning.Friction,
				bounceCombine = bounceTuning.IgnoreSurfaceBounce ? PhysicsMaterialCombine.Maximum : PhysicsMaterialCombine.Average,
				frictionCombine = PhysicsMaterialCombine.Average,
			};

			// Include inactive colliders
			foreach (Collider collider in GetComponentsInChildren<Collider>(true))
			{
				if (!collider.isTrigger)
					collider.sharedMaterial = bounceMaterial;
			}
		}

		/// <summary>
		/// Objects built into collab use their own rigidbody instead of deriving from the mod, so needs a special case.
		/// </summary>
		private bool IsBuiltIn()
		{
			CollabObjectData data = obj.GetData();
			return data != null && data.prefab != null;
		}

		private void ReadSettings(PhysicsBodySettings settings)
		{
			if (settings == null)
			{
				scaleMode = PhysicsScaleMode.ScaleMassWithVolume;
				gravityScale = 1f;
				throwTuning = new ThrowTuning();
				bounceTuning = new BounceTuning();
				return;
			}

			scaleMode = settings.ScaleMode;
			gravityScale = settings.GravityScale;
			bounceTuning = settings.Bounce;
			throwTuning = settings.Throwing;
		}

		/// <summary>
		/// Uses this object's own authored Rigidbody settings for internal mods
		/// </summary>
		private void AdoptOwnBody()
		{
			authoredIsKinematic = ownConfig.IsKinematic;
			authoredUseGravity = ownConfig.UseGravity;

			if (HasAuthority && HeldLocally)
				body.isKinematic = true;
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
			authoredUseGravity = basic.UseGravity;

			// NetworkRigidbody3D keeps every copy without authority kinematic
			if (!HasAuthority || HeldLocally)
				basic.IsKinematic = true;

			basic.Apply(body);
			collision.Apply(body);
			advanced.Apply(body);
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

		private bool UsesScaledGravity()
		{
			return IsReady && authoredUseGravity && !Mathf.Approximately(gravityScale, 1f);
		}

		/// <summary>
		/// Unity's gravity is used at scale 1 and any other scale is applied as a force by <see cref="PhysicsBodyUpdater"/>.
		/// </summary>
		private void RefreshGravity()
		{
			body.useGravity = authoredUseGravity && Mathf.Approximately(gravityScale, 1f);
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

		/// <summary>
		/// Holds the body kinematic for a tool that moves its transform directly, such as the Mover.
		/// </summary>
		public void BeginHold()
		{
			holdCount++;

			if (holdCount > 1)
				return;

			// Only objects that actually get held need the sample buffer
			throwEstimator ??= new ThrowVelocityEstimator();
			throwEstimator.Clear();
			PhysicsBodyUpdater.AddHeld(this);

			if (IsReady)
			{
				body.isKinematic = true;
				RefreshInterpolation();
			}
		}

		/// <summary>
		/// Ends a hold started with <see cref="BeginHold"/>. The body is released once every hold has ended.
		/// </summary>
		/// <param name="throwOnRelease">True to keep the motion it was being moved with, false to drop it in place.</param>
		public void EndHold(bool throwOnRelease)
		{
			if (holdCount == 0)
				return;

			holdCount--;

			if (holdCount > 0)
				return;

			PhysicsBodyUpdater.RemoveHeld(this);

			// Also covers being despawned while held
			if (!IsReady || Object == null || !Object.IsValid)
				return;

			Vector3 linear = Vector3.zero;
			Vector3 angular = Vector3.zero;

			if (throwOnRelease)
				GetThrowVelocity(out linear, out angular);

			if (HasAuthority)
				Release(linear, angular);
			else
				RPC_Release(linear, angular);
		}

		private void OnGrabbedLocal(Grabber grabber)
		{
			if (handHeld)
				return;

			handHeld = true;
			BeginHold();
		}

		private void OnReleasedLocal(Grabber grabber)
		{
			if (!handHeld || grabbable.Holding != NetworkGrabbable.HoldState.None)
				return;

			handHeld = false;
			EndHold(true);
		}

		private void GetThrowVelocity(out Vector3 linear, out Vector3 angular)
		{
			throwEstimator.TryGetVelocity(throwTuning.SampleWindow, out linear, out angular);

			// The estimate is for the transform's origin so the body moves about its center of mass
			linear += Vector3.Cross(angular, body.worldCenterOfMass - transform.position);
			linear = Vector3.ClampMagnitude(linear * throwTuning.Strength, throwTuning.MaxSpeed);
			angular *= throwTuning.Spin;
		}

		private void Release(Vector3 linear, Vector3 angular)
		{
			// A late release from the previous holder must not drop an object this client has since grabbed
			if (HeldLocally)
				return;

			NetworkedIsHeld = false;
			body.isKinematic = authoredIsKinematic;
			RefreshInterpolation();

			if (authoredIsKinematic)
				return;

			body.linearVelocity = linear;
			body.angularVelocity = angular;
			body.WakeUp();
		}

		/// <summary>
		/// Special casing because of how Unity handles interpolation
		/// </summary>
		private void RefreshInterpolation()
		{
			bool simulatedHere = IsReady && HasAuthority && !HeldLocally && !authoredIsKinematic;
			body.interpolation = simulatedHere ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
		}

		private void TryClaimContact(Collision collision)
		{
			if (body.isKinematic || collision.relativeVelocity.sqrMagnitude < contactAuthoritySpeed * contactAuthoritySpeed)
				return;

			Rigidbody other = collision.rigidbody;

			if (other != null && other != body && other.TryGetComponent(out NetworkRigidbodyAdapter otherBody))
				otherBody.ClaimFromContact();
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		public void RPC_Release(Vector3 linear, Vector3 angular) => Release(linear, angular);
	}
}
