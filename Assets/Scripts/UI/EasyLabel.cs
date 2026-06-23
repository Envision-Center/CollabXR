using TMPro;
using UnityEngine;

namespace CollabXR.UI
{
	public class EasyLabel : MonoBehaviour
	{
#if UNITY_EDITOR

		[SerializeField]
		private TMP_Text tmp;

		private void OnValidate()
		{
			if (tmp == null)
			{
				tmp = GetComponentInChildren<TMP_Text>(true);
			}

			if (tmp != null && gameObject != null && isActiveAndEnabled)
			{
				tmp.text = gameObject.name;
			}
		}
#endif
	}
}
