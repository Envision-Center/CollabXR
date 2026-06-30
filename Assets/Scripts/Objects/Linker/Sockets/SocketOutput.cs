using CollabXR.ModExtras.Annotation;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Linker.Sockets
{
	[CreateAssetMenu(fileName = "SocketOutput", menuName = "CollabXR/Sockets/Socket Output")]
	public class SocketOutput : SocketBase
	{
		private SocketAnnotation annotation;

		public SocketBehavior behavior
		{
			get { return annotation.behavior; }
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
				case SocketBehavior.StaticImage:
					annotation.c_imageTexture.AddListener(
						(Texture2D texture) =>
						{
							pushTexture.Invoke(texture);
						}
					);
					break;
				case SocketBehavior.Volumetric:
					annotation.c_volumeTexture.AddListener(
						(Texture3D texture) =>
						{
							pushVolumetric.Invoke(texture, annotation.pointOfReference);
						}
					);
					break;
			}
		}

		/// <summary>
		/// Emitted when connected to another socket.
		/// </summary>
		public override void OnConnect(SocketBase otherSocket)
		{
			base.OnConnect(otherSocket);
			switch (annotation.behavior)
			{
				case SocketBehavior.ScriptableObject:
					pushScriptableObject.Invoke(annotation.scriptableObject, annotation.pointOfReference);
					break;
				case SocketBehavior.StaticImage:
					pushTexture.Invoke(annotation.imageTexture);
					break;
				case SocketBehavior.Volumetric:
					pushVolumetric.Invoke(annotation.volumeTexture, annotation.pointOfReference);
					break;
			}
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
		public UnityEvent<Texture2D> pushTexture = new UnityEvent<Texture2D>();
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
