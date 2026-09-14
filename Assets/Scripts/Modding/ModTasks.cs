using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CollabXR.ModLoader
{
	internal enum TaskLoadStatus
	{
		Pending,
		Failed,
		Completed
	}

	internal class ModLoadTask
	{
		internal TaskLoadStatus status;

		internal Guid modUuid { get; set; }

		private List<ModLoadTaskAwaiter> awaiters = new();

		internal ModLoadTask(Guid modUuid)
		{
			this.modUuid = modUuid;

			status = TaskLoadStatus.Pending;

			ModManager.Instance.LoadMod(this);
		}

		internal void NotifyModReady()
		{
			status = TaskLoadStatus.Completed;

			foreach (ModLoadTaskAwaiter awaiter in awaiters)
			{
				awaiter.NotifyModReady();
			}
		}

		internal void NotifyModFailedToLoad(Exception ex)
		{
			status = TaskLoadStatus.Failed;

			foreach (ModLoadTaskAwaiter awaiter in awaiters)
			{
				awaiter.NotifyModFailed(ex);
			}
		}

		public ModLoadTaskAwaiter GetAwaiter()
		{
			ModLoadTaskAwaiter newAwaiter = new(this);

			awaiters.Add(newAwaiter);

			return newAwaiter;
		}
	}

	internal class ModLoadTaskAwaiter : INotifyCompletion
	{
		ModLoadTask activeModLoadTask;
		List<Exception> exceptions;
		Action continuationAction;

		public ModLoadTaskAwaiter(ModLoadTask activeModLoadTask)
		{
			this.activeModLoadTask = activeModLoadTask;
			exceptions = new();
			this.IsCompleted = this.activeModLoadTask.status == TaskLoadStatus.Completed;
			this.continuationAction = null;
		}

		internal void NotifyModReady()
		{
			this.IsCompleted = true;

			this.continuationAction?.Invoke();
		}

		internal void NotifyModFailed(Exception ex)
		{
			this.IsCompleted = false;

			exceptions.Add(ex);
		}

		public Guid GetResult()
		{
			if (exceptions.Count > 0)
			{
				throw new AggregateException($"Errors occured while loading mod {activeModLoadTask}.", exceptions);
			}
			return this.activeModLoadTask.modUuid;
		}

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

		void NotifyAssetFailedToLoad();
	}

	internal class AssetPointerLoadTask<T> : IAssetPointerLoadTask
	{
		internal TaskLoadStatus status;

		public IAssetReference assetReference { get; set; }

		private List<AssetPointerLoadTaskAwaiter<T>> awaiters = new();

		internal AssetPointerLoadTask(IAssetReference assetReference)
		{
			this.assetReference = assetReference;

			ModManager.Instance.LoadAssetFromMod(this);
		}

		public void NotifyAssetReady()
		{
			status = TaskLoadStatus.Completed;

			foreach (AssetPointerLoadTaskAwaiter<T> awaiter in awaiters)
			{
				awaiter.NotifyAssetReady();
			}
		}

		public void NotifyAssetFailedToLoad()
		{
			status = TaskLoadStatus.Failed;

			awaiters.Clear();
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

			this.IsCompleted = this.activeAssetPointerLoadTask.status == TaskLoadStatus.Completed;
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
