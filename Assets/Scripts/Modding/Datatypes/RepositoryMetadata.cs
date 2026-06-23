using System;
using Newtonsoft.Json;

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

		public Guid[] Mods;
	}
}
