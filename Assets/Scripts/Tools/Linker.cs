using CollabXR.Objects;
using CollabXR.Objects.Linker.Sockets;
using UnityEngine;

namespace CollabXR.Tools
{
	public class Linker : MonoBehaviour
	{
		private LineRenderer line;
		public GameObject grabber;

		// Start is called once before the first execution of Update after the MonoBehaviour is created
		void Start()
		{
			line = GetComponent<LineRenderer>();
			line.useWorldSpace = true;
		}

		// Update is called once per frame
		void Update()
		{
			line.enabled = linking;
			if (linking)
			{
				if (selectedStart != null)
				{
					line.SetPosition(0, selectedStart.transform.position);
				}
				if (selectedEnd != null)
				{
					line.SetPosition(1, selectedEnd.transform.position);
				}
				else
				{
					line.SetPosition(1, transform.position);
				}
			}
		}

		private void OnEnable()
		{
			Debug.Log("Linker Tool: OnEnable!!");
			//grabber?.SetActive(false);
		}

		private void OnDisable()
		{
			Debug.Log("Linker Tool: OnDisable!!");
			//grabber?.SetActive(true);
		}

		bool linking = false;
		SocketBase hovered;
		SocketBase selectedStart;
		SocketBase selectedEnd;

		public void StartConnection()
		{
			if (hovered != null)
			{
				selectedStart = hovered;
				linking = true;
				Debug.Log(string.Format("Linker Tool: StartConnection called with {0}", hovered));
			}
		}

		public void EndConnection()
		{
			if (!linking)
			{
				return;
			}

			if (hovered != null)
			{
				selectedEnd = hovered;
			}

			if (selectedStart != null && selectedEnd != null)
			{
				// Ensure flow is always going from pipe out > pipe in
				if (selectedEnd.flow == SocketFlowDirection.Output)
				{
					var swap = selectedEnd;
					selectedEnd = selectedStart;
					selectedStart = swap;
				}

				// If either socket is connected, disconnect them
				if (selectedStart.IsConnected(selectedEnd) || selectedEnd.IsConnected(selectedStart))
				{
					selectedStart.Disconnect(selectedEnd);
				} // If both sockets can connect to each other, do so
				else if (selectedStart.CanConnect(selectedEnd) && selectedEnd.CanConnect(selectedStart))
				{
					Debug.Log(string.Format("Linker Tool: Forming connection between {0} -> {1} !", selectedEnd, selectedStart));
					selectedEnd.Connect(selectedStart);
				}
			}
			else
			{
				Debug.Log(string.Format("Linker Tool: Invalid link targets {0} -> {1} !", selectedStart, selectedEnd));
			}

			linking = false;
			selectedStart = null;
			selectedEnd = null;
		}

		public void SetTarget(GameObject g)
		{
			// If the game object exists, attempt to set our hovering value to the socket on it
			if (g != null && g.TryGetComponent<SocketBase>(out hovered))
			{
				//Debug.Log("Hovered is " + hovered.ToString());
				// no-op
			}
			else
			{
				hovered = null; // Otherwise, clear hover status
				//Debug.Log("Hovered is null");
			}
		}
	}
}
