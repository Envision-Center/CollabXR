using System.Collections.Generic;
using CollabXR.ModExtras.Annotation;
using CollabXR.ModExtras.Measurement;
using NUnit.Framework;
using UnityEngine;

namespace CollabXR.Objects.Linker.Sockets
{
	[CreateAssetMenu(fileName = "SocketTexture", menuName = "CollabXR/Sockets/Texture")]
	public class SocketTexture : SocketBase
	{
		[Header("Material")]
		[SerializeField, Tooltip("Base material to use for the actual screen portion of the object.")]
		private Material screenMaterial;

		[SerializeField, Tooltip("Shader parameter ID to apply textures to.")]
		private string parameterName = "_Texture";

		[SerializeField, Tooltip("Mesh Renderer to apply the material to.")]
		private MeshRenderer meshTarget;

		[SerializeField, Tooltip("Material slot on the mesh to utilize.")]
		private ushort materialSlot = 0;

		[SerializeField, Tooltip("Optional, default texture to apply to the screen, if any.")]
		private Texture2D defaultTexture;

		private Material instancedMaterial;

		protected override void Awake()
		{
			base.Awake();
			flow = SocketFlowDirection.Consumer; // Ensure that this is a data consumer

			// Preload the material
			instancedMaterial = Instantiate(screenMaterial);

			List<Material> sharedMaterials = new List<Material>();
			meshTarget.GetSharedMaterials(sharedMaterials);
			sharedMaterials[materialSlot] = instancedMaterial;
			meshTarget.SetSharedMaterials(sharedMaterials);

			// Apply a default texture if defined
			if (defaultTexture != null)
			{
				ApplyTexture(defaultTexture);
			}
		}

		public override bool CanConnect(SocketBase otherSocket)
		{
			if (base.CanConnect(otherSocket) && otherSocket is SocketOutput)
			{
				SocketOutput output = (SocketOutput)otherSocket;
				if (output == null)
				{
					Debug.Log("Socket Legend: other socket was not an output");
					return false;
				}
				return output.behavior == SocketBehavior.Texture;
			}

			return false;
		}

		protected override void OnConnect(SocketBase otherSocket)
		{
			base.OnConnect(otherSocket);

			// We should only be allowed to get to this step if
			// the other socket is already a SocketOutput
			SocketOutput output = (SocketOutput)otherSocket;
			output.pushTexture.AddListener(ApplyTexture);
		}

		protected override void OnDisconnect(SocketBase otherSocket)
		{
			base.OnDisconnect(otherSocket);

			SocketOutput output = (SocketOutput)otherSocket;
			output.pushTexture.RemoveListener(ApplyTexture);
		}

		private void ApplyTexture(Texture newTexture)
		{
			instancedMaterial.SetTexture(parameterName, newTexture);
		}
	}
}
