using System;
using UnityEngine;

namespace CollabXR
{
	[Serializable]
	public struct RigidbodyBasicConfig : IEquatable<RigidbodyBasicConfig>
	{
		public float Mass;
		public float LinearDamping;
		public float AngularDamping;
		public bool UseGravity;
		public bool IsKinematic;
		public RigidbodyConstraints Constraints;
		public RigidbodyInterpolation Interpolation;
		public CollisionDetectionMode CollisionDetectionMode;
		public bool DetectCollisions;
		public float MaxLinearVelocity;
		public float MaxAngularVelocity;
		public float MaxDepenetrationVelocity;
		public float SleepThreshold;
		public int SolverIterations;
		public int SolverVelocityIterations;

		public static RigidbodyBasicConfig Capture(Rigidbody rb) =>
			new()
			{
				Mass = rb.mass,
				LinearDamping = rb.linearDamping,
				AngularDamping = rb.angularDamping,
				UseGravity = rb.useGravity,
				IsKinematic = rb.isKinematic,
				Constraints = rb.constraints,
				Interpolation = rb.interpolation,
				CollisionDetectionMode = rb.collisionDetectionMode,
				DetectCollisions = rb.detectCollisions,
				MaxLinearVelocity = rb.maxLinearVelocity,
				MaxAngularVelocity = rb.maxAngularVelocity,
				MaxDepenetrationVelocity = rb.maxDepenetrationVelocity,
				SleepThreshold = rb.sleepThreshold,
				SolverIterations = rb.solverIterations,
				SolverVelocityIterations = rb.solverVelocityIterations,
			};

		public void Apply(Rigidbody rb)
		{
			rb.mass = Mass;
			rb.linearDamping = LinearDamping;
			rb.angularDamping = AngularDamping;
			rb.useGravity = UseGravity;
			rb.isKinematic = IsKinematic;
			rb.constraints = Constraints;
			rb.interpolation = Interpolation;
			rb.collisionDetectionMode = CollisionDetectionMode;
			rb.detectCollisions = DetectCollisions;
			rb.maxLinearVelocity = MaxLinearVelocity;
			rb.maxAngularVelocity = MaxAngularVelocity;
			rb.maxDepenetrationVelocity = MaxDepenetrationVelocity;
			rb.sleepThreshold = SleepThreshold;
			rb.solverIterations = SolverIterations;
			rb.solverVelocityIterations = SolverVelocityIterations;
		}

		public bool Equals(RigidbodyBasicConfig o) =>
			Mass == o.Mass
			&& LinearDamping == o.LinearDamping
			&& AngularDamping == o.AngularDamping
			&& UseGravity == o.UseGravity
			&& IsKinematic == o.IsKinematic
			&& Constraints == o.Constraints
			&& Interpolation == o.Interpolation
			&& CollisionDetectionMode == o.CollisionDetectionMode
			&& DetectCollisions == o.DetectCollisions
			&& MaxLinearVelocity == o.MaxLinearVelocity
			&& MaxAngularVelocity == o.MaxAngularVelocity
			&& MaxDepenetrationVelocity == o.MaxDepenetrationVelocity
			&& SleepThreshold == o.SleepThreshold
			&& SolverIterations == o.SolverIterations
			&& SolverVelocityIterations == o.SolverVelocityIterations;
	}

	[Serializable]
	public struct RigidbodyCollisionConfig : IEquatable<RigidbodyCollisionConfig>
	{
		public LayerMask IncludeLayers;
		public LayerMask ExcludeLayers;

		public static RigidbodyCollisionConfig Capture(Rigidbody rb) => new() { IncludeLayers = rb.includeLayers, ExcludeLayers = rb.excludeLayers };

		public void Apply(Rigidbody rb)
		{
			rb.includeLayers = IncludeLayers;
			rb.excludeLayers = ExcludeLayers;
		}

		public bool Equals(RigidbodyCollisionConfig o) => IncludeLayers == o.IncludeLayers && ExcludeLayers == o.ExcludeLayers;
	}

	[Serializable]
	public struct RigidbodyAdvancedConfig : IEquatable<RigidbodyAdvancedConfig>
	{
		public bool AutomaticCenterOfMass;
		public Vector3 CenterOfMass;
		public bool AutomaticInertiaTensor;
		public Vector3 InertiaTensor;
		public Quaternion InertiaTensorRotation;

		public static RigidbodyAdvancedConfig Capture(Rigidbody rb) =>
			new()
			{
				AutomaticCenterOfMass = rb.automaticCenterOfMass,
				CenterOfMass = rb.centerOfMass,
				AutomaticInertiaTensor = rb.automaticInertiaTensor,
				InertiaTensor = rb.inertiaTensor,
				InertiaTensorRotation = rb.inertiaTensorRotation,
			};

		public void Apply(Rigidbody rb)
		{
			rb.automaticCenterOfMass = AutomaticCenterOfMass;

			if (!AutomaticCenterOfMass)
				rb.centerOfMass = CenterOfMass;

			rb.automaticInertiaTensor = AutomaticInertiaTensor;

			if (!AutomaticInertiaTensor)
			{
				rb.inertiaTensor = InertiaTensor;
				rb.inertiaTensorRotation = InertiaTensorRotation;
			}
		}

		public bool Equals(RigidbodyAdvancedConfig o) =>
			AutomaticCenterOfMass == o.AutomaticCenterOfMass
			&& CenterOfMass == o.CenterOfMass
			&& AutomaticInertiaTensor == o.AutomaticInertiaTensor
			&& InertiaTensor == o.InertiaTensor
			&& InertiaTensorRotation == o.InertiaTensorRotation;
	}
}
