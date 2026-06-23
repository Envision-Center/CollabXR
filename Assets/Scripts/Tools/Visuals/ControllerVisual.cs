using CollabXR.VR;
using UnityEngine;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace CollabXR
{
	public class ControllerVisual : MonoBehaviour
	{
		[SerializeField]
		private Transform stick;

		[SerializeField]
		private Transform primaryButton;

		[SerializeField]
		private Transform secondaryButton;

		[SerializeField]
		private Transform trigger;

		[SerializeField]
		private Transform grip;

		private RigHandRef handRef;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
		}

		private void LateUpdate()
		{
			Vector2 primaryAxis = handRef.Hand.Value.Controller.GetFeatureState(CommonUsages.primary2DAxis);

			stick.localEulerAngles = new Vector3(primaryAxis.y, 0, primaryAxis.x * (handRef.Hand.Value.isRight ? 1 : -1)) * 23;

			trigger.localEulerAngles = new Vector3(handRef.Hand.Value.Controller.GetFeatureState(CommonUsages.trigger) * 23, 0, 0);
			// hand.Controller.GetFeatureState(CommonUsages.grip);
			// hand.Controller.GetFeatureState(CommonUsages.primaryButton);
			// hand.Controller.GetFeatureState(CommonUsages.secondaryButton);
		}

		void OnEnable() { }
	}
}
