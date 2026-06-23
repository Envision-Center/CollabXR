using Fusion;
using UnityEngine;

namespace CollabXR
{
	public abstract class SingletonBehavior<T> : MonoBehaviour
		where T : SingletonBehavior<T>
	{
		public static T Instance { get; private set; }

		protected virtual void Awake()
		{
			if (Instance == null)
			{
				Instance = (T)this;
			}
			else
			{
				Debug.LogWarning("More than one instance of " + GetType() + " created!");
				Destroy(this);
			}
		}

		protected virtual void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}
	}

	public abstract class SingletonNetworkBehavior<T> : NetworkBehaviour
		where T : SingletonNetworkBehavior<T>
	{
		public static T Instance { get; private set; }

		protected virtual void Awake()
		{
			if (ReferenceEquals(Instance, null))
			{
				Instance = (T)this;
			}
			else
			{
				Debug.LogWarning("More than one instance of " + GetType() + " created!");
				Destroy(this);
			}
		}

		protected virtual void OnDestroy()
		{
			if (Instance == this)
				Instance = null;
		}
	}
}
