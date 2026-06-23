using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace CollabXR.ModLoader
{
	public class DemonicGeometricSummoningCircle : MonoBehaviour
	{
		[SerializeField]
		public GameObject TestPrefabLoaderPrefab;

		[SerializeField]
		public int NumberRings = 10;

		const float PI = 3.14159265358979f;

		void Start()
		{
			float delay = 0;

			int numberChildren = 0;

			for (int i = 0; i < NumberRings; i++)
			{
				float c = 2 * PI * i;

				if (i == 0)
				{
					GameObject newTestPrefabLoader = Instantiate(TestPrefabLoaderPrefab, transform);

					newTestPrefabLoader.transform.localPosition = Vector3.zero;

					newTestPrefabLoader.GetComponent<TestPrefabLoader>().timerOffset = delay;

					delay += 0.01f;
					numberChildren++;
				}
				else
				{
					float numObjs = Mathf.Floor(c);

					for (int j = 0; j < numObjs; j++)
					{
						GameObject newTestPrefabLoader = Instantiate(TestPrefabLoaderPrefab, transform);

						newTestPrefabLoader.transform.localPosition = new Vector3(i * Mathf.Sin(((j / numObjs) * 2 * PI) - PI), 0, i * Mathf.Cos(((j / numObjs) * 2 * PI) - PI));

						newTestPrefabLoader.GetComponent<TestPrefabLoader>().timerOffset = delay;

						delay += 0.01f;
						numberChildren++;
					}
				}
			}

			Debug.Log($"We're rockin and rollin with {numberChildren} objects!!!");
		}
	}
}
