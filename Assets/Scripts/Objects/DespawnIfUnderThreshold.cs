using Fusion;

namespace CollabXR.Objects
{
	public class DespawnIfUnderThreshold : NetworkBehaviour
	{
		private const float MinY = -100;

		public override void Render()
		{
			if (HasStateAuthority && transform.position.y < MinY)
				Object.Runner.Despawn(Object);
		}
	}
}
