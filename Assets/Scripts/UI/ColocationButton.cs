using CollabXR.Colocation;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class ColocationButton : MonoBehaviour
	{
		private Button button;

		private void Awake()
		{
			button = GetComponent<Button>();
		}

		private void Start()
		{
			button.onClick.AddListener(StartCollocation);

			ColocationDriver.Instance?.notBusy.AddListener(SetInteractive);
		}

		private void StartCollocation()
		{
			PassthroughManager.PassthroughOn.Value = true;

			ColocationDriver d = ColocationDriver.Instance;

			if (d != null)
			{
				Transform r = HardwareRig.Instance.root;
				d.Anchor(r.position, r.rotation, () => d.UploadCurrentAnchor(d.ShareCurrentAnchorWithAll));
			}

			GetComponentInParent<GameMenu>().gameObject.SetActive(false);
		}

		private void SetInteractive(bool b)
		{
			button.interactable = b;
		}
	}
}
