using System;
using System.Text;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Converts a <see cref="StrokeSchema"/> to packed words and back.
	 * </summary>
	 */
	public static class StrokeCodec
	{
		#region Public Methods

		/**
		 * <summary>
		 * Compresses the schema's raw values into bit fields: the point count, every field, then each point's channels in order.
		 * </summary>
		 * <exception cref="InvalidOperationException">Channels hold different point counts, or more than <see cref="StrokeSchema.MaxPoints"/>.</exception>
		 */
		public static void Compress(StrokeSchema schema, RecordingBitSink output)
		{
			int count = schema.PointCount;
			IStrokeChannel inconsistent = schema.FindInconsistentChannel();
			if (inconsistent != null)
			{
				throw new InvalidOperationException($"Channel {inconsistent.Name} holds {inconsistent.Count} points but the stroke holds {count}.");
			}

			if (count > StrokeSchema.MaxPoints)
			{
				throw new InvalidOperationException($"A stroke holds at most {StrokeSchema.MaxPoints} points but this one holds {count}.");
			}

			output.Clear();
			schema.ResetModels();
			output.Write((ulong)count, StrokeSchema.CountBits);

			foreach (IStrokeField field in schema.Fields)
			{
				field.Write(output);
			}

			// point by point rather than channel by channel so the stream only grows at the end
			for (int i = 0; i < count; i++)
			{
				foreach (IStrokeChannel channel in schema.Channels)
				{
					channel.Compress(i, output);
				}
			}
		}

		/**
		 * <summary>
		 * Compresses <paramref name="schema"/> and reports what each channel spent per point, for tuning and debugging.
		 * </summary>
		 * <returns>A line in format like <c>Position 62.8, EulerAngles 55.7, Color 2.0</c>, or an empty string for an empty stroke.</returns>
		 */
		public static string DescribeChannelBits(StrokeSchema schema, RecordingBitSink scratch)
		{
			int points = schema.PointCount;
			if (points == 0)
			{
				return string.Empty;
			}

			scratch.Clear();
			schema.ResetModels();
			int[] totals = new int[schema.Channels.Count];
			for (int point = 0; point < points; point++)
			{
				for (int channel = 0; channel < schema.Channels.Count; channel++)
				{
					int before = scratch.BitCount;
					schema.Channels[channel].Compress(point, scratch);
					totals[channel] += scratch.BitCount - before;
				}
			}

			StringBuilder description = new();
			for (int channel = 0; channel < totals.Length; channel++)
			{
				description.Append(channel == 0 ? string.Empty : ", ");
				description.Append($"{schema.Channels[channel].Name} {totals[channel] / (double)points:F1}");
			}

			return description.ToString();
		}

		/**
		 * <summary>
		 * Packs compressed bit fields into <paramref name="words"/>, zeroing every unused bit.
		 * </summary>
		 * <returns>Number of bits used, or -1 if they do not fit.</returns>
		 */
		public static int Pack(RecordingBitSink compressed, BitPacker packer, int[] words)
		{
			// checked before Reset() since that clears the buffer
			if (compressed.BitCount > words.Length * BitStreamExtensions.WordBits)
			{
				return -1;
			}

			packer.Reset(words);
			compressed.CopyTo(packer);
			return packer.BitPosition;
		}

		/**
		 * <summary>
		 * Compresses then packs <paramref name="schema"/> into <paramref name="words"/>.
		 * </summary>
		 * <returns>Number of bits used, or -1 if the stroke does not fit.</returns>
		 */
		public static int Encode(StrokeSchema schema, RecordingBitSink compressed, BitPacker packer, int[] words)
		{
			Compress(schema, compressed);
			return Pack(compressed, packer, words);
		}

		/**
		 * <summary>
		 * Replaces the schema's contents with the stroke packed in <paramref name="words"/>.
		 * </summary>
		 * <returns>Number of bits consumed by the header and every point that was read.</returns>
		 */
		public static int Decode(int[] words, StrokeSchema schema, BitUnpacker unpacker)
		{
			unpacker.Reset(words);
			schema.ResetModels();
			schema.TruncatePoints(0);

			if (!unpacker.TryRead(StrokeSchema.CountBits, out ulong count) || !TryReadFields(schema, unpacker))
			{
				schema.ResetFields();
				return 0;
			}

			int points = (int)Math.Min(count, StrokeSchema.MaxPoints);

			int usedBits = unpacker.BitPosition;
			for (int i = 0; i < points; i++)
			{
				if (!TryDecompressPoint(schema, unpacker))
				{
					schema.TruncatePoints(i); //  truncating to i realigns channels if one fails partway
					break;
				}

				usedBits = unpacker.BitPosition;
			}

			return usedBits;
		}

		#endregion

		#region Helpers

		private static bool TryReadFields(StrokeSchema schema, IBitSource source)
		{
			foreach (IStrokeField field in schema.Fields)
			{
				if (!field.TryRead(source))
				{
					return false;
				}
			}

			return true;
		}

		private static bool TryDecompressPoint(StrokeSchema schema, IBitSource source)
		{
			foreach (IStrokeChannel channel in schema.Channels)
			{
				if (!channel.TryDecompressNext(source))
				{
					return false;
				}
			}

			return true;
		}

		#endregion
	}
}
