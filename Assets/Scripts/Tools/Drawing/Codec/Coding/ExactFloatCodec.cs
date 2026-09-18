using System;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Losslessly codes a sequence of floats by predicting each one from the values before it and storing the exact
	 * integer residual on the coarsest float grid the values have been using, or the raw bits when cheaper.
	 * </summary>
	 */
	public sealed class ExactFloatCodec
	{
		public const int RawBits = 32; // a whole float is stored as it is when nothing cheaper is available

		// worst case per value: the raw symbol plus the whole float
		public const int WorstCaseBits = RawSymbolBits + RawBits;

		// symbols ordered most common first for best compression
		private const int SameExponentSymbolBits = 1; // '0'
		private const int ExponentStepSymbolBits = 3; // '11' plus a direction bit
		private const int RawSymbolBits = 2; // '10'

		// step size beyond which the raw path is taken instead of the predicted grid
		private const int MaxExponentStep = 1;

		// initial mean for the Rice coder
		private const ulong InitialRiceMean = 1UL << 16;

		// lifted values stay under 2^61 so clamping predictions at 2^62 leaves room for 2*a - b without overflow
		private const long PredictionLimit = 1L << 62;
		private const int SecondOrderHistory = 2; // values needed before a straight line can be continued through them
		private const int ScoreErrorShift = 8; // errors are scaled down before scoring so a long run cannot overflow the total
		private const ulong ScoreErrorCap = 1UL << 56; // ceiling on error contribution
		private const int ScoreDecayShift = 2; // each new error retires a quarter of the running score

		private const int GridHistory = 8; // ring buffer size at which the estimate is trusted
		private const int MinGridHistory = 4; // values required before the estimate is trusted at all

		private enum Symbol
		{
			SameExponent, // the value has the same exponent as the prediction
			ExponentStep, // the value has a different exponent than the prediction
			Raw, // the value is too far from the grid to be coded
		}

		private readonly ILiftedDomain _domain;

		/** <summary>Trailing zero bit counts of the most recent lifted values.</summary> */
		private readonly int[] _recentGridShifts = new int[GridHistory];

		/** <summary>How many of each recent value's trailing zero bits lie above its own float spacing, aligned with <see cref="_recentGridShifts"/>.</summary> */
		private readonly int[] _recentGridSlack = new int[GridHistory];
		private AdaptiveRice _rice;
		private long _previous;
		private long _beforePrevious;
		private int _history;
		private ulong _firstOrderScore;
		private ulong _secondOrderScore;
		private int _gridShiftCount;
		private int _nextGridShiftSlot;

		public ExactFloatCodec(ILiftedDomain domain)
		{
			_domain = domain ?? throw new ArgumentNullException(nameof(domain));
			Reset();
		}

		#region Public Methods

		public void Reset()
		{
			_rice = new AdaptiveRice(InitialRiceMean);
			_previous = 0;
			_beforePrevious = 0;
			_history = 0;
			_firstOrderScore = 0;
			_secondOrderScore = 0;
			_gridShiftCount = 0;
			_nextGridShiftSlot = 0;
		}

		/** <summary>Determines how <paramref name="value"/> could be coded given the values committed so far.</summary> */
		public ExactFloatPlan Plan(float value)
		{
			int bits = ExactFloat.ToBits(value);
			if (!TryPredict(out long prediction) || prediction == 0 || !ExactFloat.TryLift(bits, out long lifted, out int exponent))
			{
				return new ExactFloatPlan(bits);
			}

			int step = exponent - ExactFloat.ExponentOf(prediction);
			if (Math.Abs(step) > MaxExponentStep)
			{
				return new ExactFloatPlan(bits);
			}

			// the value has to sit on the grid the decoder will assume, or the residual could not be whole
			int shift = GridShift(exponent);
			if (!IsOnGrid(lifted, shift))
			{
				return new ExactFloatPlan(bits);
			}

			// decode and rebuild residual for determinism
			long residual = Residual(prediction, lifted, shift);
			if (Reconstruct(prediction, residual, shift) != lifted)
			{
				return new ExactFloatPlan(bits);
			}

			// a residual costing more than the raw float is not coded
			ulong zigzag = ZigZag.Encode(residual);
			int k = _rice.K;
			int riceBits = AdaptiveRice.Length(zigzag, k);
			if (riceBits > RawBits)
			{
				return new ExactFloatPlan(bits);
			}

			return new ExactFloatPlan(bits, true, step, zigzag, k, riceBits);
		}

		/** <summary>Number of bits <see cref="Write"/> uses for <paramref name="plan"/> when the exponent symbol is written.</summary> */
		public static int ExplicitBits(ExactFloatPlan plan)
		{
			return SelectSymbol(plan) switch
			{
				Symbol.SameExponent => SameExponentSymbolBits + plan.RiceBits,
				Symbol.ExponentStep => ExponentStepSymbolBits + plan.RiceBits,
				_ => WorstCaseBits,
			};
		}

		/**
		 * <summary>
		 * Writes a planned value.
		 * </summary>
		 * <param name="plan">Plan returned by <see cref="Plan"/> for this value.</param>
		 * <param name="implicitExponent">True to skip the exponent symbol.</param>
		 * <param name="sink">Destination for the bits.</param>
		 */
		public void Write(ExactFloatPlan plan, bool implicitExponent, IBitSink sink)
		{
			WriteSymbolAndResidual(plan, implicitExponent, sink);

			// call Commit() here to preserve the invariant that decoder and encoder agree on the history state
			Commit(plan.Bits);
		}

		private void WriteSymbolAndResidual(ExactFloatPlan plan, bool implicitExponent, IBitSink sink)
		{
			if (implicitExponent)
			{
				AdaptiveRice.Write(sink, plan.Residual, plan.RiceK);
				return;
			}

			switch (SelectSymbol(plan))
			{
				case Symbol.SameExponent:
					sink.WriteFlag(false);
					AdaptiveRice.Write(sink, plan.Residual, plan.RiceK);
					break;
				case Symbol.ExponentStep:
					sink.WriteFlag(true);
					sink.WriteFlag(true);
					sink.WriteFlag(plan.ExponentStep > 0);
					AdaptiveRice.Write(sink, plan.Residual, plan.RiceK);
					break;
				default:
					sink.WriteFlag(true);
					sink.WriteFlag(false);
					sink.Write((uint)plan.Bits, RawBits);
					break;
			}
		}

		/**
		 * <summary>
		 * Reads a value written by <see cref="Write"/>.
		 * </summary>
		 * <returns>False if the source ran out or the bits do not describe a valid value.</returns>
		 */
		public bool TryRead(IBitSource source, bool implicitExponent, out float value)
		{
			value = 0f;
			if (implicitExponent)
			{
				return TryReadResidual(source, 0, out value);
			}

			if (!source.TryReadFlag(out bool hasExplicitSymbol))
			{
				return false;
			}

			if (!hasExplicitSymbol)
			{
				return TryReadResidual(source, 0, out value);
			}

			if (!source.TryReadFlag(out bool isExponentStep))
			{
				return false;
			}

			if (!isExponentStep)
			{
				return TryReadRaw(source, out value);
			}

			if (!source.TryReadFlag(out bool stepsUp))
			{
				return false;
			}

			return TryReadResidual(source, stepsUp ? MaxExponentStep : -MaxExponentStep, out value);
		}

		#endregion

		#region Helpers

		/** <summary>Folds a coded value into the history that predicts and codes the next one.</summary> */
		private void Commit(int bits)
		{
			UpdateRice(bits);

			long current = ExactFloat.LiftForHistory(bits);
			if (_history >= SecondOrderHistory)
			{
				_firstOrderScore = DecayScore(_firstOrderScore, Distance(current, _previous));
				_secondOrderScore = DecayScore(_secondOrderScore, Distance(current, SecondOrderPrediction()));
			}

			_beforePrevious = _previous;
			_previous = current;
			_history = Math.Min(_history + 1, SecondOrderHistory);
			RecordGridShift(bits, current);
		}

		private static Symbol SelectSymbol(ExactFloatPlan plan)
		{
			if (!plan.HasResidual)
			{
				return Symbol.Raw;
			}

			if (plan.ExponentStep == 0)
			{
				return Symbol.SameExponent;
			}

			return ExponentStepSymbolBits + plan.RiceBits < WorstCaseBits ? Symbol.ExponentStep : Symbol.Raw;
		}

		private bool TryReadRaw(IBitSource source, out float value)
		{
			if (!source.TryRead(RawBits, out ulong raw))
			{
				value = 0f;
				return false;
			}

			int bits = (int)(uint)raw;
			value = ExactFloat.FromBits(bits);
			Commit(bits);
			return true;
		}

		private bool TryReadResidual(IBitSource source, int exponentStep, out float value)
		{
			value = 0f;
			if (!TryPredict(out long prediction) || prediction == 0)
			{
				return false;
			}

			int exponent = ExactFloat.ExponentOf(prediction) + exponentStep;
			if (exponent < ExactFloat.MinExponent || exponent > ExactFloat.MaxExponent)
			{
				return false;
			}

			if (!AdaptiveRice.TryRead(source, _rice.K, out ulong zigzag))
			{
				return false;
			}

			long lifted = Reconstruct(prediction, ZigZag.Decode(zigzag), GridShift(exponent));
			if (!ExactFloat.TryUnlift(lifted, exponent, out int bits))
			{
				return false;
			}

			value = ExactFloat.FromBits(bits);
			Commit(bits);
			return true;
		}

		private void UpdateRice(int bits)
		{
			if (!TryPredict(out long prediction) || !ExactFloat.TryLift(bits, out long lifted, out int exponent))
			{
				return;
			}

			int shift = GridShift(exponent);
			if (!IsOnGrid(lifted, shift))
			{
				shift = ExactFloat.UlpShift(exponent);
			}

			long residual = Residual(prediction, lifted, shift);
			if (Reconstruct(prediction, residual, shift) == lifted)
			{
				_rice.Update(ZigZag.Encode(residual));
			}
		}

		private bool TryPredict(out long prediction)
		{
			prediction = 0;
			if (_history == 0)
			{
				return false;
			}
			bool useSecondOrder = _history >= SecondOrderHistory && _secondOrderScore < _firstOrderScore;
			prediction = _domain.Normalize(useSecondOrder ? SecondOrderPrediction() : _previous);
			return true;
		}

		private long SecondOrderPrediction()
		{
			// where the value lands if it keeps its rate, 2a - b, clamped so a saturated history cannot overflow
			return Math.Clamp(2 * _previous - _beforePrevious, -PredictionLimit, PredictionLimit);
		}

		private int GridShift(int exponent)
		{
			int ulpShift = ExactFloat.UlpShift(exponent);
			if (_gridShiftCount < MinGridHistory)
			{
				return ulpShift;
			}

			int sharedGrid = ExactFloat.LiftShift; // absolute grid shift
			int sharedSlack = ExactFloat.LiftShift; // delta
			for (int i = 0; i < _gridShiftCount; i++)
			{
				sharedGrid = Math.Min(sharedGrid, _recentGridShifts[i]);
				sharedSlack = Math.Min(sharedSlack, _recentGridSlack[i]);
			}

			// take the more cautious guess
			return Math.Max(ulpShift, Math.Min(sharedGrid, ulpShift + sharedSlack));
		}

		private void RecordGridShift(int bits, long lifted)
		{
			int gridShift = Math.Min(ExactFloat.TrailingZeroCount(lifted), ExactFloat.LiftShift);
			int slack = ExactFloat.TryLift(bits, out _, out int exponent) ? gridShift - ExactFloat.UlpShift(exponent) : ExactFloat.LiftShift;

			_recentGridShifts[_nextGridShiftSlot] = gridShift;
			_recentGridSlack[_nextGridShiftSlot] = Math.Max(0, slack);
			_nextGridShiftSlot = (_nextGridShiftSlot + 1) % GridHistory;
			_gridShiftCount = Math.Min(_gridShiftCount + 1, GridHistory);
		}

		private static bool IsOnGrid(long lifted, int shift)
		{
			// on a grid of 2^shift when the low shift bits are all zero
			return (ExactFloat.Magnitude(lifted) & BitStreamExtensions.LowMask(shift)) == 0;
		}

		private long Residual(long prediction, long lifted, int shift)
		{
			// value and rounded prediction are both multiples of 2^shift so the difference divides exactly
			// a residual of whole grid steps carries no rounding error
			return _domain.Difference(lifted, RoundToGrid(prediction, shift)) >> shift;
		}

		private long Reconstruct(long prediction, long residual, int shift)
		{
			return _domain.Normalize(RoundToGrid(prediction, shift) + (residual << shift));
		}

		private static long RoundToGrid(long value, int shift)
		{
			// snap the prediction onto the value's grid
			return shift == 0 ? value : ((value + (1L << (shift - 1))) >> shift) << shift;
		}

		private ulong Distance(long a, long b)
		{
			return ExactFloat.Magnitude(_domain.Difference(a, b));
		}

		private static ulong DecayScore(ulong score, ulong error)
		{
			// running average forgetting a quarter per value
			// errors are scaled down and capped to account for outliers
			return score - (score >> ScoreDecayShift) + Math.Min(error >> ScoreErrorShift, ScoreErrorCap);
		}

		#endregion
	}
}
