using Fusion;

namespace CollabXR
{
	/// <summary>
	/// Physics values synced across players.
	/// </summary>
	public struct PhysicsBodyProperties : INetworkStruct
	{
		public NetworkBool UseGravity;
		public float GravityScale;
		public float LinearDrag;
		public float AngularDrag;
		public NetworkBool OverrideMaterial;
		public float Bounciness;
		public float Friction;
		public NetworkBool Frozen;
		public float ThrowStrength;
	}
}
