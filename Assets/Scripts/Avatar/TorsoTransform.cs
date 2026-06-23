using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Avatar
{
	public class TorsoTransform : MonoBehaviour
	{
		public Transform Head,
			LeftHand,
			RightHand;

		[Tooltip("The vector from the tracked head position (headset) to the neck (torso pivot) in head's local space")]
		public Vector3 HeadOffset;

		public Vector3 EulerOffset;

		public Text weightReadout;

		private void Update()
		{
			transform.rotation = Quaternion.Euler(EulerOffset) * Quaternion.Slerp(HeadRotationFactor(), HandsRotationFactor(), HandsWeight());
			transform.position = Head.position + Head.TransformDirection(HeadOffset);
		}

		private Quaternion HeadRotationFactor()
		{
			Quaternion headFactor = Quaternion.LookRotation(Vector3.Scale(Head.forward, new Vector3(1, 0, 1)), Vector3.up);
			return headFactor;
		}

		private Quaternion HandsRotationFactor()
		{
			Vector3 handsForward = Vector3.Cross(RightHand.position - LeftHand.position, Vector3.up).normalized;
			Quaternion handsFactor;
			if (handsForward != Vector3.zero)
				handsFactor = Quaternion.LookRotation(handsForward, Vector3.up);
			else
				handsFactor = Quaternion.identity;
			return handsFactor;
		}

		private float HandsWeight()
		{
			float weight = 0.5f;
			Vector3 handsFoward = Vector3.Cross(RightHand.position - LeftHand.position, Vector3.up).normalized;
			Debug.DrawRay(0.5f * (LeftHand.position + RightHand.position), handsFoward);
			weight = 1 - Mathf.Clamp(Mathf.Abs(Vector3.Dot(LeftHand.forward, handsFoward) + Vector3.Dot(RightHand.forward, handsFoward)), 0, 1);
			//weightReadout.text = weight.ToString();
			return weight;
		}
	}
}
