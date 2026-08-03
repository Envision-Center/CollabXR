using System;
using System.Collections.Generic;
using CollabXR.Colocation;
using CollabXR.ModLoader;
using CollabXR.Networking;
using CollabXR.VR;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Modding
{
	public class RepositoryScanner : InputFieldScanner
	{
		public TextMeshProUGUI outputLabel;
		private void OnEnable()
		{
			outputLabel.text = "";
		}
		public void AddNewRepository()
		{
			RepositoryManager.RepositoryAddResult result = RepositoryManager.AddRepository(targetField.text, true);
			targetField.text = "";
			switch (result)
			{
				case RepositoryManager.RepositoryAddResult.Success:
					outputLabel.text = "Repository added.";
					break;
				case RepositoryManager.RepositoryAddResult.Duplicate:
					outputLabel.text = "Repository already loaded.";
					break;
				case RepositoryManager.RepositoryAddResult.InvalidURL:
					outputLabel.text = "Repository must be a valid URL.";
					break;
			}
		}

		public void RefreshRepositories()
		{
			RepositoryManager.RefreshAllMods();
		}
	}
}
