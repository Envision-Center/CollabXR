using System.Collections;
using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.XR;

namespace CollabXR
{
	public class ToolShortcutButton : MonoBehaviour
	{
		[SerializeField]
		private GameObject targetTool;
		private int toolActiveBeforeTarget;
		private bool targetToolActivated;

		public void ActivateTool(bool b)
		{
			if (b)
				ActivateTool();
			else
				DeactivateToolAndSwitchBackToPrevious();
		}

		public void ActivateTool()
		{
			if (targetToolActivated || targetTool.activeSelf)
				return;

			targetToolActivated = true;

			for (int i = 0; i < transform.childCount; i++)
			{
				GameObject g = transform.GetChild(i).gameObject;
				if (g != targetTool && g.activeSelf)
				{
					toolActiveBeforeTarget = i;
					break;
				}
			}

			targetTool.SetActive(true);
		}

		public void DeactivateToolAndSwitchBackToPrevious()
		{
			if (!targetToolActivated)
				return;

			targetToolActivated = false;

			transform.GetChild(toolActiveBeforeTarget).gameObject.SetActive(true);
		}

		private void OnDisable()
		{
			DeactivateToolAndSwitchBackToPrevious();
		}
	}
}
