using System;
using UnityEngine;

namespace CollabXR.Colocation
{
	public static class RoomCodeScanner
	{
		public static event Action<bool> Toggled = delegate { };
		private static readonly IRoomCodeScanner scanner;

		static RoomCodeScanner()
		{
#if UNITY_EDITOR
			scanner = new DefaultRoomScanner();
#elif META_QUEST
			scanner = new MetaQuestRoomCodeScanner();
#else
			scanner = new DefaultRoomScanner();
#endif

			scanner.Toggled += OnToggled;
		}

		public static void Scan() => scanner.Scan();

		public static void Stop() => scanner.Stop();

		private static void OnToggled(bool toggled) => Toggled.Invoke(toggled);
	}

	public interface IRoomCodeScanner
	{
		public event Action<bool> Toggled;

		public void Scan();
		public void Stop();
	}

	public class DefaultRoomScanner : IRoomCodeScanner
	{
		public event Action<bool> Toggled = delegate { };

		public void Scan()
		{
			Toggled.Invoke(true);
			Debug.Log("QR scanner not implemented for this platform!");
		}

		public void Stop() => Toggled.Invoke(false);
	}
}
