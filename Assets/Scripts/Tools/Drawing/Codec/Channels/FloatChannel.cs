namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke channel of scalar values.
	 * </summary>
	 */
	public sealed class FloatChannel : StrokeChannel<float>
	{
		private readonly ExactFloatCodec _codec;

		public override string Name { get; }

		public override int WorstCaseBitsPerPoint => ExactFloatCodec.WorstCaseBits;

		public FloatChannel(string name)
			: this(name, LinearDomain.Instance) { }

		public FloatChannel(string name, ILiftedDomain domain)
		{
			Name = name;
			_codec = new ExactFloatCodec(domain);
		}

		#region Public Methods

		public override void ResetModel()
		{
			_codec.Reset();
		}

		#endregion

		#region Helpers

		protected override void Encode(float value, IBitSink sink)
		{
			_codec.Write(_codec.Plan(value), implicitExponent: false, sink);
		}

		protected override bool TryDecode(IBitSource source, out float value)
		{
			return _codec.TryRead(source, implicitExponent: false, out value);
		}

		#endregion
	}
}
