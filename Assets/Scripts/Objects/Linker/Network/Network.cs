using System;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects.Linker.Network
{
	// https://doc.photonengine.com/fusion/v2/manual/fusion-types/network-collections
	[Serializable]
	public struct NetworkLinkerSocketConnection : INetworkStruct
	{
		public ushort fromSocketIndex; // TODO: could be compressed as a byte?
		public NetworkId toObject;
		public ushort toSocketIndex;
	}
}
