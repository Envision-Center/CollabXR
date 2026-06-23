using UnityEngine;

namespace CollabXR.Objects
{
	public static class CollabObjectHelper
	{
		public static Object Instantiate(Object original, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
		{
			Object obj = Object.Instantiate(original, position, rotation, parent);
			Object.DontDestroyOnLoad(obj);
			return obj;
		}

		public static bool EnabledInHiearchy(this Behaviour b)
		{
			return b.isActiveAndEnabled && b.gameObject.activeInHierarchy;
		}

		public static void GetOrientedBoundingBoxFromMeshes(GameObject rootObject, out Bounds bounds, out Matrix4x4 transform)
		{
			MeshRenderer[] meshRenderers = rootObject.GetComponentsInChildren<MeshRenderer>();
			GetOrientedBoundingBoxFromMeshes(rootObject, meshRenderers, out bounds, out transform);
		}

		public static void GetOrientedBoundingBoxFromMeshes(GameObject rootObject, MeshRenderer[] meshRenderers, out Bounds bounds, out Matrix4x4 transform)
		{
			bounds = new Bounds();
			transform = Matrix4x4.TRS(rootObject.transform.position, rootObject.transform.rotation, Vector3.one);

			foreach (var renderer in meshRenderers)
			{
				bounds.Encapsulate(transform.inverse.MultiplyPoint(renderer.transform.TransformPoint(renderer.localBounds.min)));
				bounds.Encapsulate(transform.inverse.MultiplyPoint(renderer.transform.TransformPoint(renderer.localBounds.max)));
			}
		}
	}
}
