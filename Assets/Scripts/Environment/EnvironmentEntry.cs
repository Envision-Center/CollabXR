using System;
using UnityEngine;
using System.Collections.Generic;
using CollabXR.ModPackager;
using CollabXR.ModPackager.EnvironmentExtras;

namespace CollabXR.Environments
{
    public class EnvironmentEntry
    {
        public const string BUILTIN_CATEGORY = "Built-in Environments";
        
        /// Primary Menu display items
        /// -------------------------
        public string formattedSceneName;
        public Texture2D thumbnail;
        public List<string> teleports;
        public List<Sprite> teleportThumbnails;

        /// The scene itself
        /// Must be a full path, either built-in or sourced from an asset bundle
        /// -------------------
        public string scenePath;
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
            this.teleports = modScene.teleports ?? new List<string>();
            this.teleportThumbnails = new List<Sprite>();
            this.formattedSceneName = modScene.FormattedName;
            this.thumbnail = modScene.Thumbnail;
            this.creatorNames = modMetadata.Creators;
            if (modMetadata.AssetMap != null && modMetadata.AssetMap.TryGetValue(assetGUID, out string path))
            {
                this.scenePath = path;
            }
            this.availableOnThisPlatform = available && !string.IsNullOrEmpty(this.scenePath);
        }

        public void InitializeFromLocalData(EnvironmentData envData)
        {
            this.modGUID = Guid.Empty;
            this.assetGUID = Guid.Empty;
            this.category = BUILTIN_CATEGORY;
            this.attribution = null;
            this.formattedSceneName = envData.sceneName;
            this.thumbnail = envData.teleportInfo != null && envData.teleportInfo.Length > 0 && envData.teleportInfo[0].thumbnail != null
                ? envData.teleportInfo[0].thumbnail.texture : null;
            this.availableOnThisPlatform = true; // built-in environments are always available
            this.creatorNames = new List<string> { "Envision Center" };
            this.scenePath = envData.scenePath;

            List<string> teleportNames = new List<string>();
            this.teleportThumbnails = new List<Sprite>();
            foreach (var teleport in envData.teleportInfo ?? Array.Empty<EnvironmentTeleportInfo>())
            {
                teleportNames.Add(teleport.name);
                this.teleportThumbnails.Add(teleport.thumbnail);
            }
            this.teleports = teleportNames;
        }
    }
}
