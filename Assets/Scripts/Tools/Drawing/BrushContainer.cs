using CollabXR.Objects;
using CollabXR.Tools.Drawing;
using UnityEngine;

namespace CollabXR
{
    public class BrushContainer : SpawnableObject
    {
		bool waitingForDeletion;

		private void Update()
		{
			if (waitingForDeletion && GetComponentsInChildren<BrushSubStroke>().Length == 0)
			{
				MarkForDeletion();
			}
		}
		public void MarkForDeletionWhenEmpty()
		{
			waitingForDeletion = true;
		}
    }
}
