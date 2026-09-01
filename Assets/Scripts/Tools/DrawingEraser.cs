using CollabXR.Networking;
using CollabXR.Tools.Drawing;
using UnityEngine;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	public class DrawingEraser : MonoBehaviour
	{
		public bool isErasing;
		public BrushSubStroke overlappingStroke;
		public TriggerForwarder triggerForwarder;

		private void OnEnable()
		{
			triggerForwarder.TriggerEnter += TriggerEnter;
			triggerForwarder.TriggerExit += TriggerExit;
		}

		private void TriggerEnter(Collider other)
		{
			if (!isErasing)
				return;

			if (other.CompareTag("Erasable"))
			{
				BrushSubStroke stroke = other.GetComponent<BrushSubStroke>();
				if (stroke != null)
				{
					overlappingStroke = stroke;
					overlappingStroke.MarkForDeletion();
				}
			}
		}

		private void TriggerExit(Collider other)
		{
			if (other.CompareTag("Erasable"))
			{
				BrushSubStroke stroke = other.GetComponent<BrushSubStroke>();
				if (stroke == overlappingStroke)
					overlappingStroke = null;
			}
		}

		private void OnDisable()
		{
			triggerForwarder.TriggerEnter -= TriggerEnter;
			triggerForwarder.TriggerExit -= TriggerExit;
			SetIsErasing(false);
		}

		public void SetIsErasing(bool isErasing)
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanDelete)
			{
				this.isErasing = false;
				return;
			}

			this.isErasing = isErasing;
		}
	}
}
