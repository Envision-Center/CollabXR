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
		private float scoreTimeframe = 0.3f;

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

		void PassFirst(CollabObject obj)
		{
			currentObject = obj;
			lastTriggerTime = Time.fixedTime;
		}

		void PassSecond(CollabObject obj)
		{
			if (obj == currentObject && (Time.fixedTime - lastTriggerTime) <= scoreTimeframe)
			{
				Score();
				currentObject = null;
			}
		}

		void Score()
		{
			particles.Play();
			onScore.Invoke();
		}
	}
}
