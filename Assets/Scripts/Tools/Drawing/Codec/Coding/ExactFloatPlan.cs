namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * How one float can be coded by <see cref="ExactFloatCodec"/>.
	 * </summary>
	 */
	public readonly struct ExactFloatPlan
	{
		#region Properties

		/** <summary>IEEE-754 bit pattern of the value.</summary> */
		public int Bits { get; }

		/** <summary>True if the value can be coded as a residual from its prediction.</summary> */
		public bool HasResidual { get; }

		/** <summary>Difference between the value's exponent and the predicted exponent, can be -1, 0 or 1.</summary> */
		public int ExponentStep { get; }

		/** <summary>Zigzag-encoded residual, in units of the value's float spacing.</summary> */
		public ulong Residual { get; }

		/** <summary>Rice parameter the residual is coded with.</summary> */
		public int RiceK { get; }

		/** <summary>Length of the residual's Rice code.</summary> */
		public int RiceBits { get; }

		/** <summary>True if the value can be coded without an exponent symbol.</summary> */
		public bool CanUseImplicitExponent => HasResidual && ExponentStep == 0;

		#endregion

		public ExactFloatPlan(int bits)
			: this(bits, false, 0, 0, 0, 0) { }

		public ExactFloatPlan(int bits, bool hasResidual, int exponentStep, ulong residual, int riceK, int riceBits)
		{
			Bits = bits;
			HasResidual = hasResidual;
			ExponentStep = exponentStep;
			Residual = residual;
			RiceK = riceK;
			RiceBits = riceBits;
		}
	}
}
