using System;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Rice code whose parameter follows a moving average of the values it has coded.
	 * A Rice code splits a value into value >> k written in unary, then its k low bits written as they are.
	 *
	 * https://en.wikipedia.org/wiki/Golomb_coding
	 * </summary>
	 */
	public struct AdaptiveRice
	{
		// a bunch of constants for the Rice code

		public const int MaxQuotient = 64; // longest unary run a decoder will follow before quitting
		private const int MaxK = 48; // k never needs to exceed the width of a capped sample

		private const int MeanDecayShift = 1; // the mean forgets half its history per value
		private const int KMeanShift = 1; // smallest k with 2^(k+1) >= mean puts 2^k near half the mean which is good for Rice coding

		private const ulong SampleCap = 1UL << 40; // ceiling on a single sample's influence
		private const int UnaryChunkBits = BitStreamExtensions.WordBits; // ones are emitted a word at a time

		private ulong _mean;

		/** <summary>Number of low bits stored after the unary quotient for the next value.</summary> */
		public int K
		{
			get
			{
				int k = 0;
				while (k < MaxK && (1UL << (k + KMeanShift)) < _mean)
				{
					k++;
				}

				return k;
			}
		}

		public AdaptiveRice(ulong initialMean)
		{
			_mean = Math.Min(initialMean, SampleCap);
		}

		#region Public Methods

		/** <summary>Folds a coded value into the moving average.</summary> */
		public void Update(ulong value)
		{
			// cap adjusts for outliers
			_mean = _mean - (_mean >> MeanDecayShift) + (Math.Min(value, SampleCap) >> MeanDecayShift);
		}

		/**
		 * <summary>
		 * Number of bits needed to code <paramref name="value"/> with parameter <paramref name="k"/>.
		 * </summary>
		 * <returns>The code length or <see cref="int.MaxValue"/> if the unary part would exceed <see cref="MaxQuotient"/>.</returns>
		 */
		public static int Length(ulong value, int k)
		{
			// unary run, the zero ending it, then the k low bits
			ulong quotient = value >> k;
			return quotient >= MaxQuotient ? int.MaxValue : (int)quotient + 1 + k;
		}

		/** <summary>Writes <paramref name="value"/> as a unary quotient followed by its <paramref name="k"/> low bits.</summary> */
		public static void Write(IBitSink sink, ulong value, int k)
		{
			// unary part, that many 1 bits then a 0 to end the run, written 32 at a time
			ulong quotient = value >> k;
			while (quotient >= UnaryChunkBits)
			{
				sink.Write(uint.MaxValue, UnaryChunkBits);
				quotient -= UnaryChunkBits;
			}

			// (1 << q) - 1 = q ones, then adds a 0 through sink.Write
			sink.Write((1UL << (int)quotient) - 1, (int)quotient + 1);
			sink.Write(value, k);
		}

		/**
		 * <summary>
		 * Reads a value written by <see cref="Write"/>.
		 * </summary>
		 * <returns>False if the source ran out or the unary part exceeded <see cref="MaxQuotient"/>.</returns>
		 */
		public static bool TryRead(IBitSource source, int k, out ulong value)
		{
			value = 0;
			ulong quotient = 0;

			while (true)
			{
				if (!source.TryReadFlag(out bool continues))
				{
					return false;
				}

				if (!continues)
				{
					break;
				}

				quotient++;
				if (quotient >= MaxQuotient)
				{
					return false;
				}
			}

			if (!source.TryRead(k, out ulong remainder))
			{
				return false;
			}

			// shift back up and add back the low bits to get the original value
			value = (quotient << k) | remainder;
			return true;
		}

		#endregion
	}
}
