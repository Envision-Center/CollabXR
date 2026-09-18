namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Source that compressed bit fields are read back from.
	 * </summary>
	 */
	public interface IBitSource
	{
		/** <summary>Number of bits read so far (index)</summary> */
		int BitPosition { get; }

		/**
		 * <summary>
		 * Reads the next <paramref name="bitCount"/> bits, least significant bit first.
		 * </summary>
		 * <param name="bitCount">Number of bits to read from 0 to 64.</param>
		 * <param name="value">The bits read or 0 on failure.</param>
		 * <returns>False if there are not enough bits left.</returns>
		 */
		bool TryRead(int bitCount, out ulong value);
	}
}
