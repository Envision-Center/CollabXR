using System;
using System.Collections.Generic;
using CollabXR.ModPackager;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects
{
	public class MainLibraryRef : SingletonBehavior<MainLibraryRef>
	{
		[SerializeField]
		private BuiltinObjectLibrary baseLibrary;
		public ObjectDictionary library;
		public UnityEvent onNewCategoryCreation,
			onNewDataLoad;
		public GameObject placeholderPrefab;
		public Texture2D defaultThumbnail;

		protected override void Awake()
		{
			base.Awake();
			ClearData();
		}

		public void AddData(string categoryName, CollabObjectData data)
		{
			ObjectCategory targetCategory = library.categories.Find(category => category.name == categoryName);
			if (targetCategory == null)
			{
				targetCategory = new ObjectCategory();
				targetCategory.name = categoryName;
				targetCategory.objectData = new List<CollabObjectData>();
				int index = library.categories.FindLastIndex(e =>
				{
					bool isAfter = String.Compare(categoryName, e.name) > 0;
					return isAfter;
				});
				library.categories.Insert(index + 1, targetCategory);
				onNewCategoryCreation.Invoke();
			}
			targetCategory.objectData.Add(data);
			data.category = targetCategory.name;
			onNewDataLoad.Invoke();
			Debug.Log($"[Object Library] added {categoryName}/{data.assetName}");
		}

		public void AddData(Guid modGUID, Guid assetGUID, ModPrefab modPrefab, ModMetadata modMetadata, bool bundleExists)
		{
			CollabObjectData newObjData = ScriptableObject.CreateInstance<CollabObjectData>();
			newObjData.Initialize(modGUID, assetGUID, modPrefab, modMetadata, bundleExists);
			AddData(newObjData.category, newObjData);
		}

		public CollabObjectData FindData(string categoryName, string objectName)
		{
			ObjectCategory category = library.categories.Find(x => x.name.Equals(categoryName));
			if (category == null)
				return null;
			CollabObjectData match = category.objectData.Find(x => x.assetName == objectName);
			return match;
		}

		public void ClearData()
		{
			library = new ObjectDictionary();
			foreach (ObjectCategory category in baseLibrary.dictionary.categories)
			{
				ObjectCategory newCategory = new ObjectCategory();
				newCategory.name = category.name;
				newCategory.objectData = new List<CollabObjectData>(category.objectData);
				library.categories.Add(newCategory);
			}
		}
	}
}
