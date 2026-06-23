using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

namespace CollabXR.Input
{
	public class MenuButton : MonoBehaviour
	{
		public UnityEvent OnPress;

		private void Awake()
		{
			GetComponent<XRControllerSubject>().SubscribeToOnPressEvent(CommonUsages.menuButton, OnPress.Invoke);
		}
	}
}
