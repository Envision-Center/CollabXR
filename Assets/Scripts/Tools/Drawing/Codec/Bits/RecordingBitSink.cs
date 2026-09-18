using System;
using System.Collections.Generic;
using static CollabXR.Tools.Drawing.BitStreamExtensions;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Compressed output held as an ordered list of bit fields before packing.
	 * </summary>
	 */
	public sealed class RecordingBitSink : IBitSink
	{
		private readonly List<ulong> _values = new();
		private readonly List<byte> _lengths = new();

		/** <summary>Total number of bits recorded.</summary> */
		public int BitCount { get; private set; }

		#region Public Methods

		public void Clear()
		{
			_values.Clear();
			_lengths.Clear();
			BitCount = 0;
		}

		public void Write(ulong value, int bitCount)
		{
			if (bitCount < 0 || bitCount > MaxFieldBits)
			{
				throw new ArgumentOutOfRangeException(nameof(bitCount));
			}

			// Rice code with k = 0 has no low bits to store
			if (bitCount == 0)
			{
				return;
			}

			// mask to the field width so a wider value cannot leak into the next field when this is replayed
			_values.Add(value & LowMask(bitCount));
			_lengths.Add((byte)bitCount);
			BitCount += bitCount;
		}

		/** <summary>Replays every recorded field, in order, into <paramref name="sink"/>.</summary> */
		public void CopyTo(IBitSink sink)
		{
			for (int i = 0; i < _values.Count; i++)
			{
				sink.Write(_values[i], _lengths[i]);
			}
		}

		#endregion
	}
}
