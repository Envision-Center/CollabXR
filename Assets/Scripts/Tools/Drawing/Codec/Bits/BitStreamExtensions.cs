namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Shared bit stream layout, mapping a bit position onto the word it lives in, plus single-bit flag helpers.
	 * </summary>
	 */
	public static class BitStreamExtensions
	{
		public const int FlagBits = 1; // number of bits used for flags
		public const int WordBits = 32; // Fusion uses 4 byte words to track networked variables
		public const int MaxFieldBits = 64; // sizeof(ulong) * 8
		private const int WordIndexShift = 5; // 2^5 = 32 bits per word
		private const int BitOffsetMask = WordBits - 1; // bit & 31 = bit % 32

		/** <summary>Index of the word <paramref name="bitPosition"/> lands in.</summary> */
		public static int WordIndex(int bitPosition)
		{
			return bitPosition >> WordIndexShift;
		}

		/** <summary>How far into its word <paramref name="bitPosition"/> sits.</summary> */
		public static int BitOffset(int bitPosition)
		{
			return bitPosition & BitOffsetMask;
		}

		/** <summary>A mask of the lowest <paramref name="bitCount"/> bits.</summary> */
		public static ulong LowMask(int bitCount)
		{
			// 1UL << 64 is 1UL << 0 in C# which is 1 rather than 0 so a full field has to be edge cased :(
			return bitCount >= MaxFieldBits ? ulong.MaxValue : (1UL << bitCount) - 1;
		}

		/** <summary>Writes <paramref name="flag"/> as one bit.</summary> */
		public static void WriteFlag(this IBitSink sink, bool flag)
		{
			sink.Write(flag ? 1UL : 0UL, FlagBits);
		}

		/** <summary>Reads one bit as a flag.</summary> */
		public static bool TryReadFlag(this IBitSource source, out bool flag)
		{
			bool read = source.TryRead(FlagBits, out ulong value);
			flag = value != 0;
			return read;
		}
	}
}
