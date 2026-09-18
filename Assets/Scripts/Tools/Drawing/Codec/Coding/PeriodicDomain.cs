using System;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Domain for values that repeat every whole number of units.
	 * </summary>
	 */
	public sealed class PeriodicDomain : ILiftedDomain
	{
		private const int MaxPeriod = 1 << (ExactFloat.MaxExponent + 1);

		private readonly long _modulus;

		public PeriodicDomain(int period)
		{
			if (period <= 0 || period > MaxPeriod)
			{
				throw new ArgumentOutOfRangeException(nameof(period));
			}

			_modulus = (long)period << ExactFloat.LiftShift;
		}

		public long Normalize(long lifted)
		{
			long remainder = lifted % _modulus;
			return remainder < 0 ? remainder + _modulus : remainder;
		}

		public long Difference(long lifted, long reference)
		{
			long forward = Normalize(lifted - reference);
			return forward > _modulus / 2 ? forward - _modulus : forward;
		}
	}
}
