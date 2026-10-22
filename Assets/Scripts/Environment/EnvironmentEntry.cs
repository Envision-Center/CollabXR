using System;
using UnityEngine;
using System.Collections.Generic;
using CollabXR.ModPackager;
using CollabXR.ModPackager.EnvironmentExtras;

namespace CollabXR.Environments
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

        public void InitializeFromLocalData(EnvironmentData envData)
        {
            this.modGUID = Guid.Empty;
            this.assetGUID = Guid.Empty;
            this.category = "Built-in Environments"; // placeholder
            this.attribution = null;
            this.formattedSceneName = envData.sceneName;
            this.thumbnail = envData.teleportInfo[0].thumbnail.texture;
            this.availableOnThisPlatform = true; // built-in environments are always available
            this.creatorNames = new List<string> { "Envision Center" };

            List<string> teleportNames = new List<string>();
            foreach (var teleport in envData.teleportInfo)
            {
                teleportNames.Add(teleport.name);
            }
            this.teleports = teleportNames;
        }
    }
}
