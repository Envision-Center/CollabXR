using System;
using System.Collections;
using System.Collections.Generic;
using CollabXR.ModExtras.Annotation;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Linker.Sockets
{
	[CreateAssetMenu(fileName = "SocketOutput", menuName = "CollabXR/Sockets/Socket Output")]
	//[RequireComponent(typeof(SocketAnnotation))] // Throws errors from object previews
	public class SocketOutput : SocketBase
	{
		private SocketAnnotation annotation;

		[SerializeField, Tooltip("Whether to automatically initialize. Only use for built-in prefabs.")]
		private bool autoInitialize;

		[SerializeField, Tooltip("Whether to request authority before passing event data to connections. ONLY USE THIS WHEN INPUT IS CLIENT-SIDE ONLY.")]
		private bool requestAuthorityOnEvent = false;

		/// <summary>
		/// Maximum amount of time to request state authority before performing the requested action regardless.
		/// </summary>
		private const float MAX_ATTEMPT_DURATION = 2.0f;

		public SocketBehavior behavior => annotation.behavior;

		protected override void Awake()
		{
			base.Awake();
			if (autoInitialize)
			{
				Initialize(GetComponent<SocketAnnotation>());
			}
		}

		/// <summary>
		/// Initializes the Socket Output with the given annotation.
		/// </summary>
		/// <param name="annotation"></param>
		public void Initialize(SocketAnnotation annotation)
		{
			this.annotation = annotation;
			// Bind to on-change events
			switch (annotation.behavior)
			{
				case SocketBehavior.Texture:
					annotation.c_texture.AddListener(
						(Texture texture) =>
						{
							FireWithAuthority(() => pushTexture.Invoke(texture));
						}
					);
					break;
				case SocketBehavior.Volumetric:
					annotation.c_texture.AddListener(
						(Texture texture) =>
						{
							FireWithAuthority(() => pushVolumetric.Invoke(texture as Texture3D, annotation.pointOfReference));
						}
					);
					break;
				case SocketBehavior.PlainEvent:
					annotation.c_plainEvent.AddListener(() =>
					{
						FireWithAuthority(pushEvent.Invoke);
					});
					break;
				case SocketBehavior.Float:
					annotation.c_float.AddListener(
						(float value) =>
						{
							FireWithAuthority(() => pushFloat.Invoke(value));
						}
					);
					break;
				case SocketBehavior.String:
					annotation.c_string.AddListener(
						(string value) =>
						{
							FireWithAuthority(() => pushString.Invoke(value));
						}
					);
					break;
			}
			UpdateSocketColor(); // Now that we know the output data type, apply it
		}

		protected override Color GetSocketColor()
		{
			if (annotation != null)
			{
				return LinkerConfig.Instance.colorBehavior[(int)annotation.behavior];
			}
			return LinkerConfig.Instance.colorProvider;
		}

		/// <summary>
		/// Emitted when connected to another socket.
		/// </summary>
		protected override void OnConnect(SocketBase otherSocket)
		{
			base.OnConnect(otherSocket);
			switch (annotation.behavior)
			{
				case SocketBehavior.ScriptableObject:
					FireWithAuthority(() => pushScriptableObject.Invoke(annotation.scriptableObject, annotation.pointOfReference));
					break;
				case SocketBehavior.Texture:
					FireWithAuthority(() => pushTexture.Invoke(annotation.textureValue));
					break;
				case SocketBehavior.Volumetric:
					FireWithAuthority(() => pushVolumetric.Invoke(annotation.textureValue as Texture3D, annotation.pointOfReference));
					break;
				case SocketBehavior.PlainEvent:
					// Don't send any event on connect
					break;
				case SocketBehavior.Float:
					FireWithAuthority(() => pushFloat.Invoke(annotation.floatValue));
					break;
				case SocketBehavior.String:
					FireWithAuthority(() => pushString.Invoke(annotation.stringValue));
					break;
			}
			eventConnected.Invoke();
		}

		protected override void OnDisconnect(SocketBase otherSocket)
		{
			base.OnDisconnect(otherSocket);
			eventDisconnected.Invoke();
		}

		public UnityEvent<ScriptableObject, Transform> pushScriptableObject = new UnityEvent<ScriptableObject, Transform>();
		public UnityEvent<Texture> pushTexture = new UnityEvent<Texture>();
		public UnityEvent<Texture3D, Transform> pushVolumetric = new UnityEvent<Texture3D, Transform>();
		public UnityEvent pushEvent = new UnityEvent();
		public UnityEvent<float> pushFloat = new UnityEvent<float>();
		public UnityEvent<string> pushString = new UnityEvent<string>();

		protected override void OnDestroy()
		{
			base.OnDestroy();

			// Unbind socket annotation listeners
			pushScriptableObject.RemoveAllListeners();
			pushFloat.RemoveAllListeners();
			pushTexture.RemoveAllListeners();
			pushVolumetric.RemoveAllListeners();
			pushEvent.RemoveAllListeners();
		}

		/// <typeparam name="T"></typeparam>
		/// <returns>
		/// Whether there is a ScriptableObject and if it is of the given type.
		/// </returns>
		public bool UsesScriptableObjectType<T>()
			where T : ScriptableObject
		{
			//Debug.Log(string.Format("UsesScriptableObjectType: {0}, {1}", annotation.scriptableObject != null, annotation.scriptableObject is T));
			return annotation.scriptableObject != null && annotation.scriptableObject is T;
		}

		/// <summary>
		/// If no authority is required, immediately calls the callback.
		/// If authority over connections is required
		/// (i.e. this socket is only fired on our client),
		/// requests authority and waits before pushing the event.
		/// </summary>
		/// <param name="callback">Callback to perform.</param>
		private void FireWithAuthority(Action callback)
		{
			if (!requestAuthorityOnEvent)
			{
				callback(); // No authority needed
				return;
			}

			// See if we need authority from any objects before firing our event
			List<NetworkObject> awaitingAuthority = new List<NetworkObject>();
			foreach (SocketBase connection in connections)
			{
				NetworkObject netObj = connection.GetNetworkObject();
				// If we do not already have state authority, request it
				// Because we can be connected to multiple sockets on the same object, avoid duplicates
				if (!netObj.HasStateAuthority && !awaitingAuthority.Contains(netObj))
				{
					netObj.RequestStateAuthority();
					awaitingAuthority.Add(netObj);
				}
			}

			if (awaitingAuthority.Count > 0)
			{
				// We need to wait for authority before firing callback
				FireWithAuthorityDeferred(awaitingAuthority, callback);
				return;
			}

			callback(); // We already own all the connections
		}

		/// <summary>
		/// Waits to obtain authority over the given list of NetworkObjects,
		/// and then fires the callback.
		///
		/// If authority was never gained or the request times out, fires callback anyway.
		/// Sockets are not gauranteed to fire, but we want our best chances.
		/// </summary>
		/// <param name="awaitingAuthority">NetworkObjects we are waiting to get authority over.</param>
		/// <param name="callback">Action to call at the end.</param>
		/// <returns></returns>
		private IEnumerator FireWithAuthorityDeferred(List<NetworkObject> awaitingAuthority, Action callback)
		{
			float startTime = Time.time; // When the request started

			// Wait until we either have state authority, or our request times out
			while ((awaitingAuthority.Count > 0) && (Time.time < startTime + MAX_ATTEMPT_DURATION))
			{
				// Remove any objects from queue that we've gained authority over
				// Index loop because foreach hates array modification
				for (int i = 0; i < awaitingAuthority.Count; i++)
				{
					if (awaitingAuthority[i].HasStateAuthority)
					{
						awaitingAuthority.Remove(awaitingAuthority[i]);
						i--;
					}
				}
				yield return null;
			}

			// Perform action regardless of authority at this point (we tried)
			callback();
		}
	}
}
