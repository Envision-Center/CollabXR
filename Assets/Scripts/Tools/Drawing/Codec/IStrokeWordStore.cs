namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Fixed-length word storage a packed stroke lives in.
	 * </summary>
	 */
	public interface IStrokeWordStore
	{
		/** <summary>Number of words available.</summary> */
		int Length { get; }

		/** <summary>The word at <paramref name="index"/>.</summary> */
		int Get(int index);

		/** <summary>Overwrites the word at <paramref name="index"/>.</summary> */
		void Set(int index, int value);
	}
}
