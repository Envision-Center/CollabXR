using System;
using CollabXR.Objects;
using CollabXR.UI;
using UnityEngine;

namespace CollabXR
{
	/**
	 * <summary>
	 * Interface for spawning custom context menus when interacting with an object.
	 * Should primarily be placed on the network object itself.
	 * </summary>
	 */
	public interface IContextProvider
	{
		string TabName { get; }

		CollabContext AddTab(GameObject obj, CollabContextMenu menu);

		GameObject GetContextPrefab() => null;

		GameObject SelfObject => null;
	}
}
