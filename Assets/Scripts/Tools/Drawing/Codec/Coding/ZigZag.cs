namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Maps signed integers onto unsigned ones so values near zero stay small whether they are positive or negative.
	 * Important for Rice coding because we need small magnitude numbers to be close to 0 so we cant use two's complement.
	 * </summary>
	 */
	public static class ZigZag
	{
		private const int SignShift = 63; // shifting a long right by 63 leaves only copies of its sign bit
		#region Public Methods

		/**
		 * <summary>Encodes 0, -1, 1, -2, 2, ... as 0, 1, 2, 3, 4, ...</summary>
		 */
		public static ulong Encode(long value)
		{
			return (ulong)((value << 1) ^ (value >> SignShift));
		}

		/** <summary>Inverse of <see cref="Encode"/>.</summary> */
		public static long Decode(ulong value)
		{
			return (long)(value >> 1) ^ -(long)(value & 1);
		}

		#endregion
	}
}
