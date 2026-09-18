using System.Collections.Generic;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Base for a stroke channel that stores its raw values in a list and codes them one at a time.
	 * </summary>
	 * <typeparam name="T">Type of the stored value.</typeparam>
	 */
	public abstract class StrokeChannel<T> : IStrokeChannel
	{
		public List<T> Values { get; } = new(StrokeSchema.MaxPoints);

		public abstract string Name { get; }

		public int Count => Values.Count;

		public abstract int WorstCaseBitsPerPoint { get; }

		#region Public Methods

		public abstract void ResetModel();

		public void Compress(int index, IBitSink sink)
		{
			Encode(Values[index], sink);
		}

		public bool TryDecompressNext(IBitSource source)
		{
			if (!TryDecode(source, out T value))
			{
				return false;
			}

			Values.Add(value);
			return true;
		}

		public void Truncate(int count)
		{
			if (count < Values.Count)
			{
				Values.RemoveRange(count, Values.Count - count);
			}
		}

		#endregion


		#region Helpers

		protected abstract void Encode(T value, IBitSink sink);

		protected abstract bool TryDecode(IBitSource source, out T value);

		#endregion
	}
}
