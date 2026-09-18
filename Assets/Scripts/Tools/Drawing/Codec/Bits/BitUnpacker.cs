using System;
using static CollabXR.Tools.Drawing.BitStreamExtensions;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Reads bits back out of words written by <see cref="BitPacker"/>, with bounds checking.
	 * </summary>
	 */
	public sealed class BitUnpacker : IBitSource
	{
		private int[] _words = Array.Empty<int>();

		public int BitPosition { get; private set; }

		#region Public Methods

		public void Reset(int[] source)
		{
			_words = source;
			BitPosition = 0;
		}

		public bool TryRead(int bitCount, out ulong value)
		{
			value = 0;
			if (bitCount < 0 || bitCount > MaxFieldBits || BitPosition + bitCount > _words.Length * WordBits)
			{
				return false;
			}

			int filled = 0;

			while (filled < bitCount)
			{
				// eat bits in current word then iterate for the rest
				int bitOffset = BitOffset(BitPosition);
				int take = Math.Min(WordBits - bitOffset, bitCount - filled);

				// cast to uint first so a negative word does not sign extend ones into the value
				ulong chunk = ((ulong)(uint)_words[WordIndex(BitPosition)] >> bitOffset) & LowMask(take);

				// bits arrive lowest first so each chunk lands above the ones already collected
				value |= chunk << filled;

				filled += take;
				BitPosition += take;
			}

			return true;
		}

		#endregion
	}
}
