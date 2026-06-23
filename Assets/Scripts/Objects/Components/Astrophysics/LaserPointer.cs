using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects.Components.Astrophysics
{
	public class LaserPointer : NetworkBehaviour
	{
		public LineRenderer laser;

		[Networked, OnChangedRender(nameof(OnLaserChange))]
		public bool laserOn { get; set; }

		public void ToggleLaser()
		{
			laserOn = !laserOn;
		}

		public void OnLaserChange()
		{
			UpdateLaserLocally();
		}

		protected void UpdateLaserLocally()
		{
			laser.enabled = laserOn;
		}
	}
}
