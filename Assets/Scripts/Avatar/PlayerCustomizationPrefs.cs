using CollabXR.Avatar;
using UnityEngine;

namespace CollabXR.Avatar
{
	[CreateAssetMenu(menuName = "CollabXR/Avatar Data Container", fileName = "Avatar Data")]
	public class PlayerCustomizationPrefs : ScriptableObject
	{
		public AvatarCustomizationData BuiltAvatar { get; set; }
		public string Username { get; set; }

		public void LoadAvatarData(AvatarCustomizationData defaultAvatar)
		{
			string savedAvatar = PlayerPrefs.GetString("BuiltAvatar", "default");
			Username = "User";

			if (savedAvatar.Equals("default"))
				BuiltAvatar = defaultAvatar;
			else
				BuiltAvatar = JsonUtility.FromJson<AvatarCustomizationData>(savedAvatar);
		}

		public void SaveAvatarData(AvatarCustomizationData currentAvatar)
		{
			BuiltAvatar = currentAvatar;
			string avatarJSON = JsonUtility.ToJson(BuiltAvatar);
			PlayerPrefs.SetString("BuiltAvatar", avatarJSON);
		}
	}
}
