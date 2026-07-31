using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

namespace CollabXR.Scriptables
{
	// [CreateAssetMenu(fileName = "Boolean", menuName = "CollabXR/Scriptable Variables/Boolean")]
	public abstract class GenericScriptableVariable<T> : ScriptableObject
	{
		// default starting value to read from at runtime
		[SerializeField]
		private T serializedValue;

		// value that affects behavior
		private T variableValue;

		[SerializeField]
		protected string playerPrefName;

		public event Action<T> onChange = delegate { };
		public event Action<T> beforeChange = delegate { };

		[NonSerialized]
		private bool initialized = false; // flag for whether or not serialized value has been read

		public void Initialize()
		{
			if (!playerPrefName.IsNullOrEmpty())
			{
				variableValue = GetPlayerPref();
				Debug.Log($"Deserialized {playerPrefName} as {variableValue}");
			}
			else
			{
				variableValue = serializedValue;
			}
			initialized = true;
		}

		public void Set(T t)
		{
			Value = t;
			SetPlayerPref(t);
		}

		public void AddChangeListenerAndCheck(Action<T> f)
		{
			f.Invoke(Value);
			onChange += f;
		}

		public T Value
		{
			get
			{
				if (!initialized)
				{
					Initialize();
				}
				return variableValue;
			}
			set
			{
				initialized = true;
				if (EqualityComparer<T>.Default.Equals(variableValue, value))
					return;

				beforeChange.Invoke(variableValue);
				variableValue = value;
				onChange.Invoke(variableValue);
			}
		}

		public abstract T GetPlayerPref();
		public abstract void SetPlayerPref(T t);
	}

	public abstract class GenericScriptableVariableEvents<T> : MonoBehaviour
	{
		[SerializeField]
		private GenericScriptableVariable<T> scriptableVariable;

		public UnityEvent<T> onChange;
		public UnityEvent<T> beforeChange;

		public bool waitForNetworkObject;

		private NetworkObject netObj;
		private bool initialized = false;

		protected virtual void Awake()
		{
			scriptableVariable.onChange += onChange.Invoke;
			scriptableVariable.beforeChange += beforeChange.Invoke;
			if (waitForNetworkObject)
			{
				netObj = GetComponent<NetworkObject>();
			}
		}

		private void Update()
		{
			if (!initialized && waitForNetworkObject && netObj.IsValid && isActiveAndEnabled) // this component is enabled and the network object only just spawned
			{
				initialized = true;
				onChange.Invoke(scriptableVariable.Value);
			}
		}

		private void OnEnable()
		{
			if (!waitForNetworkObject || netObj.IsValid) // this component is enabled and the network object is irrelevant or already active
			{
				initialized = true;
				onChange.Invoke(scriptableVariable.Value);
			}
		}

		public void AddChangeListenerAndCheck(UnityAction<T> f)
		{
			f.Invoke(scriptableVariable.Value);
			onChange.AddListener(f);
		}

		private void OnDestroy()
		{
			scriptableVariable.onChange -= onChange.Invoke;
			scriptableVariable.beforeChange -= beforeChange.Invoke;
		}
	}
}
