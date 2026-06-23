using CollabXR.Objects.Components;
using CollabXR.Tools;
using UnityEngine;

namespace CollabXR.Objects
{
	public class TwoHandedGrabTransform : SingletonBehavior<TwoHandedGrabTransform>
	{
		public void SetTransform(NetworkGrabbable grabbable, Grabber primaryGrabber, Grabber secondaryGrabber, Vector3 primaryLocalPosition, Vector3 secondaryLocalPosition)
		{
			Transform t = grabbable.transform;
			Transform primary = primaryGrabber.transform;
			Transform secondary = secondaryGrabber.transform;
			Vector3 targetPos = t.TransformPoint(t.InverseTransformPoint(primary.position) - primaryLocalPosition);
			transform.position = targetPos;
			Quaternion targetRot = Quaternion.LookRotation(primary.position - secondary.position);
			transform.rotation = targetRot;
		}
	}
}
