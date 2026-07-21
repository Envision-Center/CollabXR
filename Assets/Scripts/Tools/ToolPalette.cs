using System;
using CollabXR.Tools.Palette;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;

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

		/// <summary>
		/// On user selects new tool, passes the previous tool and the new tool.
		/// </summary>
		public UnityEvent<Transform, Transform> onToolChange;

		public RigHand Hand { get; private set; }

		private RigHandRef handRef;

		private Transform selectedToolTransform;

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

		public void ActivateTool(int index)
		{
			(Transform prevTool, Transform curTool) = toolActivator.SetActiveChild(index);

			curTool.GetComponent<ToolAnimator>()?.AnimateToolEquip(() => onToolChange?.Invoke(prevTool, curTool));
			selectedToolTransform = curTool;
		}

		public void ActivateToolPreview(int index)
		{
			(Transform prevTool, Transform curTool) = toolActivator.SetActiveChild(index);

			if (prevTool?.TryGetComponent(out ToolAnimator prevAnimator) == true)
			{
				toolActivator.SetActiveChild(prevTool);
				prevAnimator.ShowToolPreviewLeave(() =>
				{
					toolActivator.SetActiveChild(curTool);
					curTool.GetComponent<ToolAnimator>()?.ShowToolPreview();
				});
			}
			else
			{
				curTool.GetComponent<ToolAnimator>()?.ShowToolPreview();
			}
		}

		public void DeEquipTool(Action onComplete = null)
		{
			var animator = selectedToolTransform?.GetComponent<ToolAnimator>();
			if (animator != null)
				animator.AnimateToolDisequip(onComplete);
			else
				onComplete?.Invoke();

			selectedToolTransform = null;
		}
	}
}
