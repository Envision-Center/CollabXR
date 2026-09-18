namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * One per point property of a stroke.
	 * </summary>
	 */
	public interface IStrokeChannel
	{
		/** <summary>Human-readable name (debugs).</summary> */
		string Name { get; }

		/** <summary>Number of points currently held.</summary> */
		int Count { get; }

		/** <summary>Most bits a single point can take in this channel.</summary> */
		int WorstCaseBitsPerPoint { get; }

		void ResetModel();

		/** <summary>Compresses the point at <paramref name="index"/>.</summary> */
		void Compress(int index, IBitSink sink);

		/**
		 * <summary>
		 * Decompresses the next point and appends it.
		 * </summary>
		 * <returns>False, appending nothing, if the bits are missing or invalid.</returns>
		 */
		bool TryDecompressNext(IBitSource source);

		/** <summary>Removes every point after the first <paramref name="count"/>.</summary> */
		void Truncate(int count);
	}
}
