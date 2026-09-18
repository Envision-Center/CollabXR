namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Per-stroke float stored as its raw bits.
	 * </summary>
	 */
	public sealed class FloatField : IStrokeField
	{
		public float Value { get; set; }

		public string Name { get; }

		public int BitCount => ExactFloatCodec.RawBits;

		public FloatField(string name)
		{
			Name = name;
		}

		#region Public Methods

		public void Reset()
		{
			Value = 0f;
		}

		public void Write(IBitSink sink)
		{
			sink.Write((uint)ExactFloat.ToBits(Value), BitCount);
		}

		/** <inheritdoc /> */
		public bool TryRead(IBitSource source)
		{
			if (!source.TryRead(BitCount, out ulong bits))
			{
				return false;
			}

			Value = ExactFloat.FromBits((int)(uint)bits);
			return true;
		}

		#endregion
	}
}
