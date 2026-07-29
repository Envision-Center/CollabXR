using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CollabXR.Scriptables;
using System;

namespace CollabXR.UI
{
	public class ObjectLibraryInfoPanel : MonoBehaviour
	{
		[SerializeField]
		List<GameObject> debugOnlyInfo;
		[SerializeField] ScriptableBool debugMode;

		[SerializeField]
		TextMeshProUGUI assetName;

		[SerializeField]
		TextMeshProUGUI attribution;

		[SerializeField]
		Image thumbnail;

		[SerializeField]
		TextMeshProUGUI version;

		[SerializeField]
		TextMeshProUGUI creator;

		[SerializeField]
		TextMeshProUGUI repositoryName;
		[SerializeField]
		TextMeshProUGUI modGUID;
		[SerializeField]
		TextMeshProUGUI assetGUID;

		[Header("Unimplemented placeholder")]
		[SerializeField]
		TextMeshProUGUI description;

		[SerializeField]
		TextMeshProUGUI publishDate;

		public void SetInfoPanel(
			string assetName = default,
			string attribution = default,
			Sprite thumbnail = default,
			List<string> creator = default,
			string version = default,
			string repositoryName = default,
			Guid modGUID = default,
			Guid assetGUID = default
		) {
			UpdateDebugVisibility();

			this.assetName.text = assetName;
			this.thumbnail.sprite = thumbnail;

			// hide if info was not provided
			if (thumbnail != default)
			{
				this.thumbnail.color = new(255, 255, 255, 255);
			}
			else
			{
				this.thumbnail.color = new(0, 0, 0, 0);
			}

			if (creator != null)
			{
				string creatorList = string.Join(", ", creator);
				SetTextField(this.creator, creatorList, "Creator: ");
			}
			else
			{
				this.creator.text = "";
			}

			SetTextField(this.attribution, attribution, "Author: ");
			SetTextField(this.version, version, "Version: ");

			SetTextField(this.repositoryName, repositoryName, "Repository: ");
			SetTextField(this.modGUID, modGUID.ToString(), "Mod GUID: ");
			SetTextField(this.assetGUID, assetGUID.ToString(), "Asset GUID: ");
		}

		public void ToggleVisibility()
		{
			ToggleVisibility(!gameObject.activeSelf);
		}

		public void ToggleVisibility(bool visible)
		{
			gameObject.SetActive(visible);
		}

		public void UpdateDebugVisibility()
		{
			SetDebugVisibility(debugMode.Value);
		}

		public void SetDebugVisibility(bool visible)
		{
			for (int i = 0; i < debugOnlyInfo.Count; i++)
			{
				debugOnlyInfo[i].SetActive(visible);
			}
		}

		// Set textfield if given valid text, hides the UI otherwise
		private void SetTextField(TextMeshProUGUI textField, string newText, string prefix = "")
		{
			if (string.IsNullOrEmpty(newText))
			{
				textField.text = "";
			}
			else
			{
				textField.text = prefix + newText;
			}
		}
	}
}
