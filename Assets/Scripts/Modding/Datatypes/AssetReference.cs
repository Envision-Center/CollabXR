using System;

namespace CollabXR.ModLoader
{
	internal interface IAssetReference
	{
		Guid modUuid { get; set; }

		Guid assetUuid { get; set; }

		Guid assetReferenceUuid { get; set; }

		object value { get; set; }

		void InvokeOnReloadedEvent();

		bool InvokeOnRequestReleaseEvent();
	}

	/// <summary>
	/// Class <c>AssetReference</c> contains a reference to an external asset, as well as event providers for requesting asset reloads and requesting asset release.
	/// Do NOT instantiate this class yourself otherwise the Mod Loader can break in fun ways.
	/// </summary>
	public class AssetReference<T> : IAssetReference
	{
		/// <summary>
		/// Field <c>Value</c> contains the asset data.
		/// Note that you can mutate this reference, but at any point it could be changed externally in the case of a mod reload.
		/// (If a change occurs, <c>OnReloadedEvent</c> will be fired to notify you)
		/// </summary>
		public T Value => (T)this.value;

		///// <summary>
		///// This is internal data used for the mod loader's reference counter.
		///// Do NOT modify it under any circumstances.
		///// </summary>
		public Guid modUuid { get; set; }

		///// <summary>
		///// This is internal data used for the mod loader's reference counter.
		///// Do NOT modify it under any circumstances.
		///// </summary>
		public Guid assetUuid { get; set; }

		///// <summary>
		///// This is internal data used for the mod loader's reference counter.
		///// Do NOT modify it under any circumstances.
		///// </summary>
		public Guid assetReferenceUuid { get; set; }

		///// <summary>
		///// This is internal data used for the mod loader's reference counter.
		///// Do NOT modify it under any circumstances.
		///// </summary>
		public object value { get; set; }

		public delegate void OnReloadedEventHandler();
		public delegate bool OnRequestReleaseEventHandler();

		/// <summary>
		/// Event <c>OnReloadedEvent</c> informs the asset user that the asset was re-loaded and now has a new value.
		/// The new value will be placed in <c>Value</c> before this event is fired.
		/// Not registering this event has no effect on external reloads and <c>Value</c> will change regardless.
		/// </summary>
		public event OnReloadedEventHandler OnReloadedEvent;

		/// <summary>
		/// This is an internal function used for the mod loader's reference counter.
		/// Do NOT call it under any circumstances.
		/// </summary>
		public void InvokeOnReloadedEvent()
		{
			OnReloadedEvent?.Invoke();
		}

		/// <summary>
		/// Event <c>OnRequestReleaseEvent</c> asks the asset user if the asset is okay to release early.
		/// If you return <c>true</c> from this event, assume your asset reference is now gone (this includes removing the need to manually unload the release the asset reference via <c>ModLoader.ReleaseAsset()</c>) and request a new one if needed.
		/// Not registering this event will force the mod loader assume that the asset is always in use and will reject any early release requests.
		/// Only one subscriber should be used for this event. In the case of multiple subscribers, they must all agree to release the reference.
		/// Each instance of this loaded asset will have a seperate event so you don't have to fight with other classes to get the object released.
		/// </summary>
		public event OnRequestReleaseEventHandler OnRequestReleaseEvent;

		/// <summary>
		/// This is an internal function used for the mod loader's reference counter.
		/// Do NOT call it under any circumstances.
		/// </summary>
		public bool InvokeOnRequestReleaseEvent()
		{
			if (OnRequestReleaseEvent == null)
			{
				return false;
			}

			bool canRelease = true;

			foreach (Delegate registeredDelegate in OnRequestReleaseEvent.GetInvocationList())
			{
				if (!(bool)registeredDelegate.Method.Invoke(registeredDelegate.Target, Array.Empty<object>()))
				{
					canRelease = false;
				}
			}

			return canRelease;
		}

		internal AssetPointerLoadTask<T> LoadSelf()
		{
			return new AssetPointerLoadTask<T>(this);
		}

		// Finalizer to make sure we release this asset just in case :)
		//~AssetReference()
		//{
		//    ModLoader.ReleaseAsset(this);
		//}
	}
}
