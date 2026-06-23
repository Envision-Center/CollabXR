using System.Collections;
using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.UI
{
	public class CanvasController : MonoBehaviour
	{
		public GameObject compositor;
		public GameObject canvas;

		// Start is called before the first frame update
		void Start()
		{
			bool isQuest = (HardwareConfig.type == HardwareType.DepthQuest || HardwareConfig.type == HardwareType.NonDepthQuest);
			int targetLayer = isQuest ? LayerMask.NameToLayer("OVR UI") : LayerMask.NameToLayer("UI");
			compositor.SetActive(isQuest);
			canvas.layer = targetLayer;
			foreach (Transform t in canvas.transform)
			{
				t.gameObject.layer = targetLayer;
			}
		}

		// Update is called once per frame
		void Update() { }
	}
}
