using System.Collections;
using CollabXR.Objects.Linker;
using CollabXR.Objects.Linker.Sockets;
using Fusion;
using TMPro;
using UnityEngine;

namespace CollabXR.Tools
{
	public class Linker : MonoBehaviour
	{
		/// <summary>
		/// Maximum amount of time to request state authority before performing the requested action.
		/// </summary>
		private const float MAX_ATTEMPT_DURATION = 2.0f;

		private LineRenderer line;

		[Header("Feedback")]
		[SerializeField]
		private GameObject uiErrorObject;

		[SerializeField]
		private CanvasGroup uiErrorGroup;

		[SerializeField]
		private TextMeshProUGUI uiErrorText;

		[SerializeField]
		private AudioSource uiErrorSound;

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
				line.SetPosition(1, transform.position);
			}
		}

		private void OnEnable()
		{
			LinkerConfig.Instance.socketViewers.Value += 1;
			uiErrorObject.SetActive(false); // Clear error value
		}

		private void OnDisable()
		{
			EndConnection(); // Cut off connection if there was one
			LinkerConfig.Instance.socketViewers.Value -= 1;
		}

		/// <summary>Whether we are actively forming a link.</summary>
		bool linking = false;

		/// <summary>Current socket we are hovering over.</summary>
		SocketBase hovered;

		/// <summary>
		/// The first socket a connection starts from.
		/// While ending a connection, this is rearranged to be the data provider.
		/// </summary>
		SocketBase selectedStart;

		/// <summary>
		/// User presses trigger and begins extending a link from the given socket.
		/// </summary>
		public void StartConnection()
		{
			if (hovered != null)
			{
				selectedStart = hovered;
				linking = true;
				//Debug.Log(string.Format("Linker Tool: StartConnection called with {0}", hovered));
			}
		}

		/// <summary>
		/// Waits up to a specified amount of time, trying to take state authority of an object.
		/// If state authority cannot be obtained by the time it ends, it exits the routine.
		/// </summary>
		/// <param name="connectionOwner"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="connect"></param>
		/// <returns></returns>
		// TODO: abstract into a class?
		// Except generic C# classes don't have coroutine access...
		private IEnumerator DeferredLinkChange(NetworkObject connectionOwner, SocketBase start, SocketBase end, bool connect)
		{
			connectionOwner.RequestStateAuthority();
			float startTime = Time.time; // When the request started

			// Wait until we either have state authority,
			// or our request times out
			while ((!connectionOwner.HasStateAuthority) && (Time.time < startTime + MAX_ATTEMPT_DURATION))
			{
				yield return null;
			}

			// Perform action immediately if we have state authority
			if (connectionOwner.HasStateAuthority)
			{
				if (connect)
				{
					Connect(start, end);
				}
				else
				{
					Disconnect(start, end);
				}
			}
			else
			{
				DisplayError("No Authority");
			}
			connectionOwner.ReleaseStateAuthority();
		}

		public void EndConnection()
		{
			if (!linking)
			{
				return;
			}
			linking = false;
			line.enabled = false;

			if (selectedStart == null)
			{
				DisplayError("Missing Socket");
			}

			SocketBase selectedEnd = hovered;
			if (selectedEnd == null)
			{
				switch (selectedStart.flow)
				{
					case SocketFlowDirection.Consumer:
						DisplayError("Missing Provider");
						return;
					case SocketFlowDirection.Provider:
						DisplayError("Missing Consumer");
						return;
				}
			}

			// Ensure flow is always going from provider > consumer
			if (selectedEnd.flow == SocketFlowDirection.Provider)
			{
				var swap = selectedEnd;
				selectedEnd = selectedStart;
				selectedStart = swap;
			}

			NetworkObject connectionOwner = selectedEnd.GetNetworkObject();

			// If either socket is connected, disconnect them
			if (selectedStart.IsConnected(selectedEnd) || selectedEnd.IsConnected(selectedStart))
			{
				StartCoroutine(DeferredLinkChange(connectionOwner, selectedStart, selectedEnd, false));
				return;
			}

			// If both sockets can connect to each other, do so
			if (selectedStart.CanConnect(selectedEnd) && selectedEnd.CanConnect(selectedStart))
			{
				StartCoroutine(DeferredLinkChange(connectionOwner, selectedStart, selectedEnd, true));
				return;
			}

			DisplayError("Incompatible");
		}

		/// <summary>
		/// Called when Linker tool collider overlaps a socket.
		/// </summary>
		/// <param name="g">GameObject of the overlapping socket</param>
		public void SetTarget(GameObject g)
		{
			// If the game object exists, attempt to set our hovering value to the socket on it
			if (g != null && g.TryGetComponent(out hovered))
			{
				// no-op
			}
			else
			{
				hovered = null; // Otherwise, clear hover status
			}
		}

		/// <summary>
		/// Displays an error and plays a notification sound, telling the user what went wrong.
		/// </summary>
		/// <param name="reason"></param>
		private void DisplayError(string reason)
		{
			uiErrorGroup.alpha = 1.0f;
			uiErrorText.text = reason;
			uiErrorObject.SetActive(true);
			uiErrorSound.Play();
			UiTweens.GenericTween(this, uiErrorGroup, 1.0f, 0.0f, 4.0f, EaseType.EaseOut, c => uiErrorGroup.alpha = c, (a, b, t) => Mathf.Lerp(a, b, t), () => uiErrorObject.SetActive(false));
		}

		/// <summary>
		/// Forms a connection between two sockets.
		/// </summary>
		/// <param name="from">Data Provider</param>
		/// <param name="to">Data Consumer</param>
		private void Connect(SocketBase from, SocketBase to)
		{
			to.Connect(from);
			uiErrorObject.SetActive(false);
			// TODO: Play sound
			// TODO: Visual particles or something
		}

		/// <summary>
		/// Removes a connection between two sockets.
		/// </summary>
		/// <param name="from">Data Provider</param>
		/// <param name="to">Data Consumer</param>
		private void Disconnect(SocketBase from, SocketBase to)
		{
			to.Disconnect(from);
			uiErrorObject.SetActive(false);
			// TODO: Play sound
			// TODO: Visual particles or something
		}
	}
}
