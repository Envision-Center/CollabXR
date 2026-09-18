namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Destination that compressed bit fields are appended to.
	 * </summary>
	 */
	public interface IBitSink
	{
		/**
		 * <summary>
		 * Appends the lowest <paramref name="bitCount"/> bits of <paramref name="value"/>, least significant bit first.
		 * </summary>
		 * <param name="value">Bits to append. Bits above <paramref name="bitCount"/> are ignored.</param>
		 * <param name="bitCount">Number of bits to append from 0 to 64.</param>
		 */
		void Write(ulong value, int bitCount);
	}
}
