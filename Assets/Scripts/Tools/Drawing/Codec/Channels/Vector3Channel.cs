using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke channel of <see cref="Vector3"/> values.
	 * </summary>
	 */
	public class Vector3Channel : StrokeChannel<Vector3>
	{
		private readonly ExactVector3Codec _codec;

		public override string Name { get; }

		public override int WorstCaseBitsPerPoint => ExactVector3Codec.WorstCaseBits;

		public Vector3Channel(string name, ILiftedDomain domain)
		{
			Name = name;
			_codec = new ExactVector3Codec(domain);
		}

		#region Public Methods

		public override void ResetModel()
		{
			_codec.Reset();
		}

		#endregion

		#region Helpers

		protected override void Encode(Vector3 value, IBitSink sink)
		{
			_codec.Encode(value, sink);
		}

		protected override bool TryDecode(IBitSource source, out Vector3 value)
		{
			return _codec.TryDecode(source, out value);
		}

		#endregion
	}
}
