namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke channel of Euler angles in degrees.
	 * </summary>
	 */
	public sealed class EulerChannel : Vector3Channel
	{
		private const string ChannelName = "EulerAngles";
		public const int DegreesPerTurn = 360;

		public EulerChannel()
			: base(ChannelName, new PeriodicDomain(DegreesPerTurn)) { }
	}
}
