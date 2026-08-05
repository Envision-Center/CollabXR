using System.Collections;
using System.Collections.Generic;
using CollabXR.Networking;
using CollabXR.Tools.Drawing;
using Fusion;
using UnityEngine;

namespace CollabXR
{
    public class SessionKiller : NetworkBehaviour
    {
		public GameObject subStrokePrefab;
		public int strokeAmt = 200;
		public float baseWeight = 0.04f;
		public List<BrushSubStroke> strokeList;
		// Start is called once before the first execution of Update after the MonoBehaviour is created
		public override void Spawned()
		{
			base.Spawned();
			if(Object.HasStateAuthority)
			{
				StartCoroutine(AddStroke());
			}
		}

		public IEnumerator AddStroke()
		{
			while (strokeList.Count < strokeAmt)
			{
				NetworkObject spawnedStroke = NetworkManager.Runner.Spawn(subStrokePrefab);
				BrushSubStroke currentSubStroke = spawnedStroke.GetComponent<BrushSubStroke>();
				currentSubStroke.SetParent(Object);
				currentSubStroke.Init(Color.red, baseWeight);
				Vector3 startingPos = spawnedStroke.transform.position;
				Quaternion startingRot = Quaternion.Euler(70, 120, -30);
				for (int j = 0; j < 32; j++)
				{
					startingPos += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
					currentSubStroke.AddStrokePoint(startingPos, startingRot);
				}
				strokeList.Add(currentSubStroke);
				yield return new WaitForSeconds(0.1f);
			}
		}
    }
}
