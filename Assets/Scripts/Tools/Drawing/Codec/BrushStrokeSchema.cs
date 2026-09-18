using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke schema for brush ribbons: a width, then a position, Euler rotation and color per point.
	 * </summary>
	 */
	public sealed class BrushStrokeSchema : StrokeSchema
	{
		// modify this to adjust the schema's bit rate
		public const int StreamWords = 192; // 6144 bits, which holds about 47 points i think
		private const string WeightName = "Weight";

		public FloatField Weight { get; } = new(WeightName);

		public PositionChannel Positions { get; } = new();
		public EulerChannel EulerAngles { get; } = new();
		public ColorChannel Colors { get; } = new();

		public BrushStrokeSchema()
		{
			AddField(Weight);
			AddChannel(Positions);
			AddChannel(EulerAngles);
			AddChannel(Colors);
		}

		#region Public Methods

		public void AddPoint(Vector3 position, Vector3 eulerAngles, Color32 color)
		{
			Positions.Values.Add(position);
			EulerAngles.Values.Add(eulerAngles);
			Colors.Values.Add(color);
		}

		public void SetLastPoint(Vector3 position, Vector3 eulerAngles)
		{
			int last = PointCount - 1;
			Positions.Values[last] = position;
			EulerAngles.Values[last] = eulerAngles;
		}

		#endregion
	}
}
