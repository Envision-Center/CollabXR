using System;
using UnityEngine;
using System.Collections.Generic;
using CollabXR.ModPackager;

[CreateAssetMenu(fileName = "NewEnvEntry", menuName = "CollabXR/Env/Library Entry")]
public class EnvEntry : ScriptableObject
{
    /// Primary Menu display items
    /// -------------------------
    public string formattedSceneName;
    public Texture2D thumbnail;

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
        // here's where I'd put my ModScene - IF I HAD ONE!!!
        ModMetadata modMetadata,
        bool available)
    {
        this.modGUID = modGUID;
        this.assetGUID = assetGUID;
        // this.category = modMetadata.category;
        // this.attribution = modMetadata.attribution;
        this.availableOnThisPlatform = available;
        this.creatorNames = modMetadata.Creators;
    }
    
}