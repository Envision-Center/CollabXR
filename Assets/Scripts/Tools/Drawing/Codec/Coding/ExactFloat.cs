using System;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Lossless conversions between IEEE-754 floats and exact 64-bit integers equal to <c>value * 2^52</c>,
	 * using only integer operations so every platform produces identical results.
	 *
	 * I feel like the guy from quake 3 rn
	 * </summary>
	 */
	public static class ExactFloat
	{
		// a float is M * 2^(E-23) with a 24 bit M, therefore scaling by 2^52 gives M * 2^(E+29)
		public const int LiftShift = 52;

		// E <= 8 keeps |n| under 2^62 so 2*a - b cannot overflow
		public const int MinExponent = -29; // 1.9e-9 is our minimum value
		public const int MaxExponent = 8; // 512 is our maximum value which should be fine probably

		public const long HistoryLimit = 1L << 61;
		private const int ExponentBias = 127; // IEEE-754 moment for storing the exponent unsigned
		private const int MantissaBits = 23; // stored mantissa bits; the 24th being the implied leading 1
		private const int MantissaMask = (1 << MantissaBits) - 1; // isolates mantissa bits
		private const int ImplicitMantissaBit = 1 << MantissaBits; // the leading 1 to put back and the low bound on a valid M
		private const int MaxBiasedExponent = 0xFF; // reserved for infinity and NaN
		private const int SignBit = int.MinValue; // bit 31
		private const int BitsPerLong = 64;

		private const int HalfWordShift = 32;
		private const int QuarterWordShift = 16;
		private const int ByteShift = 8;
		private const int NibbleShift = 4;
		private const int PairShift = 2;

		#region Public Methods

		/** <summary>The IEEE-754 bit pattern of <paramref name="value"/>.</summary> */
		public static int ToBits(float value)
		{
			return BitConverter.SingleToInt32Bits(value);
		}

		/** <summary>The float with IEEE-754 bit pattern <paramref name="bits"/>.</summary> */
		public static float FromBits(int bits)
		{
			return BitConverter.Int32BitsToSingle(bits);
		}

		/** <summary>Shift that gives the spacing between adjacent floats with <paramref name="exponent"/>, in lifted units.</summary> */
		public static int UlpShift(int exponent)
		{
			// neighbours differ by 2^(E-23) which lifts to 2^(E-23+52)
			return exponent + LiftShift - MantissaBits;
		}

		/**
		 * <summary>
		 * Converts a float's bits to its exact lifted integer.
		 * </summary>
		 * <param name="bits">IEEE-754 bit pattern.</param>
		 * <param name="lifted">The value times 2^52.</param>
		 * <param name="exponent">The unbiased binary exponent.</param>
		 * <returns>False for zero, subnormals, infinities, NaN, and exponents outside [<see cref="MinExponent"/>, <see cref="MaxExponent"/>].</returns>
		 */
		public static bool TryLift(int bits, out long lifted, out int exponent)
		{
			// slide the exponent field down past the mantissa, mask off the sign above it, then undo the bias
			int biasedExponent = (bits >> MantissaBits) & MaxBiasedExponent;
			exponent = biasedExponent - ExponentBias;
			lifted = 0;

			if (biasedExponent == 0 || biasedExponent == MaxBiasedExponent || exponent < MinExponent || exponent > MaxExponent)
			{
				return false;
			}

			// put the implied leading 1 back to get the full 24 bit M then shift it into lifted units
			lifted = (long)((bits & MantissaMask) | ImplicitMantissaBit) << UlpShift(exponent);
			if (bits < 0)
			{
				lifted = -lifted;
			}

			return true;
		}

		/**
		 * <summary>
		 * Converts a lifted integer back to float bits, requiring it to be a float with exactly <paramref name="exponent"/>.
		 * </summary>
		 * <returns>False if no float with that exponent equals <paramref name="lifted"/>.</returns>
		 */
		public static bool TryUnlift(long lifted, int exponent, out int bits)
		{
			bits = 0;
			if (lifted == 0 || exponent < MinExponent || exponent > MaxExponent)
			{
				return false;
			}

			int shift = UlpShift(exponent);
			ulong magnitude = Magnitude(lifted);

			// anything finer than this exponent's spacing is not a representable float, so reject rather than round (for now)
			if ((magnitude & BitStreamExtensions.LowMask(shift)) != 0)
			{
				return false;
			}

			// M has to land back in this range to belong in this exponent
			ulong mantissa = magnitude >> shift;
			if (mantissa < ImplicitMantissaBit || mantissa > (ImplicitMantissaBit | MantissaMask))
			{
				return false;
			}

			// reassemble sign, biased exponent, and M with its leading bit masked away again
			bits = (lifted < 0 ? SignBit : 0) | ((exponent + ExponentBias) << MantissaBits) | ((int)mantissa & MantissaMask);
			return true;
		}

		/**
		 * <summary>
		 * Deterministic lifted stand-in for any float. Needed because of the predictive nature of the codec, so we need to be able to tell
		 * apart values that cannot be lifted (NaN or out of range) from those that can (which are lifted to zero or saturate at <see cref="HistoryLimit"/>).
		 * </summary>
		 */
		public static long LiftForHistory(int bits)
		{
			if (TryLift(bits, out long lifted, out int exponent))
			{
				return lifted;
			}

			// NaN is the reserved exponent with a non empty mantissa
			bool isNaN = ((bits >> MantissaBits) & MaxBiasedExponent) == MaxBiasedExponent && (bits & MantissaMask) != 0;
			if (isNaN || exponent < MinExponent)
			{
				return 0;
			}

			// saturate too large values while keeping the sign
			return bits < 0 ? -HistoryLimit : HistoryLimit;
		}

		/** <summary>Binary exponent of a nonzero lifted value.</summary> */
		public static int ExponentOf(long lifted)
		{
			// the highest set bit sits that far above E
			return HighestSetBit(Magnitude(lifted)) - LiftShift;
		}

		/** <summary>Number of zero bits below the lowest set bit of <paramref name="value"/>'s magnitude, or 64 for zero.</summary> */
		public static int TrailingZeroCount(long value)
		{
			ulong magnitude = Magnitude(value);
			return magnitude == 0 ? BitsPerLong : HighestSetBit(magnitude & (0UL - magnitude)); // number of trailing zeros are how coarse a grid the value is on
		}

		/** <summary>Absolute value of <paramref name="value"/> as an unsigned integer.</summary> */
		public static ulong Magnitude(long value)
		{
			return value < 0 ? (ulong)(-value) : (ulong)value;
		}

		#endregion

		#region Helpers

		/** <summary>Index of the highest set bit, or 0 for a value with none.</summary> */
		private static int HighestSetBit(ulong value)
		{
			// this is literally just binary search

			int bit = 0;
			bit += ShiftIfAbove(ref value, HalfWordShift);
			bit += ShiftIfAbove(ref value, QuarterWordShift);
			bit += ShiftIfAbove(ref value, ByteShift);
			bit += ShiftIfAbove(ref value, NibbleShift);
			bit += ShiftIfAbove(ref value, PairShift);
			bit += ShiftIfAbove(ref value, 1);
			return bit;
		}

		private static int ShiftIfAbove(ref ulong value, int shift)
		{
			if ((value >> shift) == 0)
			{
				return 0;
			}

			value >>= shift;
			return shift;
		}

		#endregion
	}
}
