using System;

namespace CollabXR.Tools.Drawing
{
	/**
	 * <summary>
	 * A strokes raw values synced with packed words <see cref="IStrokeWordStore"/>.
	 * </summary>
	 * <typeparam name="TSchema">Schema describing the stroke's fields and channels.</typeparam>
	 */
	public sealed class PackedStroke<TSchema>
		where TSchema : StrokeSchema, new()
	{
		private readonly RecordingBitSink _compressed = new();
		private readonly BitPacker _packer = new();
		private readonly BitUnpacker _unpacker = new();

		private readonly int[] _storedWords;
		private readonly int[] _packedWords;

		/** <summary>Raw values of the stroke.</summary> */
		public TSchema Schema { get; } = new();

		/** <summary>Bits the stroke took.</summary> */
		public int UsedBits { get; private set; }

		/** <summary>Bits available.</summary> */
		public int CapacityBits => _storedWords.Length * BitStreamExtensions.WordBits;

		/** <summary>Number of points that are guaranteed to still fit.</summary> */
		public int CapacityRemaining => Schema.GetCapacityRemaining(UsedBits, CapacityBits);

		public PackedStroke(int wordCount)
		{
			_storedWords = new int[wordCount];
			_packedWords = new int[wordCount];
		}

		#region Public Methods

		/**
		 * <summary>
		 * Reads every word from <paramref name="store"/> and decodes them into <see cref="Schema"/>.
		 * </summary>
		 * <exception cref="ArgumentException">The store is smaller than this stroke.</exception>
		 */
		public void Load(IStrokeWordStore store)
		{
			RequireCapacity(store);
			for (int i = 0; i < _storedWords.Length; i++)
			{
				_storedWords[i] = store.Get(i);
			}

			UsedBits = StrokeCodec.Decode(_storedWords, Schema, _unpacker);
		}

		/**
		 * <summary>
		 * Encodes <see cref="Schema"/> and writes only the words that differ from the last load or save.
		 * </summary>
		 * <returns>False, leaving <paramref name="store"/> untouched, if the stroke does not fit.</returns>
		 * <exception cref="ArgumentException">The store is smaller than this stroke.</exception>
		 */
		public bool Save(IStrokeWordStore store)
		{
			RequireCapacity(store);

			int bits = StrokeCodec.Encode(Schema, _compressed, _packer, _packedWords);
			if (bits < 0)
			{
				return false;
			}

			// writes only what differs from the last load or save to minimize network traffic
			for (int i = 0; i < _packedWords.Length; i++)
			{
				if (_packedWords[i] != _storedWords[i])
				{
					store.Set(i, _packedWords[i]);
					_storedWords[i] = _packedWords[i];
				}
			}

			UsedBits = bits;
			return true;
		}

		#endregion

		#region Helpers

		/* <summary>Ensures the store has enough capacity (throws because this should be invariant)</summary> */
		private void RequireCapacity(IStrokeWordStore store)
		{
			if (store.Length < _storedWords.Length)
			{
				throw new ArgumentException($"Store holds {store.Length} words but the stroke needs {_storedWords.Length}.", nameof(store));
			}
		}

		#endregion
	}
}
