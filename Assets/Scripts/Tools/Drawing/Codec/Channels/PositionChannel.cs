namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke channel of local positions.
	 * </summary>
	 */
	public sealed class PositionChannel : Vector3Channel
	{
		private const string ChannelName = "Position";

		public PositionChannel()
			: base(ChannelName, LinearDomain.Instance) { }
	}
}
