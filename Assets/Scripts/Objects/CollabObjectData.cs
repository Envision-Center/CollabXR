using System;
using System.Collections.Generic;
using CollabXR.ModPackager;
using SaintsField;
using UnityEngine;

namespace CollabXR.Objects
{
	[CreateAssetMenu(menuName = "CollabXR/Collab Object Data")]
	public class CollabObjectData : ScriptableObject
	{
		public Vector3 startingScale = Vector3.one,
			minScale = Vector3.one / 2,
			maxScale = Vector3.one * 2;
		public Vector3 startingOffset = Vector3.zero;
		public GameObject prefab;

		[TextArea]
		public string attribution;

		public GameObject contextPrefab;
		public bool isSimpleModel = true;
		public string formattedName;

		[ReadOnly]
		public string category;

		[ReadOnly]
		public string assetName;
		public Sprite thumbnail;

		[ReadOnly]
		public Guid modGUID;

		[ReadOnly]
		public Guid assetGUID;
		public bool availableOnThisPlatform = true;

		[ReadOnly]
		public List<string> creators;

		public void Initialize(Guid modGUID, Guid assetGUID, ModPrefab modPrefab, ModMetadata modMetadata, bool available)
		{
			this.modGUID = modGUID;
			this.assetGUID = assetGUID;
			this.category = modPrefab.Category;
			this.assetName = modPrefab.FormattedName;
			this.attribution = modPrefab.Attribution;
			this.creators = modMetadata.Creators;
			this.formattedName = modPrefab.FormattedName;
			this.startingScale = Vector3.one;
			this.minScale = modPrefab.MinScale;
			this.maxScale = modPrefab.MaxScale;
			this.startingOffset = modPrefab.StartingOffset;
			this.availableOnThisPlatform = available;
			Texture2D thumbTex = modPrefab.Thumbnail == null ? MainLibraryRef.Instance.defaultThumbnail : modPrefab.Thumbnail;
			this.thumbnail = Sprite.Create(thumbTex, new Rect(0, 0, thumbTex.width, thumbTex.height), new Vector2(0.5f, 0.5f));
			if (modPrefab.Thumbnail == null)
			{
				Debug.LogWarning($"Found Collab Object {this.category}/{this.assetName} with a null thumbnail!");
			}
		}
	}
}
