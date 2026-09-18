namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Number line that lifted float values are predicted and compared on.
	 * </summary>
	 */
	public interface ILiftedDomain
	{
		/** <summary>Maps <paramref name="lifted"/> onto the domain's canonical range.</summary> */
		long Normalize(long lifted);

		/** <summary>Signed distance from <paramref name="reference"/> to <paramref name="lifted"/> as the domain measures it.</summary> */
		long Difference(long lifted, long reference);
	}
}
