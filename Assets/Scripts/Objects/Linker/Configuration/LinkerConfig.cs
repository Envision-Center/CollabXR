using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.Objects.Linker
{
	public class LinkerConfig : SingletonBehavior<LinkerConfig>
	{
		/// <summary>
		/// Number of observers for linker tool connections.
		/// </summary>
		[NonSerialized]
		public EventVariable<int> socketViewers = new();

		[Header("Prefabs")]
		public GameObject prefabConnection;
		public GameObject prefabSocket;

		[Header("Colors")]
		public Color colorProvider;
		public Color colorConsumer;
		public List<Color> colorBehavior;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start() { }
	}
}
