using System;
using CollabXR.Tools;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace CollabXR.Objects.Components.Radiation
{
	public class SurveyMeter : NetworkBehaviour
	{
		public float multiplier;

		public float minCpmToBeep = 1000;
		public float countsPerMinute;

		[SerializeField]
		private Knob knob;

		[SerializeField]
		private GaugeNeedle needle;
		private AudioSource audioSource;

		private NetworkGrabbable grabbable;

		private RigHand holdingHand;
		private bool probeIsPaired;

		private Random random;

		[Networked]
		public RadiationProbe PairedProbe { get; set; }

		private void Awake()
		{
			grabbable = GetComponent<NetworkGrabbable>();

			grabbable.onGrabLocal.AddListener(OnGrab);
			grabbable.onLetGoLocal.AddListener(OnLetGo);

			audioSource = GetComponent<AudioSource>();
			random = new Random((uint)DateTime.Now.Millisecond);

			knob.onValueChange.AddListener(SetMultiplier);
		}

		private void OnDestroy()
		{
			grabbable.onGrabLocal.RemoveListener(OnGrab);
			grabbable.onLetGoLocal.RemoveListener(OnLetGo);
		}

		public void OnGrab(Grabber grabber)
		{
			TryPairWithGrabbable(grabber.OtherGrabber.HeldGrabbable);
			grabber.OtherGrabber.onGrab.AddListener(TryPairWithGrabbable);
		}

		public void OnLetGo(Grabber grabber)
		{
			grabber.OtherGrabber.onGrab.RemoveListener(TryPairWithGrabbable);
			PairedProbe = null;
		}

		private void TryPairWithGrabbable(NetworkGrabbable g)
		{
			if (g == null)
				return;

			PairedProbe = g.GetComponentInChildren<RadiationProbe>();
		}

		public void SetMultiplier(float f)
		{
			multiplier = f;
		}

		public void FixedUpdate()
		{
			countsPerMinute = 0;

			if (PairedProbe != null)
				countsPerMinute = PairedProbe.Detect() * multiplier;

			needle.SetValue(countsPerMinute);

			float rand = Mathf.Abs(NextGaussian.Float(ref random));

			if (countsPerMinute * rand > minCpmToBeep)
				audioSource.Play();
		}
	}

	public class NextGaussian
	{
		// https://stackoverflow.com/a/218600
		public static float Float(ref Random random)
		{
			float u1 = 1.0f - random.NextFloat(); //uniform(0,1] random doubles
			float u2 = 1.0f - random.NextFloat();
			float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2); //random normal(0,1)

			return randStdNormal;
		}
	}
}
