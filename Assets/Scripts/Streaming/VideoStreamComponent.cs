using System.Runtime.CompilerServices;
using Unity.WebRTC;
using UnityEngine;

namespace CollabXR.Streaming
{
	public class VideoStreamComponent : MonoBehaviour
	{
		private RTCPeerConnection remoteConnection;
		private RTCDataChannel receiveChannel;

		private MediaStream receiveStream;

		public Material shaderMaterial;
		public MeshRenderer meshToAssignMaterial;
		public string streamURL = "stun:stun.l.google.com:19302"; // Google Stun server

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			var config = new RTCConfiguration { iceServers = new RTCIceServer[] { new RTCIceServer { urls = new string[] { streamURL } } } };

			remoteConnection = new RTCPeerConnection(ref config);
			remoteConnection.OnDataChannel = ReceiveChannelCallback;
			//localConnection.CreateDataChannel("sendChannel");

			// TODO: this probably has to be shoved into a singleton, unfortunately
			// in case we support multiple video streams
			StartCoroutine(WebRTC.Update());

			shaderMaterial = Instantiate(shaderMaterial); // Make a copy so we don't modify original
			meshToAssignMaterial.sharedMaterials[0] = shaderMaterial;

			// https://docs.unity3d.com/Packages/com.unity.webrtc@3.0/manual/videostreaming.html#receiving-video
			receiveStream = new MediaStream();
			receiveStream.OnAddTrack = e =>
			{
				if (e.Track is VideoStreamTrack videoTrack)
				{
					// TODO: generalize this to make texture available on like a socket or something idk
					shaderMaterial.SetTexture("_BaseMap", videoTrack.Texture);
				}
				else if (e.Track is AudioStreamTrack audioTrack)
				{
					audioTrack.Source.Play();
				}
			};

			remoteConnection.OnTrack = (RTCTrackEvent e) =>
			{
				if (e.Track.Kind == TrackKind.Video)
				{
					// Add track to MediaStream for receiver.
					// This process triggers `OnAddTrack` event of `MediaStream`.
					receiveStream.AddTrack(e.Track);
				}
			};
		}

		private void OnDestroy()
		{
			receiveChannel.Close();
			remoteConnection.Close();
		}

		// Update is called once per frame
		void Update() { }

		private void ReceiveChannelCallback(RTCDataChannel channel)
		{
			receiveChannel = channel;
			receiveChannel.OnMessage = HandleReceiveMessage;
		}

		private void HandleReceiveMessage(byte[] bytes)
		{
			var message = System.Text.Encoding.UTF8.GetString(bytes);
			Debug.Log(message);
		}
	}
}
