using UnityEngine;

namespace CollabXR.UI
{
	public class TiltOnStart : MonoBehaviour
	{
		[SerializeField]
		private float angle;

		void Start()
		{
			Vector3 eulerAngles = transform.localEulerAngles;
			eulerAngles.x = angle;

			transform.localEulerAngles = eulerAngles;
		}
	}
}
