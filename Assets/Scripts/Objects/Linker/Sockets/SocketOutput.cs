using CollabXR.ModExtras.Annotation;
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

		public SocketBehavior behavior
		{
			get { return annotation.behavior; }
		}

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
							pushTexture.Invoke(texture);
						}
					);
					break;
				case SocketBehavior.Volumetric:
					annotation.c_texture.AddListener(
						(Texture texture) =>
						{
							pushVolumetric.Invoke(texture as Texture3D, annotation.pointOfReference);
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
					pushScriptableObject.Invoke(annotation.scriptableObject, annotation.pointOfReference);
					break;
				case SocketBehavior.Texture:
					pushTexture.Invoke(annotation.texture);
					break;
				case SocketBehavior.Volumetric:
					pushVolumetric.Invoke(annotation.texture as Texture3D, annotation.pointOfReference);
					break;
			}
			eventConnected.Invoke();
		}

		protected override void OnDisconnect(SocketBase otherSocket)
		{
			base.OnDisconnect(otherSocket);
			eventDisconnected.Invoke();
		}

		// TODO: Polling is bad
		//public void Update()
		//{
		//	if (annotation.behavior == SocketBehavior.FloatStream)
		//	{
		//		pushFloat.Invoke(annotation.floatStreamValue);
		//	}
		//}

		public UnityEvent<ScriptableObject, Transform> pushScriptableObject = new UnityEvent<ScriptableObject, Transform>();
		public UnityEvent<float> pushFloat = new UnityEvent<float>();
		public UnityEvent<Texture> pushTexture = new UnityEvent<Texture>();
		public UnityEvent<Texture3D, Transform> pushVolumetric = new UnityEvent<Texture3D, Transform>();

		private void OnDestroy()
		{
			pushScriptableObject.RemoveAllListeners();
			pushFloat.RemoveAllListeners();
			pushTexture.RemoveAllListeners();
			pushVolumetric.RemoveAllListeners();

			Debug.Log("SOCKET OUTPUT: OnDestroy finished");
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
	}
}
