using System;
using CollabXR.Tools.Palette;
using CollabXR.VR;
using UnityEngine;

namespace CollabXR.Tools
{
	public class ToolPalette : MonoBehaviour
	{
		public static ToolPalette Left { get; private set; }
		public static ToolPalette Right { get; private set; }

		public static ToolPalette Get(bool isRight)
		{
			return isRight ? Right : Left;
		}

		public RigHand Hand { get; private set; }

		private RigHandRef handRef;

		private void Awake()
		{
			handRef = this.GetRigHandRef();
			handRef?.Hand.AddListener(OnGetHand);
		}

		[SerializeField]
		private SingleChildActivator toolActivator;

		private void OnGetHand(RigHand hand)
		{
			Hand = hand;

			if (Hand.isRight)
				Right = this;
			else
				Left = this;
		}

		//private void Start()
		//{
		//    ActivateTool(0);
		//}

		public void ActivateTool(int index) => toolActivator.SetActiveChild(index);
	}
}
