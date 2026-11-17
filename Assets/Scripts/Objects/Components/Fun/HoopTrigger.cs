using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Objects.Components.Fun
{
	public class HoopTrigger : MonoBehaviour
	{
		[SerializeField, Tooltip("First physics trigger for detecting object passthrough.")]
		private PhysicsTrigger triggerFirst;

		[SerializeField, Tooltip("Second physics trigger for detecting object passthrough.")]
		private PhysicsTrigger triggerSecond;

		[Header("Scoring")]
		[SerializeField, Tooltip("Max duration it takes an object to move through the hoop before score is ignored.")]
		private float scoreTimeframe = 1.0f;

		[Header("Effects")]
		[SerializeField]
		private ParticleSystem particles;

		[Tooltip("Called upon scoring a goal.")]
		public UnityEvent onScore;

		/// <summary>
		/// Object that last entered the hoop.
		/// </summary>
		private CollabObject currentObject = null;
		private float lastTriggerTime = 0.0f;

		private void Awake()
		{
			if (onScore == null)
			{
				onScore = new UnityEvent();
			}
		}

		private void Start()
		{
			if (triggerFirst != null)
			{
				triggerFirst.objectEntered.AddListener(PassFirst);
			}

			if (triggerSecond != null)
			{
				triggerSecond.objectEntered.AddListener(PassSecond);
			}
		}

		void PassFirst(CollabObject obj, Rigidbody body)
		{
			currentObject = obj;
			lastTriggerTime = Time.fixedTime;
		}

		void PassSecond(CollabObject obj, Rigidbody body)
		{
			if (obj == currentObject && (Time.fixedTime - lastTriggerTime) <= scoreTimeframe)
			{
				currentObject = null;

				// If there is a Rigidbody, and it is not moving downward, reject the event
				if (body != null && Vector3.Dot(body.linearVelocity.normalized, transform.up) > 0.0f) {
					Debug.Log("Hoop Trigger: Rigidbody was not falling through hoop, ignore");
					return;
				}

				Score();
			}
		}

		void Score()
		{
			particles.Play();
			onScore.Invoke();
		}
	}
}
