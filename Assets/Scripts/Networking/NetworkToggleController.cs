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
	public class NetworkToggleController : ModNetworkBehaviour, IStateAuthorityChanged
	{
		public ToggleController[] toggles;

		[Networked, OnChangedRender(nameof(UpdateVisibility)), Capacity(64)]
		public NetworkArray<ToggleVariable> toggleVisibility { get; }

		[Networked, OnChangedRender(nameof(UpdateVisibility))]
		public Vector3 nonUniformScale { get; set; } = Vector3.one;
		public event Action<ToggleVariable[]> OnVisibilityUpdate = delegate { };
		public event Action<float> OnCycleValueChange = delegate { };

		/// <summary>
		/// Only set to true once the object has been completely initialized and default toggles are set.
		/// This can happen a number of ways:
		///
		/// 1. The client that spawned the object maintains state authority throughout the loading process (normal behavior).
		///
		/// 2. The client that spawned the object loses state authority midway through the loading process (ie. they leave the room).
		///    When this happens, StateAuthorityChanged() will be called and all objects will attempt to claim state authority.
		///    The first client to claim state authority will set the default toggles and initialize the object.
		/// </summary>
		[Networked]
		public bool initialized { get; set; } = false;

		protected override void CheckForScripts()
		{
			toggles = GetComponentsInChildren<ToggleController>();
			Debug.Log($"SCRIPT CHECK STATE AUTH: {Object.StateAuthority}");
			if (Object.HasStateAuthority && !initialized)
			{
				// will only run if object recieves state authority before fully loading in
				SetDefaultToggles();
				initialized = true;
			}
			UpdateVisibility();
		}

		public override void Spawned()
		{
			Debug.Log("spawned!");
			base.Spawned();
			Debug.Log($"SPAWN STATE AUTH: {Object.StateAuthority}");
			if (Object.HasStateAuthority && !initialized)
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
			toggled.priority = (ushort)priority;
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

		public void StateAuthorityChanged()
		{
			Debug.Log($"ON CHANGE STATE AUTH: {Object.StateAuthority}");
			if (Object.HasStateAuthority && !initialized)
			{
				// will only run if object not yet initialized and recieves state authority after fully loading in
				SetDefaultToggles();
			}
			else if (Object.StateAuthority == PlayerRef.None)
			{
				Object.RequestStateAuthority();
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
		public ushort priority;
	}
}
