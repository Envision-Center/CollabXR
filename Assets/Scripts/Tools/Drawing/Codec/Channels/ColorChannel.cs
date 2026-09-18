using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * Stroke channel of <see cref="Color32"/> values, coded as repeats, hue steps, hue or gray codes,
	 * or raw RGBA for anything else.
	 * Strokes are coded as deltas instead of absolutes to minimize bit representations with these prefixes:
	 * <list type="bullet">
	 * <item>0:  same color as previous.</item>
	 * <item>10: one step around the hue ring.</item>
	 * <item>11: a full color code.</item>
	 * </list>
	 * </summary>
	 */
	public sealed class ColorChannel : StrokeChannel<Color32>
	{
		private const string ChannelName = "Color";
		private const int RawColorBits = 32; // four channels of eight bits

		private const int SymbolBits = 2; // budget for prefixes
		private const ulong InitialRiceMean = 32; // k

		// where each channel sits once a color is packed into one 32bit field (red is the low byte)
		private const int GreenShift = 8;
		private const int BlueShift = 16;
		private const int AlphaShift = 24;
		private const uint ByteMask = byte.MaxValue;

		private AdaptiveRice _rice;
		private bool _hasPrevious;
		private Color32 _previous;
		private int _previousCode;

		public override string Name => ChannelName;

		public override int WorstCaseBitsPerPoint => SymbolBits + HueRing.CodeBits + RawColorBits;

		public ColorChannel()
		{
			ResetModel();
		}

		#region Public Methods
		public override void ResetModel()
		{
			_rice = new AdaptiveRice(InitialRiceMean);
			_hasPrevious = false;
			_previous = default;
			_previousCode = HueRing.NoCode;
		}

		#endregion


		#region Helpers

		protected override void Encode(Color32 value, IBitSink sink)
		{
			if (_hasPrevious && SameColor(value, _previous))
			{
				sink.WriteFlag(false);
				return;
			}

			sink.WriteFlag(true);
			int code = HueRing.ToCode(value);

			if (TryPlanHueStep(code, out ulong step, out int k))
			{
				sink.WriteFlag(false);
				AdaptiveRice.Write(sink, step, k);
			}
			else
			{
				sink.WriteFlag(true);
				WriteCode(value, code, sink);
			}

			Commit(value, code);
		}

		protected override bool TryDecode(IBitSource source, out Color32 value)
		{
			value = _previous;
			if (!source.TryReadFlag(out bool changed))
			{
				return false;
			}

			if (!changed)
			{
				return _hasPrevious;
			}

			if (!source.TryReadFlag(out bool isAbsolute))
			{
				return false;
			}

			bool read = isAbsolute ? TryReadCode(source, out value) : TryReadHueStep(source, out value);
			if (read)
			{
				Commit(value, HueRing.ToCode(value));
			}

			return read;
		}

		private bool TryPlanHueStep(int code, out ulong step, out int k)
		{
			step = 0;
			k = _rice.K;
			if (!_hasPrevious || !HueRing.IsHue(code) || !HueRing.IsHue(_previousCode))
			{
				return false;
			}

			// sweeps take this path and absolute doesnt

			step = ZigZag.Encode(HueRing.Delta(_previousCode, code));
			return AdaptiveRice.Length(step, k) < HueRing.CodeBits;
		}

		private static void WriteCode(Color32 value, int code, IBitSink sink)
		{
			if (HueRing.IsColor(code))
			{
				sink.Write((ulong)code, HueRing.CodeBits);
				return;
			}

			// just in case we add saturation/value modification to the brushes (i cant really simplify that more than 32 bit without being lossy)
			sink.Write((ulong)HueRing.EscapeCode, HueRing.CodeBits);
			sink.Write(PackRgba(value), RawColorBits);
		}

		private bool TryReadHueStep(IBitSource source, out Color32 value)
		{
			value = default;
			if (!_hasPrevious || !HueRing.IsHue(_previousCode) || !AdaptiveRice.TryRead(source, _rice.K, out ulong step))
			{
				return false;
			}

			long delta = ZigZag.Decode(step);
			if (delta < -HueRing.RingSize / 2 || delta >= HueRing.RingSize / 2) // nothing can be greater than a half turn unless something messed up
			{
				return false;
			}
			value = HueRing.FromCode(HueRing.Step(_previousCode, (int)delta));
			return true;
		}

		private static bool TryReadCode(IBitSource source, out Color32 value)
		{
			value = default;
			if (!source.TryRead(HueRing.CodeBits, out ulong code))
			{
				return false;
			}

			if (HueRing.IsColor((int)code))
			{
				value = HueRing.FromCode((int)code);
				return true;
			}

			if ((int)code != HueRing.EscapeCode || !source.TryRead(RawColorBits, out ulong rgba))
			{
				return false;
			}

			value = UnpackRgba(rgba);
			return true;
		}

		private void Commit(Color32 value, int code)
		{
			if (_hasPrevious && HueRing.IsHue(code) && HueRing.IsHue(_previousCode))
			{
				_rice.Update(ZigZag.Encode(HueRing.Delta(_previousCode, code)));
			}

			_hasPrevious = true;
			_previous = value;
			_previousCode = code;
		}

		private static bool SameColor(Color32 a, Color32 b)
		{
			return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
		}

		private static ulong PackRgba(Color32 color)
		{
			return color.r | ((ulong)color.g << GreenShift) | ((ulong)color.b << BlueShift) | ((ulong)color.a << AlphaShift);
		}

		private static Color32 UnpackRgba(ulong rgba)
		{
			return new Color32((byte)(rgba & ByteMask), (byte)((rgba >> GreenShift) & ByteMask), (byte)((rgba >> BlueShift) & ByteMask), (byte)((rgba >> AlphaShift) & ByteMask));
		}

		#endregion
	}
}
