using Fusion;
using TMPro;
using UnityEngine;

namespace CollabXR.Objects.Linker.Network
{
	public class NetworkEventCounter : NetworkBehaviour
	{
		[Networked, OnChangedRender(nameof(UpdateLabel))]
		public int Count { get; set; }

		[SerializeField, Tooltip("Canvas UI label to write the counter to (if provided).")]
		private TextMeshProUGUI label;

		/// <summary>
		/// Increments the counter by 1.
		/// </summary>
		public void CounterIncrement()
		{
			if (!Object.HasStateAuthority)
			{
				return;
			}

			Count += 1;
		}

		/// <summary>
		/// Resets the counter to zero.
		/// </summary>
		public void CounterReset()
		{
			if (!Object.HasStateAuthority)
			{
				return;
			}

			Count = 0;
		}

		private void UpdateLabel()
		{
			if (label == null)
			{
				return;
			}

			label.text = Count.ToString();
		}
	}
}
