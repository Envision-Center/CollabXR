using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CollabXR.ModExtras;
using CollabXR.Networking;
using CollabXR.Objects;
using Fusion;
using UnityEngine;

namespace CollabXR.Networking
{
	public class NetworkToggleController : ModNetworkBehaviour
	{
		public ToggleController[] toggles;

		[Networked, OnChangedRender(nameof(UpdateVisibility)), Capacity(64)]
		public NetworkArray<ToggleVariable> toggleVisibility { get; }

		[Networked, OnChangedRender(nameof(UpdateVisibility))]
		public Vector3 nonUniformScale { get; set; } = Vector3.one;
		public event Action<ToggleVariable[]> OnVisibilityUpdate = delegate { };
		public event Action<float> OnCycleValueChange = delegate { };

		protected override void CheckForScripts()
		{
			toggles = GetComponentsInChildren<ToggleController>();
			if (Object.HasStateAuthority)
			{
				SetDefaultToggles();
			}
			UpdateVisibility();
		}

		public override void Spawned()
		{
			Debug.Log("spawned!");
			base.Spawned();
			if (Object.HasStateAuthority)
			{
				SetDefaultToggles();
			}
			UpdateVisibility();
		}

		public void UpdateVisibility()
		{
			int index = 0;
			foreach (ToggleController toggleController in toggles)
			{
				foreach (ToggleableObject toggleable in toggleController.toggleableChildren)
				{
					toggleable.Toggle(toggleVisibility[index].enabled);
					toggleable.SetPriority(toggleVisibility[index].priority);
					index++;
				}
			}
			OnVisibilityUpdate.Invoke(toggleVisibility.ToArray());
		}

		public void ToggleVariable(int index, bool enabled)
		{
			ToggleVariable toggled = toggleVisibility[index];
			toggled.enabled = enabled;
			toggleVisibility.Set(index, toggled);
		}

		public void SetTransparency(int index, float transparency)
		{
			ToggleVariable toggled = toggleVisibility[index];
			toggled.transparency = transparency;
			toggleVisibility.Set(index, toggled);
		}

		/// <summary>
		///  Set render priority of a Sorting Group.
		/// </summary>
		/// <param name="index">Variable index to modify.</param>
		/// <param name="priority">Render priority.</param>
		public void SetPriority(int index, int priority)
		{
			ToggleVariable toggled = toggleVisibility[index];
			toggled.priority = priority;
			toggleVisibility.Set(index, toggled);
		}

		public void SetDefaultToggles()
		{
			int index = 0;
			foreach (ToggleController toggleController in toggles)
			{
				foreach (ToggleableObject toggleable in toggleController.toggleableChildren)
				{
					if (toggleable.defaultEnabled)
					{
						ToggleVariable(index, true);
					}
					index++;
				}
			}
		}
	}

	/// <summary>
	/// Networked data structure for managing toggle controllers.
	/// </summary>
	[System.Serializable]
	public struct ToggleVariable : INetworkStruct
	{
		/// <summary>
		/// Whether the variable is enabled or not.
		/// </summary>
		public bool enabled;

		/// <summary>
		/// Transparency of the variable.
		/// </summary>
		public float transparency;

		/// <summary>
		/// Sorting Group priority of the toggleable.
		/// </summary>
		public int priority;
	}
}
