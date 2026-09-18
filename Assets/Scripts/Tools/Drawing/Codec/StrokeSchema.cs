using System;
using System.Collections.Generic;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Ordered set of per-stroke fields and per-point channels that make up one packed stroke.
	 * </summary>
	 */
	public class StrokeSchema
	{
		public const int MaxPoints = 128;
		public const int CountBits = 8;

		private readonly List<IStrokeField> _fields = new();
		private readonly List<IStrokeChannel> _channels = new();

		public IReadOnlyList<IStrokeField> Fields => _fields;

		public IReadOnlyList<IStrokeChannel> Channels => _channels;

		public int PointCount => _channels.Count == 0 ? 0 : _channels[0].Count;

		/** <summary>Bits taken by the point count and every field.</summary> */
		public int HeaderBits
		{
			get
			{
				int bits = CountBits;
				foreach (IStrokeField field in _fields)
				{
					bits += field.BitCount;
				}

				return bits;
			}
		}

		/** <summary>Most bits a single point can take across every channel.</summary> */
		public int WorstCaseBitsPerPoint
		{
			get
			{
				int bits = 0;
				foreach (IStrokeChannel channel in _channels)
				{
					bits += channel.WorstCaseBitsPerPoint;
				}

				return bits;
			}
		}

		#region Public Methods

		public void AddField(IStrokeField field)
		{
			_fields.Add(field ?? throw new ArgumentNullException(nameof(field)));
		}

		public void AddChannel(IStrokeChannel channel)
		{
			_channels.Add(channel ?? throw new ArgumentNullException(nameof(channel)));
		}

		/** <summary>True if every channel holds the same number of points.</summary> */
		public bool HasConsistentPointCounts()
		{
			return FindInconsistentChannel() == null;
		}

		/** <summary>The first channel whose point count is different from the previous ones, null if not found.</summary> */
		public IStrokeChannel FindInconsistentChannel()
		{
			foreach (IStrokeChannel channel in _channels)
			{
				if (channel.Count != PointCount)
				{
					return channel;
				}
			}

			return null;
		}

		public void ResetModels()
		{
			foreach (IStrokeChannel channel in _channels)
			{
				channel.ResetModel();
			}
		}

		public void ResetFields()
		{
			foreach (IStrokeField field in _fields)
			{
				field.Reset();
			}
		}

		public void TruncatePoints(int count)
		{
			foreach (IStrokeChannel channel in _channels)
			{
				channel.Truncate(count);
			}
		}

		/**
		 * <summary>
		 * Worst case number of points that are guaranteed to still fit.
		 * </summary>
		 * <param name="usedBits">Bits the stroke currently takes.</param>
		 * <param name="capacityBits">Bits available in total.</param>
		 */
		public int GetCapacityRemaining(int usedBits, int capacityBits)
		{
			int pointsLeft = MaxPoints - PointCount;
			int worstCasePointsLeft = Math.Max(0, capacityBits - usedBits) / WorstCaseBitsPerPoint;
			return Math.Max(0, Math.Min(pointsLeft, worstCasePointsLeft));
		}

		#endregion
	}
}
