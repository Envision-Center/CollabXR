using System;
using System.Collections.Generic;
using System.Linq;
using CollabXR.Cycles;
using CollabXR.ModExtras;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.Tools.Palette;
using CollabXR.UI;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class CollabContextMenu : MonoBehaviour
	{
		public TextMeshProUGUI nameField;
		public Transform contextContainer;
		public GameObject authorityBlocker;
		public TextMeshProUGUI authLabel;
		public CollabContext generalContext;
		public Button selectorButtonPrefab;
		public Transform selectorButtonContainer;

		private Dictionary<string, CollabContext> contextTabs = new();
		private Transform tabContainer;
		private CollabContext activeTab;

		public Action OnRequestAuthority = delegate { };

		PlayerRef lastAuthority = PlayerRef.None;

		private void SafeGiveContext(CollabContext context, CollabObject obj)
		{
			try
			{
				context.GiveContext(obj, this);
			}
			catch (Exception e)
			{
				// funny fix because sometimes the context will have random npes and mess up initialization so im just ignoring it
				Debug.LogWarning($"Error giving context to {context.GetType().Name} for object {obj.name}; continuing regardless: {e.Message}");
			}
		}

		public void OpenContext(CollabObject obj)
		{
			// Clean up existing tabs and deactivate them
			foreach (var context in contextTabs.Values)
			{
				if (context != null)
					context.gameObject.SetActive(false);
				Destroy(context.gameObject);
			}
			contextTabs.Clear();
			activeTab = null;

			if (obj == null)
				return;

			nameField.text = obj.Data.formattedName;

			// Create or reuse the tab container
			if (tabContainer == null)
			{
				var containerGo = new GameObject("TabContainer");
				containerGo.transform.SetParent(contextContainer, false);
				tabContainer = containerGo.transform;
			}
			else
			{
				// Clear all children from container
				foreach (Transform child in tabContainer)
				{
					Destroy(child.gameObject);
				}
			}

			var generalGo = new GameObject("GeneralContext");
			generalGo.transform.SetParent(tabContainer, false);
			var generalContextInstance = Instantiate(generalContext, generalGo.transform).GetComponent<CollabContext>();
			generalContextInstance.transform.localScale = Vector3.one;
			generalContextInstance.transform.localPosition = Vector3.zero;
			generalContextInstance.transform.localRotation = Quaternion.identity;
			SafeGiveContext(generalContextInstance, obj);
			contextTabs["General"] = generalContextInstance;

			// Add object-specific context tab if available
			if (obj.Data?.contextPrefab != null)
			{
				var tabGo = new GameObject("ExtraContext");
				tabGo.transform.SetParent(tabContainer, false);
				var context = Instantiate(obj.Data.contextPrefab, tabGo.transform).GetComponent<CollabContext>();
				context.transform.localScale = Vector3.one;
				context.transform.localPosition = Vector3.zero;
				context.transform.localRotation = Quaternion.identity;
				SafeGiveContext(context, obj);
				contextTabs["Extra"] = context;
			}

			DiscoverAndAddRegisteredContexts(obj);

			SpawnButtons();

			// Activate the first tab by default
			if (contextTabs.Count > 0)
			{
				var firstTab = contextTabs.Values.First();
				ActivateTab(firstTab.gameObject);
			}
		}

		private void SpawnButtons()
		{
			// Clear old button listeners first
			foreach (Transform child in selectorButtonContainer)
			{
				var button = child.GetComponent<Button>();
				if (button != null)
					button.onClick.RemoveAllListeners();
				Destroy(child.gameObject);
			}

			foreach (var kvp in contextTabs)
			{
				string tabName = kvp.Key;
				CollabContext context = kvp.Value;

				var buttonGo = Instantiate(selectorButtonPrefab.gameObject, selectorButtonContainer);
				buttonGo.name = tabName;
				var button = buttonGo.GetComponent<Button>();
				var text = buttonGo.GetComponentInChildren<TextMeshProUGUI>();

				Image icon = null;
				foreach (Transform child in buttonGo.transform)
				{
					icon ??= child.GetComponent<Image>();
				}

				if (icon != null && context.menuIcon != null)
				{
					icon.sprite = context.menuIcon;
				}
				text.text = tabName;

				// Capture the context GameObject in a local variable to avoid closure over loop variable
				GameObject contextGameObject = context.gameObject;
				button.onClick.AddListener(() => ActivateTab(contextGameObject));
			}
		}

		private void ActivateTab(GameObject obj)
		{
			// Deactivate all tabs
			foreach (var kvp in contextTabs)
			{
				kvp.Value.gameObject.SetActive(false);
			}

			// Activate only the selected tab
			obj.SetActive(true);
			activeTab = obj.GetComponent<CollabContext>();
		}

		private void DiscoverAndAddRegisteredContexts(CollabObject obj)
		{
			var providers = obj.GetComponentsInChildren<IContextProvider>(true);

			foreach (var provider in providers)
			{
				if (provider == null)
				{
					continue;
				}

				AddComponentTab(obj, provider);
			}
		}

		/// <summary>
		/// Creates a tab for a specific component type.
		/// </summary>
		private void AddComponentTab(CollabObject obj, IContextProvider provider)
		{
			var tabGo = new GameObject(provider.TabName + "Context");
			tabGo.transform.SetParent(tabContainer, false);

			// Instantiate the context component
			var context = provider.AddTab(tabGo, this);
			if (context == null)
			{
				Debug.LogError($"[ContextMenu] AddTab returned null for provider {provider.GetType().Name} on {obj.name}");
				return;
			}
			SafeGiveContext(context, obj);
			contextTabs[provider.TabName] = context;
		}

		public void RequestAuthority()
		{
			if (activeTab != null)
			{
				OnRequestAuthority();
				activeTab.RequestAuthority();
			}
		}

		public void SetAuthorityDetails(PlayerRef auth)
		{
			if (auth != lastAuthority)
			{
				lastAuthority = auth;
				authorityBlocker.SetActive(lastAuthority != NetworkManager.Runner.LocalPlayer);
				CollabXR.Networking.NetworkPlayer rig = NetworkManager.GetPlayerRig(lastAuthority);
				if (rig != null)
				{
					authLabel.text = "Object Authority: " + rig.Name;
				}
				else
				{
					authLabel.text = "Object Authority Unknown";
				}
			}
		}

		public void StateAuthorityChanged()
		{
			if (activeTab != null)
			{
				activeTab.OnStateAuthorityChanged();
			}
		}

		public void StateAuthorityChangedAll()
		{
			foreach (var context in contextTabs.Values)
			{
				if (context != null)
					context.OnStateAuthorityChanged();
			}
		}
	}
}
