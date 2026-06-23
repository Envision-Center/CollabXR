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

		private void OnTriggerEnter(Collider other)
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

		private void OnTriggerExit(Collider other)
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
