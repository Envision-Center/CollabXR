using System;
using CollabXR;
using UnityEngine;

namespace CollabXR.Tools
{
	public class ToolAnimator : MonoBehaviour
	{
		[SerializeField]
		private Transform toolObject;

		[SerializeField]
		private Transform hoverPosition;

		[SerializeField]
		private Transform activePosition;

		[SerializeField]
		private Transform leavePosition;

		[SerializeField]
		private float animationTime = 0.15f;

		[SerializeField]
		private float leaveTime = 0.1f;

		[SerializeField]
		private EaseType easeType = EaseType.Linear;

		public void AnimateToolEquip(Action onComplete = null)
		{
			toolObject.localPosition = hoverPosition.localPosition;

			this.GenericTween(
				toolObject,
				toolObject.localPosition,
				activePosition.localPosition,
				animationTime,
				easeType,
				v => toolObject.localPosition = v,
				(a, b, t) => Vector3.Lerp(a, b, t),
				onComplete
			);
		}

		public void AnimateToolDisequip(Action onComplete = null)
		{
			toolObject.localPosition = activePosition.localPosition;
			this.GenericTween(
				toolObject,
				toolObject.localPosition,
				hoverPosition.localPosition,
				animationTime,
				easeType,
				v => toolObject.localPosition = v,
				(a, b, t) => Vector3.Lerp(a, b, t),
				onComplete
			);
		}

		public void ShowToolPreviewLeave(Action onComplete = null)
		{
			toolObject.localPosition = hoverPosition.localPosition;

			this.GenericTween(
				toolObject,
				toolObject.localPosition,
				leavePosition.localPosition,
				leaveTime,
				easeType,
				v => toolObject.localPosition = v,
				(a, b, t) => Vector3.Lerp(a, b, t),
				onComplete
			);
		}

		public void ShowToolPreview()
		{
			toolObject.localPosition = hoverPosition.localPosition;
		}
	}
}
