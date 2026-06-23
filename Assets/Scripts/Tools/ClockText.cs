using TMPro;
using UnityEngine;

namespace CollabXR.Tools
{
	public class ClockTextMesh : MonoBehaviour
	{
		private TMP_Text textMesh;

		void Awake()
		{
			textMesh = GetComponent<TMP_Text>();
		}

		void Update()
		{
			textMesh.text = System.DateTime.Now.ToString("hh:mm");
		}
	}
}
