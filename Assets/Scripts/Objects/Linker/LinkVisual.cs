using UnityEngine;

namespace CollabXR.Objects.Linker
{
	[RequireComponent(typeof(LineRenderer))]
	public class LinkVisual : MonoBehaviour
	{
		private LineRenderer line;
		public Transform pointA;
		public Transform pointB;

		void Start()
		{
			line = GetComponent<LineRenderer>();
		}

		void Update()
		{
			line.SetPosition(0, pointA.position);
			line.SetPosition(1, pointB.position);
		}
	}
}
