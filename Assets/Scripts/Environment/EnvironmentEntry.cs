using System;
using UnityEngine;
using System.Collections.Generic;
using CollabXR.ModPackager;

namespace CollabXR.Environment
{
    public class EnvironmentEntry
    {
        /// Primary Menu display items
        /// -------------------------
        public string formattedSceneName;
        public Texture2D thumbnail;
        public List<string> teleports;

        /// The scene itself
        /// Must be a full path, either built-in or sourced from an asset bundle
        /// -------------------
        public string scenePath; // scene path relative to bundle
        public string bundlePath; // bundle path relative to StreamingAssets

        /// Other Mod/Asset Information
        /// -------------------------------
        public Guid modGUID;
        public Guid assetGUID;
        public string category;
        public string attribution;
        public bool availableOnThisPlatform;
        public List<string> creatorNames;

        public void Initialize(
            Guid modGUID,
            Guid assetGUID,
            ModScene modScene,
            ModMetadata modMetadata,
            bool available)
        {
            this.modGUID = modGUID;
            this.assetGUID = assetGUID;
            this.category = modScene.Category;
            this.attribution = modScene.Attribution;
            this.teleports = modScene.teleports;
            this.formattedSceneName = modScene.FormattedName;
            this.thumbnail = modScene.Thumbnail;
            this.availableOnThisPlatform = available;
            this.creatorNames = modMetadata.Creators;
        }
    }
}
