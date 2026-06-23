using System;
using Fusion;
using TMPro;
using UnityEngine;

namespace CollabXR.Avatar
{
	public enum AvatarDataType
	{
		ShirtColor,
		SkinColor,
		HeadStyle,
		HairColor,
		HairStyle,
		EyeStyle,
		EyebrowStyle,
		GlassesStyle,
		MouthStyle,
		NoseStyle,
		AccessoryStyle,
	}

	public class AvatarRig : MonoBehaviour
	{
		public MaterialOption materialOptions;
		public MeshOption headStyles,
			hairStyles;
		public TextureOption eyeStyles,
			eyebrowStyles,
			glassesStyles,
			mouthStyles,
			noseStyles;
		public AccessoryOption accessoryStyles;
		public MeshRenderer body,
			leftSleeve,
			rightSleeve,
			head,
			leftHand,
			rightHand,
			hair,
			decals;
		public MeshFilter headMesh,
			hairMesh,
			decalMesh;
		public Transform hatRoot;
		private GameObject accessoryInstance;
		public bool accessoryVisible = true;

		private readonly int BrowMaterialIndex = 3;

		private readonly int EyeMaterialIndex = 2;

		private readonly int GlassesMaterialIndex = 6;

		private readonly int MouthMaterialIndex = 4;

		private readonly int NoseMaterialIndex = 5;

		private AvatarCustomizationData data;
		private bool initialized;
		private Material shirtMat,
			skinMat,
			hairMat;

		private void InitMaterials()
		{
			shirtMat = new Material(materialOptions.baseMaterial);
			skinMat = new Material(materialOptions.baseMaterial);
			hairMat = new Material(materialOptions.baseMaterial);
			initialized = true;
		}

		public void SetData(AvatarCustomizationData myData)
		{
			if (!initialized)
				InitMaterials();
			data = myData;

			// shirt color
			shirtMat.color = data.shirtColor;
			shirtMat.SetColor("_EmissionColor", 0.25f * data.shirtColor);
			body.material = shirtMat;
			leftSleeve.material = shirtMat;
			rightSleeve.material = shirtMat;

			// skin color
			skinMat.color = data.skinColor;
			skinMat.SetColor("_EmissionColor", 0.25f * data.skinColor);
			head.material = skinMat;
			leftHand.material = skinMat;
			rightHand.material = skinMat;

			// hair color
			hairMat.color = data.hairColor;
			hairMat.SetColor("_EmissionColor", 0.25f * data.hairColor);
			hair.material = hairMat;

			// head style
			headMesh.mesh = headStyles.options[myData.headStyle];
			decalMesh.mesh = headStyles.options[myData.headStyle];
			hairMesh.mesh = hairStyles.options[myData.hairStyle];

			// eye style
			decals.materials[EyeMaterialIndex].SetTexture("_BaseMap", eyeStyles.textures[data.eyeStyle]);
			decals.materials[EyeMaterialIndex].SetTexture("_EmissionMap", eyeStyles.textures[data.eyeStyle]);
			// eyebrow style
			decals.materials[BrowMaterialIndex].SetTexture("_BaseMap", eyebrowStyles.textures[data.eyebrowStyle]);
			decals.materials[BrowMaterialIndex].SetTexture("_EmissionMap", eyebrowStyles.textures[data.eyebrowStyle]);
			// glasses style
			decals.materials[GlassesMaterialIndex].SetTexture("_BaseMap", glassesStyles.textures[data.glassesStyle]);
			decals.materials[GlassesMaterialIndex].SetTexture("_EmissionMap", glassesStyles.textures[data.glassesStyle]);
			// mouth style
			decals.materials[MouthMaterialIndex].SetTexture("_BaseMap", mouthStyles.textures[data.mouthStyle]);
			decals.materials[MouthMaterialIndex].SetTexture("_EmissionMap", mouthStyles.textures[data.mouthStyle]);
			// nose style
			decals.materials[NoseMaterialIndex].SetTexture("_BaseMap", noseStyles.textures[data.noseStyle]);
			decals.materials[NoseMaterialIndex].SetTexture("_EmissionMap", noseStyles.textures[data.noseStyle]);
			if (accessoryInstance != null)
			{
				GameObject.Destroy(accessoryInstance);
			}
			AvatarAccessory hatToInstantiate = accessoryStyles.accessories[myData.accessoryStyle];
			if (hatToInstantiate != null && hatToInstantiate.prefab != null)
			{
				accessoryInstance = Instantiate(hatToInstantiate.prefab, hatRoot);
				accessoryInstance.transform.position = hatRoot.transform.position + hatToInstantiate.offset;
				accessoryInstance.SetActive(accessoryVisible);
				AccessoryModel model = accessoryInstance.GetComponent<AccessoryModel>();
				if (model != null)
				{
					model.SetColor(data.shirtColor);
				}
			}
			else
			{
				accessoryInstance = null;
			}
			hair.gameObject.SetActive(!hatToInstantiate.hideHairModel);
			head.gameObject.SetActive(!hatToInstantiate.hideHeadModel);
		}

		public void SetAccessoryVisibility(bool visibility)
		{
			accessoryVisible = visibility;
			accessoryInstance?.SetActive(accessoryVisible);
		}
	}

	[Serializable]
	public struct AvatarCustomizationData : INetworkStruct
	{
		public Color shirtColor;
		public Color skinColor;
		public Color hairColor;
		public int headStyle;
		public int hairStyle;
		public int eyeStyle;
		public int eyebrowStyle;
		public int glassesStyle;
		public int mouthStyle;
		public int noseStyle;
		public int accessoryStyle;

		public AvatarCustomizationData(
			Color shirt,
			Color skin,
			Color hair,
			int headstyle,
			int hairstyle,
			int eyestyle,
			int eyebrowstyle,
			int glassesstyle,
			int mouthstyle,
			int nosestyle,
			int accessorystyle
		)
		{
			shirtColor = shirt;
			skinColor = skin;
			hairColor = hair;

			headStyle = headstyle;
			hairStyle = hairstyle;
			eyeStyle = eyestyle;
			eyebrowStyle = eyebrowstyle;
			glassesStyle = glassesstyle;
			mouthStyle = mouthstyle;
			noseStyle = nosestyle;
			accessoryStyle = accessorystyle;
		}

		public AvatarCustomizationData(AvatarCustomizationData baseData)
		{
			this = baseData;
		}
	}
}
