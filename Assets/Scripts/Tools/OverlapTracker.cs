using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects
{
	public class OverlapTracker : MonoBehaviour
	{
		public List<GameObject> overlappingObjects { get; private set; } = new();

		public GameObject LatestOverlap { get; private set; }
		private GameObject previousOverlap;

		public bool IsOverlappingAything { get; private set; }
		private bool wasOverlappingAnything;

		public UnityEvent<GameObject> onNewLastOverlap;
		public UnityEvent<List<GameObject>> onOverlappingChange;
		public UnityEvent<bool> isTouchingAnything;
		public UnityEvent<bool> isNotTouchingAnything;

		private void Awake()
		{
			isTouchingAnything.AddListener((bool b) => isNotTouchingAnything.Invoke(!b));
		}

		private void ApplyOverlappingObjectsChange()
		{
			previousOverlap = LatestOverlap;
			LatestOverlap = GetLast();

			wasOverlappingAnything = IsOverlappingAything;
			IsOverlappingAything = overlappingObjects.Count > 0;

			if (LatestOverlap != previousOverlap)
				onNewLastOverlap.Invoke(LatestOverlap);

			if (IsOverlappingAything != wasOverlappingAnything)
				isTouchingAnything.Invoke(IsOverlappingAything);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!overlappingObjects.Contains(other.gameObject))
			{
				overlappingObjects.Add(other.gameObject);
				ApplyOverlappingObjectsChange();
			}
		}

		private void OnTriggerExit(Collider other)
		{
			overlappingObjects.Remove(other.gameObject);
			ApplyOverlappingObjectsChange();
		}

		private void OnDisable()
		{
			overlappingObjects.Clear();
			ApplyOverlappingObjectsChange();
		}

		private GameObject GetLast()
		{
			if (overlappingObjects.Count > 0)
				return overlappingObjects[overlappingObjects.Count - 1];

			return null;
		}

		private void FixedUpdate()
		{
			bool changedOverlappingObjects = false;

			for (int i = overlappingObjects.Count - 1; i > -1; i--)
			{
				if (overlappingObjects[i] == null || !overlappingObjects[i].activeInHierarchy)
				{
					changedOverlappingObjects = true;
					overlappingObjects.RemoveAt(i);
				}
			}

			if (changedOverlappingObjects)
			{
				ApplyOverlappingObjectsChange();
			}
		}
	}
}
