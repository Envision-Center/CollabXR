namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Domain for values that do not wrap.
	 * </summary>
	 */
	public sealed class LinearDomain : ILiftedDomain
	{
		public static LinearDomain Instance { get; } = new(); // lazy singleton

		private LinearDomain() { }

		public long Normalize(long lifted)
		{
			return lifted;
		}

		public long Difference(long lifted, long reference)
		{
			return lifted - reference;
		}
	}
}
