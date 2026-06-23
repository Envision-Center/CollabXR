using CollabXR.Networking;
using CollabXR.Objects.Components;
using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;
using NetworkPlayer = CollabXR.Networking.NetworkPlayer;

namespace CollabXR.Tools
{
	public class Grabber : MonoBehaviour
	{
		[SerializeField]
		private Grabber otherGrabber;

		public NetworkGrabbable potentialGrab;
		public Grabber OtherGrabber => otherGrabber;
		public bool IsHolding { get; private set; }
		public NetworkGrabbable HeldGrabbable { get; private set; }
		public RigHandRef handRef { get; private set; }

		public UnityEvent<NetworkGrabbable> onCanGrab;
		public UnityEvent<bool> onCanGrabBool;
		public UnityEvent<NetworkGrabbable> onGrab;
		public UnityEvent<NetworkGrabbable> onLetGo;

		public GameObject controllerButtons;

		private void Awake()
		{
			handRef = GetComponentInParent<RigHandRef>();
		}

		private void Start()
		{
			onCanGrab.Invoke(null);
			onCanGrabBool.Invoke(false);
		}

		public void SetPotentialGrab(GameObject obj)
		{
			potentialGrab = obj?.GetComponentInParent<NetworkGrabbable>();
			if (potentialGrab != null && !potentialGrab.IsIndepedentObject)
			{
				potentialGrab = potentialGrab.ParentGrabbable;
			}

			onCanGrab.Invoke(potentialGrab);
			onCanGrabBool.Invoke(potentialGrab != null);
		}

		public void TryGrab()
		{
			if (!IsHolding && potentialGrab != null)
			{
				GrabSpecific(potentialGrab);
			}
		}

		//private void UpdateBoundsVisualizer()
		//{
		//	if (IsHolding)
		//	{
		//		boundsVisualizer.enabled = true;
		//		boundsVisualizer.SetTrackedObject(HeldGrabbable.gameObject);
		//	}
		//	else
		//	{
		//		boundsVisualizer.enabled = overlappingGrabbables.Count > 0;

		//		if (boundsVisualizer.enabled)
		//			boundsVisualizer.SetTrackedObject(potentialGrab.gameObject);
		//	}
		//}

		public void GrabSpecific(NetworkGrabbable grabbable)
		{
			if (NetworkPlayer.GetLocalRole() == NetworkPlayer.NetworkPlayerRole.Student && !NetworkPermissions.Instance.StudentsCanInteract)
			{
				return;
			}

			if (handRef.Hand.Value.Interactor != null)
			{
				handRef.Hand.Value.Interactor.enabled = false;
			}
			HeldGrabbable = grabbable;
			IsHolding = true;
			//UpdateBoundsVisualizer();
			grabbable.Grab(this);

			//potentialGrab = null;
			onCanGrab.Invoke(null);
			onCanGrabBool.Invoke(false);
			onGrab.Invoke(HeldGrabbable);
		}

		public void LetGo()
		{
			if (handRef.Hand.Value.Interactor != null)
			{
				handRef.Hand.Value.Interactor.enabled = true;
			}

			if (!IsHolding)
				return;

			NetworkGrabbable letGo = HeldGrabbable;

			HeldGrabbable = null;
			IsHolding = false;
			//UpdateBoundsVisualizer();
			if (letGo != null)
			{
				letGo.Release(this);
			}
			onLetGo.Invoke(letGo);
			controllerButtons.SetActive(!HardwareConfig.IsVisionOS);
		}

		private void OnDisable()
		{
			LetGo();
			potentialGrab = null;

			onCanGrab.Invoke(null);
			onCanGrabBool.Invoke(false);
		}
	}
}
