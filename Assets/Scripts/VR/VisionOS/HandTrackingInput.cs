using System;
using System.Collections.Generic;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

namespace CollabXR.Hands
{
	public class HandTrackingInput : MonoBehaviour
	{
		XRHandSubsystem m_HandSubsystem;

		[Serializable]
		public struct HandTransformFollower
		{
			public GameObject fingertip,
				thumbtip,
				wrist;
		}

		[SerializeField]
		public List<HandTransformFollower> handFollowers = new List<HandTransformFollower>(2);
		List<CollabTrackedHandData> handData = new List<CollabTrackedHandData>(new CollabTrackedHandData[2]);

		void Start()
		{
			var handSubsystems = new List<XRHandSubsystem>();
			SubsystemManager.GetSubsystems(handSubsystems);

			for (var i = 0; i < handSubsystems.Count; ++i)
			{
				var handSubsystem = handSubsystems[i];
				if (handSubsystem.running)
				{
					m_HandSubsystem = handSubsystem;
					break;
				}
			}

			if (m_HandSubsystem != null)
			{
				m_HandSubsystem.updatedHands += OnUpdatedHands;
			}
		}

		private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
		{
			XRHand leftHand = subsystem.leftHand;
			XRHand rightHand = subsystem.rightHand;

			handData[0] = SetJoints(leftHand);
			handData[1] = SetJoints(rightHand);
			UpdateHandTrackingDebugging(handFollowers[0], handData[0]);
			UpdateHandTrackingDebugging(handFollowers[1], handData[1]);
		}

		private CollabTrackedHandData SetJoints(XRHand hand)
		{
			CollabTrackedHandData data;
			hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out data.indexFingertip);
			hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out data.thumbTip);
			hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out data.wrist);
			return data;
		}

		private void UpdateHandTrackingDebugging(HandTransformFollower follower, CollabTrackedHandData data)
		{
			follower.fingertip.transform.localPosition = data.indexFingertip.position;
			follower.fingertip.transform.localRotation = data.indexFingertip.rotation;
			follower.thumbtip.transform.localPosition = data.thumbTip.position;
			follower.thumbtip.transform.localRotation = data.thumbTip.rotation;
			follower.wrist.transform.localPosition = data.wrist.position;
			follower.wrist.transform.localRotation = data.wrist.rotation;
		}
	}

	public struct CollabTrackedHandData
	{
		public Pose indexFingertip,
			thumbTip,
			wrist;
	}
}
