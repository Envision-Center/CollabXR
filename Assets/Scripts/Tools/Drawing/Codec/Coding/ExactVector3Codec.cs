using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Losslessly codes a sequence of <see cref="Vector3"/> values with one <see cref="ExactFloatCodec"/> per axis,
	 * sharing a single flag when every axis keeps its predicted exponent.
	 * </summary>
	 */
	public sealed class ExactVector3Codec
	{
		private const int AxisCount = 3;
		public const int WorstCaseBits = BitStreamExtensions.FlagBits + AxisCount * ExactFloatCodec.WorstCaseBits; // 1 + 3 * 34 = 103

		private readonly ExactFloatCodec _x;
		private readonly ExactFloatCodec _y;
		private readonly ExactFloatCodec _z;

		public ExactVector3Codec(ILiftedDomain domain)
		{
			_x = new ExactFloatCodec(domain);
			_y = new ExactFloatCodec(domain);
			_z = new ExactFloatCodec(domain);
		}

		#region Public Methods

		public void Reset()
		{
			_x.Reset();
			_y.Reset();
			_z.Reset();
		}

		/** <summary>Writes <paramref name="value"/> and adds it to the prediction history.</summary> */
		public void Encode(Vector3 value, IBitSink sink)
		{
			// important: create plans first since Write() commits to state
			ExactFloatPlan x = _x.Plan(value.x);
			ExactFloatPlan y = _y.Plan(value.y);
			ExactFloatPlan z = _z.Plan(value.z);
			bool implicitExponent = ShouldUseImplicitExponent(x, y, z);

			sink.WriteFlag(!implicitExponent);
			_x.Write(x, implicitExponent, sink);
			_y.Write(y, implicitExponent, sink);
			_z.Write(z, implicitExponent, sink);
		}

		/**
		 * <summary>
		 * Reads a value written by <see cref="Encode"/> and adds it to the prediction history.
		 * </summary>
		 * <returns>False if the source ran out or the bits are invalid.</returns>
		 */
		public bool TryDecode(IBitSource source, out Vector3 value)
		{
			value = default;
			if (!source.TryReadFlag(out bool hasExplicitSymbols))
			{
				return false;
			}

			if (!_x.TryRead(source, !hasExplicitSymbols, out float x))
			{
				return false;
			}
			if (!_y.TryRead(source, !hasExplicitSymbols, out float y))
			{
				return false;
			}

			if (!_z.TryRead(source, !hasExplicitSymbols, out float z))
			{
				return false;
			}

			value = new Vector3(x, y, z);
			return true;
		}

		#endregion

		#region Helpers

		private static bool ShouldUseImplicitExponent(ExactFloatPlan x, ExactFloatPlan y, ExactFloatPlan z)
		{
			if (!x.CanUseImplicitExponent || !y.CanUseImplicitExponent || !z.CanUseImplicitExponent)
			{
				return false;
			}

			// when all three axes keep their predicted exponent, use one flag to save on bits
			int implicitBits = x.RiceBits + y.RiceBits + z.RiceBits;
			int explicitBits = ExactFloatCodec.ExplicitBits(x) + ExactFloatCodec.ExplicitBits(y) + ExactFloatCodec.ExplicitBits(z);
			return implicitBits <= explicitBits;
		}

		#endregion
	}
}
