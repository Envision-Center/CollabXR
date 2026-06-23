using CollabXR.Networking;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.Avatar
{
	public class SpeakerDisplay : MonoBehaviour
	{
		[SerializeField]
		private Canvas canvas;

		[SerializeField]
		private Image micIcon,
			deafenedIcon;

		[SerializeField]
		private Color grey,
			white;

		[SerializeField]
		private Speaker speaker;

		[SerializeField]
		private Sprite micOn,
			micMuted,
			headphonesOn,
			headphonesDeafened;
		private Transform camTransform;
		private NetworkPlayer player;
		private AudioSource source;

		// Start is called before the first frame update

		private void OnEnable()
		{
			source = speaker.GetComponent<AudioSource>();
			Camera cam = Camera.main;
			player = GetComponentInParent<NetworkPlayer>();
			if (cam != null)
				camTransform = cam.transform;
			NetworkManager.Instance.OnDeafenChange += UpdateDeafenStatus;
		}

		// Update is called once per frame
		private void Update()
		{
			micIcon.color = (!player.muted && speaker.IsPlaying) ? white : grey;
			micIcon.sprite = player.muted ? micMuted : micOn;
			deafenedIcon.color = player.deafened ? white : grey;
			deafenedIcon.sprite = player.deafened ? headphonesDeafened : headphonesOn;
			Vector3 targetLook = new(camTransform.position.x, canvas.transform.position.y, camTransform.position.z);
			canvas.transform.LookAt(targetLook);
		}

		private void OnDestroy()
		{
			NetworkManager.Instance.OnDeafenChange -= UpdateDeafenStatus;
		}

		private void UpdateDeafenStatus(bool deafened)
		{
			source.volume = deafened ? 0 : 1;
		}
	}
}
