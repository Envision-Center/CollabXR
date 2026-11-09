using System;

namespace CollabXR.ModLoader
{
	/// <summary>
	/// Represents a reference to a scene asset within a mod bundle.
	/// </summary>
	public sealed class SceneReference
	{
		public Guid ReferenceGuid { get; }
		public Guid ModGuid { get; }
		public Guid AssetGuid { get; }
		public string Path { get; }

		internal SceneReference(Guid modGuid, Guid assetGuid, string path)
		{
			ReferenceGuid = Guid.NewGuid();
			ModGuid = modGuid;
			AssetGuid = assetGuid;
			Path = path;
		}
	}
}
