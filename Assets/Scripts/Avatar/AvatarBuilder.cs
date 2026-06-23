using CollabXR.Networking;
using CollabXR.UI;
using TMPro;
using UnityEngine;

namespace CollabXR.Avatar
{
	public class AvatarBuilder : MonoBehaviour
	{
		public AvatarCustomizationData building;
		public Color defaultShirt,
			defaultSkin,
			defaultHair;
		public AvatarRig rig;

		public AvatarDataOption shirtColor,
			skinColor,
			hairColor,
			headStyle,
			hairStyle,
			eyeStyle,
			eyebrowStyle,
			glassesStyle,
			mouthStyle,
			noseStyle,
			accessoryStyle;

		[SerializeField]
		private PlayerCustomizationPrefs prefs;

		[SerializeField]
		private UsernameField username;

		private AvatarCustomizationData defaultAvatar;

		private void Awake()
		{
			defaultAvatar = new AvatarCustomizationData(
				defaultShirt,
				defaultSkin,
				defaultHair,
				headStyle.defaultIndex,
				hairStyle.defaultIndex,
				eyeStyle.defaultIndex,
				eyebrowStyle.defaultIndex,
				glassesStyle.defaultIndex,
				mouthStyle.defaultIndex,
				noseStyle.defaultIndex,
				accessoryStyle.defaultIndex
			);

			prefs.LoadAvatarData(defaultAvatar);
			rig.SetData(prefs.BuiltAvatar);
			building = new AvatarCustomizationData(prefs.BuiltAvatar);
			UpdateRig();
		}

		private void UpdateRig()
		{
			rig.SetData(building);
			prefs.SaveAvatarData(building);
			if (Networking.NetworkPlayer.LocalPlayer != null)
			{
				Networking.NetworkPlayer.LocalPlayer.Avatar = building;
			}
			username.SetField(prefs.Username);
		}

		public void SetColor(AvatarDataType type, Color c)
		{
			switch (type)
			{
				case AvatarDataType.ShirtColor:
					building.shirtColor = c;
					break;
				case AvatarDataType.SkinColor:
					building.skinColor = c;
					break;
				case AvatarDataType.HairColor:
					building.hairColor = c;
					break;
			}

			UpdateRig();
		}

		public Color GetColor(AvatarDataType type)
		{
			switch (type)
			{
				case AvatarDataType.ShirtColor:
					return building.shirtColor;
				case AvatarDataType.SkinColor:
					return building.skinColor;
				case AvatarDataType.HairColor:
					return building.hairColor;
			}

			return Color.white;
		}

		public void SetStyle(AvatarDataType type, int s)
		{
			switch (type)
			{
				case AvatarDataType.HeadStyle:
					building.headStyle = s;
					break;
				case AvatarDataType.HairStyle:
					building.hairStyle = s;
					break;
				case AvatarDataType.EyeStyle:
					building.eyeStyle = s;
					break;
				case AvatarDataType.EyebrowStyle:
					building.eyebrowStyle = s;
					break;
				case AvatarDataType.GlassesStyle:
					building.glassesStyle = s;
					break;
				case AvatarDataType.MouthStyle:
					building.mouthStyle = s;
					break;
				case AvatarDataType.NoseStyle:
					building.noseStyle = s;
					break;
				case AvatarDataType.AccessoryStyle:
					building.accessoryStyle = s;
					break;
			}
			UpdateRig();
		}

		public int GetStyle(AvatarDataType type)
		{
			switch (type)
			{
				case AvatarDataType.HeadStyle:
					return building.headStyle;
				case AvatarDataType.HairStyle:
					return building.hairStyle;
				case AvatarDataType.EyeStyle:
					return building.eyeStyle;
				case AvatarDataType.EyebrowStyle:
					return building.eyebrowStyle;
				case AvatarDataType.GlassesStyle:
					return building.glassesStyle;
				case AvatarDataType.MouthStyle:
					return building.mouthStyle;
				case AvatarDataType.NoseStyle:
					return building.noseStyle;
				case AvatarDataType.AccessoryStyle:
					return building.accessoryStyle;
			}

			return -1;
		}
	}
}
