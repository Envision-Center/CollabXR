using System;
using static CollabXR.Tools.Drawing.BitStreamExtensions;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Packs bits into 32-bit words for Fusion's networking stack, filled from the least significant bit.
	 * Unused bits are zero.
	 * </summary>
	 */
	public sealed class BitPacker : IBitSink
	{
		private int[] _words = Array.Empty<int>();
		private bool _overflowed;

		#region Properties

		/** <summary>Number of bits written since the last reset (index for next write)</summary> */
		public int BitPosition { get; private set; }

		/** <summary>Number of bits the target words can hold</summary> */
		public int CapacityBits => _words.Length * WordBits;

		#endregion

		#region Public Methods

		public void Reset(int[] target)
		{
			_words = target;

			// Write ORs into the words so they have to start at zero
			Array.Clear(_words, 0, _words.Length);
			BitPosition = 0;
			_overflowed = false;
		}

		public void Write(ulong value, int bitCount)
		{
			if (bitCount < 0 || bitCount > MaxFieldBits)
			{
				throw new ArgumentOutOfRangeException(nameof(bitCount));
			}

			if (_overflowed || BitPosition + bitCount > CapacityBits)
			{
				_overflowed = true;
				return;
			}

			while (bitCount > 0)
			{
				// fields are not aligned to words to maximize packing efficiency
				int bitOffset = BitOffset(BitPosition);
				int take = Math.Min(WordBits - bitOffset, bitCount);

				// keep only the bits going into this word then slide them up to the offset then OR them into place
				_words[WordIndex(BitPosition)] |= (int)((uint)(value & LowMask(take)) << bitOffset);

				// discard the bits just written so the next pass starts at the new low end
				value >>= take;
				BitPosition += take;
				bitCount -= take;
			}
		}

		#endregion
	}
}
