using System;
using Fusion;
using UnityEngine;
using UnityEngine.XR.Management;

namespace CollabXR.VR
{
	public class HardwareRig : SingletonBehavior<HardwareRig>
	{
		public static Action OnHardwareRigStart = delegate { };
		public Transform root,
			actualHead,
			actualLeftHand,
			actualRightHand;

		private string toolLeft,
			toolRight;

		private void Start()
		{
			OnHardwareRigStart.Invoke();

			//SceneManager.sceneLoaded += OnSceneLoaded;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			//SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		//private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ResetPlayerRoot();

		public RigState GetRigState() // get current hardware state
		{
			RigState rig = new();
			rig.leftHandPosition = actualLeftHand.position;
			rig.rightHandPosition = actualRightHand.position;
			rig.headPosition = actualHead.position;
			rig.leftHandRotation = actualLeftHand.rotation;
			rig.rightHandRotation = actualRightHand.rotation;
			rig.headRotation = actualHead.rotation;
			rig.leftHandTool = toolLeft;
			rig.rightHandTool = toolRight;
			return rig;
		}

		public void MovePersonTo(Vector3 position)
		{
			Vector3 personRelativeToRoot = actualHead.position - root.position;
			personRelativeToRoot.y = 0f;
			root.position = position - personRelativeToRoot;
		}

		public void RotatePersonBy(float a)
		{
			root.RotateAround(actualHead.position, Vector3.up, a);
		}

		public void TransformPlayspace(Pose from, Pose to, bool enforceUp = true)
		{
			Matrix4x4 rigMat = Matrix4x4.TRS(root.position, root.rotation, Vector3.one);
			Matrix4x4 anchorMat = Matrix4x4.TRS(from.position, from.rotation, Vector3.one);
			Matrix4x4 desiredMat = Matrix4x4.TRS(to.position, to.rotation, Vector3.one);

			if (enforceUp)
			{
				Vector3 f = anchorMat * Vector3.forward;
				Vector3 flatForward = new Vector3(f.x, 0, f.z).normalized;
				Quaternion correctedRot = Quaternion.LookRotation(flatForward, Vector3.up);
				anchorMat = Matrix4x4.TRS(anchorMat.GetPosition(), correctedRot, Vector3.one);
			}

			// the rig relative to the anchor
			Matrix4x4 rigLocalToAnchor = anchorMat.inverse * rigMat;

			// that relative matrix relative to the desired transform
			Matrix4x4 relativeToDesired = desiredMat * rigLocalToAnchor;

			Vector3 targetRigPos = relativeToDesired.GetPosition();

			root.SetPositionAndRotation(targetRigPos, relativeToDesired.rotation);
		}
	}

	public struct RigState : INetworkStruct
	{
		[Networked]
		public Vector3 headPosition { get; set; }
		public Vector3 leftHandPosition { get; set; }
		public Vector3 rightHandPosition { get; set; }
		public Quaternion headRotation { get; set; }
		public Quaternion leftHandRotation { get; set; }
		public Quaternion rightHandRotation { get; set; }
		public NetworkString<_32> leftHandTool { get; set; }
		public NetworkString<_32> rightHandTool { get; set; }
	}
}
