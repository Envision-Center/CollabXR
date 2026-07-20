using CollabXR.VR;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Tools
{
	[DefaultExecutionOrder(20)]
	public class Raycaster : MonoBehaviour
	{
		public float upwardRayOffset = 0.03f;
		public float forwardRayOffset = 0.05f;
		public bool shouldHitTriggers;
		public bool shouldTestIfStartsInsideCollider = true;
		public LayerMask shouldHitLayers = Physics.DefaultRaycastLayers;

		private Collider[] overlapSphereBuffer = new Collider[10];

		private bool didHit;
		private GameObject hitObject;
		private Vector3 hitPoint;

		public bool DidHit { get; private set; }
		public bool DidHitPrevious { get; private set; }

		public GameObject HitObject { get; private set; }
		public GameObject PreviousHitObject { get; private set; }

		public UnityEvent<GameObject> onHitChange;
		public UnityEvent<GameObject> onEnterObject;
		public UnityEvent<GameObject> onLeaveObject;
		public UnityEvent<Vector3> onHitPoint;
		public UnityEvent<Vector3> onHitNothing;
		public UnityEvent<bool> onDidHitChange;
		public UnityEvent<bool> onEnable;

		private RigHandRef handRef;

		private void Awake()
		{
			onHitChange.Invoke(null);
			onDidHitChange.Invoke(false);
			handRef = GetComponentInParent<RigHandRef>();
		}

		public bool Raycast()
		{
			QueryTriggerInteraction interaction = shouldHitTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

			RigHand hand = handRef.Hand.Value;

			Vector3 origin = hand.usingCustomRaycast ? hand.raycastOrigin.position : transform.position + transform.forward * forwardRayOffset + transform.up * upwardRayOffset;
			Vector3 direction = hand.usingCustomRaycast ? hand.raycastDirection : transform.forward;

			// test origin overlap

			if (shouldTestIfStartsInsideCollider)
			{
				int hits = Physics.OverlapSphereNonAlloc(origin, 0.01f, overlapSphereBuffer, shouldHitLayers, interaction);
				didHit = hits > 0;

				if (didHit)
				{
					// choose closest

					float maxDist = Mathf.Infinity;
					Collider closest = overlapSphereBuffer[0];
					for (int i = 1; i < hits; i++)
					{
						float sqrDist = (overlapSphereBuffer[i].transform.position - origin).sqrMagnitude;
						if (sqrDist < maxDist)
						{
							maxDist = sqrDist;
							closest = overlapSphereBuffer[i];
						}
					}

					hitObject = closest.gameObject;
					hitPoint = origin;

					UpdatePropertiesAndEvents();
					return didHit;
				}
			}

			// raycast

			Ray ray = new(origin, direction);

			didHit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, shouldHitLayers, interaction);

			if (didHit)
			{
				hitObject = hit.collider.gameObject;
				hitPoint = hit.point;

				UpdatePropertiesAndEvents();
				return didHit;
			}

			// didn't hit anything

			hitObject = null;
			hitPoint = origin + direction;

			UpdatePropertiesAndEvents();
			return didHit;
		}

		private void OnEnable()
		{
			onDidHitChange.Invoke(false);
			onEnable.Invoke(true);
		}

		private void OnDisable()
		{
			didHit = false;
			hitObject = null;
			hitPoint = transform.position;

			UpdatePropertiesAndEvents();

			onEnable.Invoke(false);
		}

		private void UpdatePropertiesAndEvents()
		{
			DidHitPrevious = DidHit;
			DidHit = didHit;

			PreviousHitObject = HitObject;
			HitObject = hitObject;

			if (DidHit)
			{
				onHitPoint.Invoke(hitPoint);
			}
			else
			{
				onHitNothing.Invoke(hitPoint);
			}

			if (HitObject != PreviousHitObject)
			{
				onHitChange.Invoke(HitObject);

				if (HitObject != null)
					onEnterObject.Invoke(HitObject);

				if (PreviousHitObject != null)
					onLeaveObject.Invoke(PreviousHitObject);
			}

			if (DidHit != DidHitPrevious)
				onDidHitChange.Invoke(DidHit);
		}

		private void LateUpdate()
		{
			Raycast();
		}
	}
}
