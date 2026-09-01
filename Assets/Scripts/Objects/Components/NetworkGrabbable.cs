using System.Collections.Generic;
using CollabXR.Tools;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Components
{
	public class NetworkGrabbable : NetworkBehaviour, IStateAuthorityChanged
	{
		public UnityEvent<Grabber> onGrabLocal;
		public UnityEvent<Grabber> onLetGoLocal;

		private readonly Dictionary<GameObject, int> originalLayers = new();

		private bool ignoreRaycast;
		private Grabber primaryGrabber,
			secondaryGrabber;

		public enum HoldState
		{
			None,
			OneHanded,
			TwoHanded,
		};

		public HoldState Holding;
		public bool IsHeldLocally => Holding == HoldState.OneHanded || Holding == HoldState.TwoHanded;
		private Vector3 primaryLocalGrabPosition,
			secondaryLocalGrabPosition;

		/// <summary>
		/// The initial uniform scale of the object.
		/// </summary>
		private float initialGrabScale;

		private float initialTwoHandedGrabDistance;
		private IFollower follower;
		private RigHandRef handRef;
		private NetworkScalable scalable;
		private SpawnableObject spawnableObject;

		public bool IsIndepedentObject => spawnableObject.IsIndepedentObject;
		public NetworkGrabbable ParentGrabbable => IsIndepedentObject ? null : transform.root.GetComponentInChildren<NetworkGrabbable>(); // finds the top-level grabbable in case of parenting

		private void Awake()
		{
			handRef = GetComponent<RigHandRef>();
			follower = GetComponent<IFollower>();
			scalable = GetComponent<NetworkScalable>();
			spawnableObject = GetComponent<SpawnableObject>();
		}

		private void OnDisable()
		{
			if (IsHeldLocally)
				ReleaseAllHands();
		}

		private void LateUpdate()
		{
			if (Holding == HoldState.TwoHanded)
			{
				float currentTwoHandedGrabDistance = Vector3.Distance(primaryGrabber.transform.position, secondaryGrabber.transform.position);
				float ratio = currentTwoHandedGrabDistance / initialTwoHandedGrabDistance;
				scalable?.SetUniformScale(initialGrabScale * ratio);

				TwoHandedGrabTransform.Instance.SetTransform(this, primaryGrabber, secondaryGrabber, primaryLocalGrabPosition, secondaryLocalGrabPosition);
				transform.position = TwoHandedGrabTransform.Instance.transform.position;
				//transform.rotation = TwoHandedGrabTransform.Instance.transform.rotation;
			}
		}

		public void StateAuthorityChanged()
		{
			if (IsHeldLocally && !Object.HasStateAuthority)
				ReleaseAllHands();
		}

		public void Grab(Grabber grabber)
		{
			Object.RequestStateAuthority();
			if (Holding == HoldState.None)
				HoldLocally(grabber, null);
			else if (Holding == HoldState.OneHanded)
				HoldLocally(primaryGrabber, grabber);
		}

		protected virtual void HoldLocally(Grabber grabber, Grabber grabber2)
		{
			primaryGrabber = grabber;
			secondaryGrabber = grabber2;
			UpdateHoldingState();
			initialGrabScale = (scalable != null) ? scalable.uniformNetworkScale : 1.0f;
			if (Holding == HoldState.OneHanded)
			{
				primaryLocalGrabPosition = transform.InverseTransformPoint(primaryGrabber.transform.position);
				initialTwoHandedGrabDistance = 0;
			}
			else if (Holding == HoldState.TwoHanded)
			{
				secondaryLocalGrabPosition = transform.InverseTransformPoint(secondaryGrabber.transform.position);
				initialTwoHandedGrabDistance = Vector3.Distance(primaryGrabber.transform.position, secondaryGrabber.transform.position);
			}

			IgnoreRaycast();

			onGrabLocal.Invoke(primaryGrabber);
		}

		public void Release(Grabber grabber)
		{
			if (primaryGrabber == grabber)
			{
				primaryGrabber = null;
				if (secondaryGrabber != null)
					HoldLocally(secondaryGrabber, null);
			}
			else if (secondaryGrabber == grabber)
			{
				secondaryGrabber = null;
			}
			UpdateHoldingState();
			onLetGoLocal.Invoke(grabber);
		}

		public void ReleaseAllHands()
		{
			if (Holding != HoldState.None)
			{
				primaryGrabber.LetGo();
				if (Holding == HoldState.TwoHanded)
				{
					secondaryGrabber.LetGo();
				}
			}
		}

		public void UpdateHoldingState()
		{
			if (primaryGrabber == null)
			{
				Holding = HoldState.None;

				((MonoBehaviour)follower)?.SetEnabled(false);
				if (handRef != null)
					handRef.Hand.Value = null;
				RestoreLayers();
			}
			else if (secondaryGrabber == null)
			{
				Holding = HoldState.OneHanded;

				follower?.SetTarget(primaryGrabber);
				((MonoBehaviour)follower)?.SetEnabled(true);
				if (handRef != null)
					handRef.Hand.Value = primaryGrabber.handRef.Hand.Value;
			}
			else
			{
				Holding = HoldState.TwoHanded;

				((MonoBehaviour)follower)?.SetEnabled(false);
				if (handRef != null)
					handRef.Hand.Value = primaryGrabber.handRef.Hand.Value;
			}
			Debug.Log($"{Holding}, {primaryGrabber?.transform.parent.name}, {secondaryGrabber?.transform.parent.name}");
		}

		private void IgnoreRaycast()
		{
			if (ignoreRaycast)
				return;

			ignoreRaycast = true;

			IgnoreRaycast(transform);
		}

		private void IgnoreRaycast(Transform t)
		{
			GameObject g = t.gameObject;
			originalLayers[g] = g.layer;
			g.layer = 2;

			for (int i = 0; i < t.childCount; i++)
				IgnoreRaycast(t.GetChild(i));
		}

		private void RestoreLayers()
		{
			ignoreRaycast = false;

			RestoreLayers(transform);
		}

		private void RestoreLayers(Transform t)
		{
			GameObject g = t.gameObject;
			if (originalLayers.ContainsKey(g))
			{
				g.layer = originalLayers[g];
			}

			for (int i = 0; i < t.childCount; i++)
				RestoreLayers(t.GetChild(i));
		}

		private void OnDestroy()
		{
			ReleaseAllHands();
		}
	}
}
