using CollabXR.Networking;
using CollabXR.Objects.Linker.Sockets;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Linker.Sockets
{
	/// <summary>
	/// Socket that forwards plain UnityEvents from its original input.
	/// </summary>
	public class SocketEvent : SocketBase
	{
		public UnityEvent socketEvent;

		protected override void Awake()
		{
			base.Awake();
			flow = SocketFlowDirection.Consumer; // Ensure that this is a data consumer

			if (socketEvent == null)
			{
				socketEvent = new UnityEvent();
			}
		}

		protected override void OnConnect(SocketBase otherSocket)
		{
			base.OnConnect(otherSocket);

			// We should only be allowed to get to this step if
			// the other socket is already a SocketOutput
			SocketOutput output = (SocketOutput)otherSocket;
			output.pushEvent.AddListener(PushEvent);
		}

		protected override void OnDisconnect(SocketBase otherSocket)
		{
			base.OnDisconnect(otherSocket);

			SocketOutput output = (SocketOutput)otherSocket;
			output.pushEvent.RemoveListener(PushEvent);
		}

		private void PushEvent()
		{
			socketEvent.Invoke();
		}
	}
}
