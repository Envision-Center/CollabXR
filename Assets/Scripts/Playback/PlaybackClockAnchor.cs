using Fusion;

namespace CollabXR.Cycles
{
	/** <summary>
	 * Lightweight Fusion struct to transmit relevant playback data across clients.
	 * Contains only blittable value types so Fusion can interpolate it in Render().
	 * Component indices are stored separately as a [Networked] property on NetworkPlaybackDirector.
	 * </summary> */
	public struct PlaybackClockAnchor : INetworkStruct
	{
		public float AnchorPercent;
		public bool IsPlaying;
		public float Speed;
	}
}
