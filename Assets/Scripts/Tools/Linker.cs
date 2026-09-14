using System.Collections;
using CollabXR.Networking;
using CollabXR.Objects;
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
		public GameObject grabber;

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
			LinkerConfig.Instance.socketViewers.Value += 1;
			uiErrorObject.SetActive(false);
		}

		private void OnDisable()
		{
			Debug.Log("Linker Tool: OnDisable!!");
			//grabber?.SetActive(true);
			LinkerConfig.Instance.socketViewers.Value -= 1;
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

		// TODO: abstract into a class? Except generic C# classes don't have coroutine access...
		private IEnumerator DeferredLinkChange(NetworkObject connectionOwner, SocketBase start, SocketBase end, bool connect)
		{
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
					end.Connect(start);
				}
				else
				{
					end.Disconnect(start);
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
					NetworkObject connectionOwner = selectedEnd.GetNetworkObject();
					connectionOwner.RequestStateAuthority();

					//Debug.Log(string.Format("Linker Tool: DISCONNECTING between {0} -> {1} !", selectedEnd, selectedStart));
					if (connectionOwner.HasStateAuthority)
					{
						selectedEnd.Disconnect(selectedStart);
						connectionOwner.ReleaseStateAuthority();
					}
					else
					{
						StartCoroutine(DeferredLinkChange(connectionOwner, selectedStart, selectedEnd, false));
					}
				} // If both sockets can connect to each other, do so
				else if (selectedStart.CanConnect(selectedEnd) && selectedEnd.CanConnect(selectedStart))
				{
					NetworkObject connectionOwner = selectedEnd.GetNetworkObject();
					connectionOwner.RequestStateAuthority();

					//Debug.Log(string.Format("Linker Tool: Forming connection between {0} -> {1} !", selectedEnd, selectedStart));
					if (connectionOwner.HasStateAuthority)
					{
						selectedEnd.Connect(selectedStart);
						connectionOwner.ReleaseStateAuthority();
					}
					else
					{
						StartCoroutine(DeferredLinkChange(connectionOwner, selectedStart, selectedEnd, true));
					}
				}
				else
				{
					DisplayError("Incompatible Sockets");
				}
			}
			else
			{
				Debug.Log(string.Format("Linker Tool: Invalid link targets {0} -> {1} !", selectedStart, selectedEnd));
				DisplayError("Needs 2 Sockets");
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

		public void DisplayError(string reason)
		{
			uiErrorGroup.alpha = 1.0f;
			uiErrorText.text = reason;
			uiErrorObject.SetActive(true);
			uiErrorSound.Play();
			UiTweens.GenericTween(this, uiErrorGroup.alpha, 1.0f, 0.0f, 4.0f, EaseType.EaseOut, c => uiErrorGroup.alpha = c, (a, b, t) => Mathf.Lerp(a, b, t), () => uiErrorObject.SetActive(false));
		}
	}
}
