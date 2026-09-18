using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Exact 11-bit codes for the 1530 fully saturated <see cref="Color32"/> hues and the 256 opaque grays.
	 * </summary>
	 */
	public static class HueRing
	{
		// a saturated color traces six edges of the RGB cube at 255 steps each so 1530 colors
		public const int CodeBits = 11; // 2^11 is 2048 > 1530 > 1024
		public const int NoCode = -1; // returned for a color that is neither a hue nor a gray
		public const int EdgeLength = byte.MaxValue; // 255 steps along an edge
		public const int EdgeCount = 6; // of the RGB cube
		public const int RingSize = EdgeCount * EdgeLength; // = 1530

		// the 256 grays fit in the space left over; the last code is kept as an escape for any other color
		public const int GrayStart = RingSize; // grays begin where the ring ends
		public const int GrayCount = byte.MaxValue + 1; // black through white
		public const int EscapeCode = (1 << CodeBits) - 1; // = 2047

		private const int RedToYellowEdge = 0;
		private const int YellowToGreenEdge = 1;
		private const int GreenToCyanEdge = 2;
		private const int CyanToBlueEdge = 3;
		private const int BlueToMagentaEdge = 4;

		#region Public Methods

		/** <summary>True if <paramref name="code"/> is a saturated hue.</summary> */
		public static bool IsHue(int code)
		{
			return code >= 0 && code < RingSize;
		}

		/** <summary>True if <paramref name="code"/> is a hue or a gray.</summary> */
		public static bool IsColor(int code)
		{
			return code >= 0 && code < GrayStart + GrayCount;
		}

		/**
		 * <summary>
		 * Finds the code for <paramref name="color"/>.
		 * </summary>
		 * <returns>The code, or <see cref="NoCode"/> if the color is neither an opaque saturated hue nor an opaque gray.</returns>
		 */
		public static int ToCode(Color32 color)
		{
			byte max = byte.MaxValue;
			if (color.a != max)
			{
				return NoCode;
			}

			if (color.r == color.g && color.g == color.b)
			{
				return GrayStart + color.r;
			}

			if (color.r == max && color.b == 0)
			{
				return EdgeStart(RedToYellowEdge) + color.g;
			}

			if (color.g == max && color.b == 0)
			{
				return EdgeStart(YellowToGreenEdge) + max - color.r;
			}

			if (color.g == max && color.r == 0)
			{
				return EdgeStart(GreenToCyanEdge) + color.b;
			}

			if (color.b == max && color.r == 0)
			{
				return EdgeStart(CyanToBlueEdge) + max - color.g;
			}

			if (color.b == max && color.g == 0)
			{
				return EdgeStart(BlueToMagentaEdge) + color.r;
			}

			if (color.r == max && color.g == 0)
			{
				return (RingSize - color.b) % RingSize;
			}

			return NoCode;
		}

		/** <summary>The color for <paramref name="code"/>. Only valid when <see cref="IsColor"/> is true.</summary> */
		public static Color32 FromCode(int code)
		{
			byte max = byte.MaxValue;
			if (!IsHue(code))
			{
				byte gray = (byte)(code - GrayStart);
				return new Color32(gray, gray, gray, max);
			}

			// quotient = edge, remainder = interpolated value
			int edge = code / EdgeLength;
			byte rising = (byte)(code - EdgeStart(edge));
			byte falling = (byte)(max - rising);

			return edge switch
			{
				RedToYellowEdge => new Color32(max, rising, 0, max),
				YellowToGreenEdge => new Color32(falling, max, 0, max),
				GreenToCyanEdge => new Color32(0, max, rising, max),
				CyanToBlueEdge => new Color32(0, falling, max, max),
				BlueToMagentaEdge => new Color32(rising, 0, max, max),
				_ => new Color32(max, 0, falling, max),
			};
		}

		/** <summary>Shortest signed number of steps around the ring from hue <paramref name="from"/> to hue <paramref name="to"/>.</summary> */
		public static int Delta(int from, int to)
		{
			int forward = Wrap(to - from);
			return forward >= RingSize / 2 ? forward - RingSize : forward;
		}

		/** <summary>The hue <paramref name="delta"/> steps around the ring from <paramref name="from"/>.</summary> */
		public static int Step(int from, int delta)
		{
			return Wrap(from + delta);
		}

		#endregion

		#region Helpers

		private static int EdgeStart(int edge)
		{
			return edge * EdgeLength;
		}

		/** <summary>Brings any number of steps back onto the ring, counting from red.</summary> */
		private static int Wrap(int code)
		{
			int remainder = code % RingSize;
			return remainder < 0 ? remainder + RingSize : remainder;
		}

		#endregion
	}
}
