using System;

namespace CollabXR.ModLoader
{
	/// <summary>
	/// Represents a reference to a scene asset within a mod bundle.
	/// Minimal environment data structute for ModManager and EnvironmentManager to pass around. 
	/// </summary>
	public class SceneReference
	{
		/// <summary>
		/// Unique identifier for an instance of a scene.
		/// Not super useful since I can't think of why you'd need 2+ of the same scene active at a time.
		/// This is mainly to 
		/// 
		/// </summary>
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
