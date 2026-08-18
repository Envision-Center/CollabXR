using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CollabXR.ModLoader
{
	public class RepositoryMetadata
	{
		[UnityEngine.Scripting.Preserve]
		public RepositoryMetadata() { }

		public int StructVersion;

		public string BaseURL;

		public string S3BucketName;

		public string CognitoURL;
		public string CognitoUserPool;
		public string CognitoIdentityPool;
		public string CognitoClientID;

		public string RepoName;
		public string RepoOwner;

		public string accessKey;
		public string secretKey;

		public string[] Mods;

		/// <summary>
		/// Stores the folder path in which the mod is stored, look up by mod guid
		/// To get full path to mod, use folder path + mod guid
		/// </summary>
		[NonSerialized]
		public Dictionary<Guid, string> rootFolderLookUp;

		[OnDeserialized]
		private void ConstructLookUpTable(StreamingContext context)
		{
			rootFolderLookUp = new();
			foreach (var url in Mods)
			{ 
				int delim = url.LastIndexOf('/');
				rootFolderLookUp.Add(new(url[(delim + 1)..]), url[..(delim + 1)]);
			}
		}
	}
}
