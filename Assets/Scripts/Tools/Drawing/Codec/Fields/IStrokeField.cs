namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * One per stroke value, such as brush width, stored once in the stroke header.
	 * </summary>
	 */
	public interface IStrokeField
	{
		string Name { get; }

		int BitCount { get; }

		void Reset();
		void Write(IBitSink sink);
		bool TryRead(IBitSource source);
	}
}
