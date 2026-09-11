using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CollabXR.ModLoader
{
	internal class ModLoadTask
	{
		internal bool IsLoaded = false;

		internal Guid modUuid { get; set; }

		private List<ModLoadTaskAwaiter> awaiters = new();

		internal ModLoadTask(Guid modUuid)
		{
			this.modUuid = modUuid;

			ModManager.Instance.LoadMod(this);
		}

		internal void NotifyModReady()
		{
			IsLoaded = true;

			foreach (ModLoadTaskAwaiter awaiter in awaiters)
			{
				awaiter.NotifyModReady();
			}
		}

		public ModLoadTaskAwaiter GetAwaiter()
		{
			ModLoadTaskAwaiter newAwaiter = new ModLoadTaskAwaiter(this);

			awaiters.Add(newAwaiter);

			return newAwaiter;
		}
	}

	internal class ModLoadTaskAwaiter : INotifyCompletion
	{
		ModLoadTask activeModLoadTask;

		Action continuationAction;

		public ModLoadTaskAwaiter(ModLoadTask activeModLoadTask)
		{
			this.activeModLoadTask = activeModLoadTask;

			this.IsCompleted = this.activeModLoadTask.IsLoaded;
			this.continuationAction = null;
		}

		internal void NotifyModReady()
		{
			this.IsCompleted = true;

			this.continuationAction?.Invoke();
		}

		public Guid GetResult() => this.activeModLoadTask.modUuid;

		public bool IsCompleted { get; internal set; }

		public void OnCompleted(Action continuation)
		{
			this.continuationAction = continuation;

			if (this.IsCompleted)
				this.continuationAction?.Invoke();
		}
	}

	internal interface IAssetPointerLoadTask
	{
		IAssetReference assetReference { get; set; }

		void NotifyAssetReady();
	}

	internal class AssetPointerLoadTask<T> : IAssetPointerLoadTask
	{
		internal bool IsLoaded = false;

		public IAssetReference assetReference { get; set; }

		private List<AssetPointerLoadTaskAwaiter<T>> awaiters = new();

		internal AssetPointerLoadTask(IAssetReference assetReference)
		{
			this.assetReference = assetReference;

			ModManager.Instance.LoadAssetFromMod(this);
		}

		public void NotifyAssetReady()
		{
			IsLoaded = true;

			foreach (AssetPointerLoadTaskAwaiter<T> awaiter in awaiters)
			{
				awaiter.NotifyAssetReady();
			}
		}

		public AssetPointerLoadTaskAwaiter<T> GetAwaiter()
		{
			AssetPointerLoadTaskAwaiter<T> newAwaiter = new AssetPointerLoadTaskAwaiter<T>(this);

			awaiters.Add(newAwaiter);

			return newAwaiter;
		}
	}

	internal class AssetPointerLoadTaskAwaiter<T> : INotifyCompletion
	{
		AssetPointerLoadTask<T> activeAssetPointerLoadTask;

		Action continuationAction;

		public AssetPointerLoadTaskAwaiter(AssetPointerLoadTask<T> activeAssetPointerLoadTask)
		{
			this.activeAssetPointerLoadTask = activeAssetPointerLoadTask;

			this.IsCompleted = this.activeAssetPointerLoadTask.IsLoaded;
			this.continuationAction = null;
		}

		internal void NotifyAssetReady()
		{
			this.IsCompleted = true;

			this.continuationAction?.Invoke();
		}

		public AssetReference<T> GetResult() => (AssetReference<T>)this.activeAssetPointerLoadTask.assetReference;

		public bool IsCompleted { get; internal set; }

		public void OnCompleted(Action continuation)
		{
			this.continuationAction = continuation;

			if (this.IsCompleted)
				this.continuationAction?.Invoke();
		}
	}

	public class RepositoryManagerLoadingAwaiter : INotifyCompletion
	{
		Action continuationAction;

		public RepositoryManagerLoadingAwaiter()
		{
			this.IsCompleted = RepositoryManager.Instance.DoneLoadingRepositories;
			this.continuationAction = null;
		}

		internal void NotifyLoadingDone()
		{
			this.IsCompleted = true;

			this.continuationAction?.Invoke();
		}

		public bool GetResult() => RepositoryManager.Instance.DoneLoadingRepositories;

		public bool IsCompleted { get; internal set; }

		public void OnCompleted(Action continuation)
		{
			this.continuationAction = continuation;

			if (this.IsCompleted)
				this.continuationAction?.Invoke();
		}
	}
}
