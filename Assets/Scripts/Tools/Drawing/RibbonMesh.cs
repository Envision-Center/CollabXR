using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Tools.Drawing
{
	public class RibbonMesh : MonoBehaviour
	{
		private MeshCollider meshCollider;

		private Mesh mesh;
		private List<Vector3> vertices;
		private List<Vector3> normals;
		private List<int> triangles;
		private List<Color32> colors;

		public int PointCount { get; private set; }

		private void Awake()
		{
			meshCollider = GetComponent<MeshCollider>();
			MeshFilter filter = GetComponent<MeshFilter>();

			mesh = filter.mesh;

			vertices = new List<Vector3>(32);
			normals = new List<Vector3>(32);
			triangles = new List<int>(vertices.Count * 3);
			colors = new List<Color32>(32);

			ClearRibbon();
		}

		/// <summary>
		/// Adds a single point to the internal mesh buffer.
		/// Does not apply it to the mesh directly, to avoid repeated copies.
		/// Call UpdateGeometry to finalize the mesh.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="rotation"></param>
		/// <param name="width"></param>
		/// <param name="color"></param>
		public void AddRibbonPoint(Vector3 position, Quaternion rotation, float width, Color32 color)
		{
			// Calculate vertices + normal for ribbon point
			Vector3 p1;
			Vector3 p2;
			Vector3 normal;

			CalculateVerticesAndNormalForRibbonPoint(position, rotation, width, out p1, out p2, out normal);

			// Add verts
			vertices.Add(p1);
			vertices.Add(p2);

			// Add normals
			normals.Add(normal);
			normals.Add(normal);

			// Colors
			colors.Add(color);
			colors.Add(color);

			if (vertices.Count >= 4)
			{
				// Add triangles
				// tri 1
				triangles.Add(vertices.Count - 4);
				triangles.Add(vertices.Count - 3);
				triangles.Add(vertices.Count - 2);
				// tri 2
				triangles.Add(vertices.Count - 1);
				triangles.Add(vertices.Count - 2);
				triangles.Add(vertices.Count - 3);
			}

			PointCount++;
		}

		/// <summary>
		/// Clears all mesh buffers, and clears the GPU mesh buffer as well.
		/// </summary>
		public void ClearRibbon()
		{
			vertices.Clear();
			normals.Clear();
			triangles.Clear();
			colors.Clear();

			PointCount = 0;

			UpdateGeometry();
		}

		private void CalculateVerticesAndNormalForRibbonPoint(Vector3 position, Quaternion rotation, float width, out Vector3 p1, out Vector3 p2, out Vector3 normal)
		{
			p1 = position + rotation * new Vector3(-width / 2.0f, 0.0f, 0.0f);
			p2 = position + rotation * new Vector3(width / 2.0f, 0.0f, 0.0f);
			normal = rotation * Vector3.up;
		}

		/// <summary>
		/// Copies the current mesh buffers to the GPU.
		/// </summary>
		public void UpdateGeometry()
		{
			bool validMesh = vertices.Count >= 4;

			if (validMesh)
			{
				mesh.SetVertices(vertices.ToArray());
				mesh.SetNormals(normals.ToArray());
				mesh.SetTriangles(triangles.ToArray(), 0);
				mesh.SetColors(colors.ToArray());

				meshCollider.sharedMesh = mesh;
			}
			else
			{
				mesh.Clear();
			}

			//gameObject.SetActive(validMesh);
		}

		private void OnDestroy()
		{
			Destroy(mesh);
		}
	}
}
