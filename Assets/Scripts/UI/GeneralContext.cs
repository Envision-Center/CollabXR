using System.Collections;
using System.Collections.Generic;
using CollabXR.ModExtras;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.UI;
using Cysharp.Threading.Tasks.Triggers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace CollabXR.UI
{
	public class GeneralContext : CollabContext
	{
		protected NetworkToggleController controller;
		public List<List<ToggleUI>> toggleCategories;
		public GameObject variableSliderPrefab;
		public GameObject pagePrefab;
		public Transform pageParent;
		public CategoryView view;

		protected NetworkScalable scaler;
		public Slider x,
			y,
			z;

		private List<GameObject> pageInstances = new List<GameObject>();

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			ResetContext();
			base.GiveContext(context, menu);
			controller = dataObj.GetComponentInChildren<NetworkToggleController>();
			scaler = dataObj.GetComponentInChildren<NetworkScalable>();

			if (controller != null)
			{
				controller.OnVisibilityUpdate += AdjustToggleState;
			}
			if (scaler != null)
			{
				scaler.OnNonUniformScaleChange += AdjustScaleSliders;
			}

			toggleCategories = new List<List<ToggleUI>>();
			int index = 0;
			if (controller != null)
			{
				foreach (ToggleController toggle in controller.toggles)
				{
					GameObject pageInstance = Instantiate(pagePrefab, pageParent);
					pageInstance.name = toggle.gameObject.name;
					pageInstances.Add(pageInstance);
					//pageInstance.GetComponentInChildren<TextMeshProUGUI>().text = toggle.gameObject.name;

					List<ToggleUI> currentPageList = new List<ToggleUI>();
					foreach (ToggleableObject toggleable in toggle.toggleableChildren)
					{
						int localIndex = index;
						string varName = "";

						SortingGroup sortingGroup = null; // Optional SortingGroup component

						switch (toggleable.type)
						{
							case ToggleableObjectType.GameObject:
								varName = toggleable.obj.name;
								toggleable.obj.TryGetComponent(out sortingGroup);
								break;
							case ToggleableObjectType.Component:
								varName = toggleable.component.gameObject.name;
								break;
							default:
								varName = toggleable.shaderPropertyName;
								break;
						}

						ToggleVariable toggleVar = controller.toggleVisibility[localIndex];
						GameObject toggleVariable = Instantiate(variableSliderPrefab, pageInstance.transform);
						Slider toggleSlider = toggleVariable.GetComponentInChildren<Slider>();
						ToggleUI toggleUI = toggleVariable.GetComponent<ToggleUI>();

						if (!toggleable.needsTransparencySlider && toggleSlider != null)
						{
							Destroy(toggleSlider.gameObject);
						}

						if (!toggleable.CanPrioritize())
						{
							toggleUI.priorities.Clear();
							Destroy(toggleUI.prioritiesList);
						}

						toggleUI.InitializeUI(
							varName,
							toggleVar.enabled,
							toggleVar.transparency,
							toggleVar.priority,
							(bool enabled) =>
							{
								ToggleVariable(localIndex, enabled);
							},
							(float transparency) =>
							{
								SetVariableTransparency(localIndex, transparency);
							},
							(int priority) =>
							{
								SetVariablePriority(localIndex, priority);
							}
						);
						currentPageList.Add(toggleUI);
						index++;
					}
					toggleCategories.Add(currentPageList);
				}
				view.RefreshButtons();
				controller.UpdateVisibility();
			}
			AdjustScaleSliders();
		}

		public void ResetContext()
		{
			if (controller != null)
			{
				controller.OnVisibilityUpdate -= AdjustToggleState;
			}
			if (scaler != null)
			{
				scaler.OnNonUniformScaleChange -= AdjustScaleSliders;
			}
			foreach (GameObject page in pageInstances)
			{
				DestroyImmediate(page);
			}
			pageInstances.Clear();
			view.RefreshButtons();
		}

		public void AdjustToggleState(ToggleVariable[] visibility)
		{
			Debug.Log("Adjusting context state");
			int index = 0;
			for (int i = 0; i < toggleCategories.Count; i++)
			{
				for (int j = 0; j < toggleCategories[i].Count; j++)
				{
					toggleCategories[i][j].toggle.isOn = visibility[index].enabled;
					toggleCategories[i][j].slider.value = visibility[index].transparency;
					toggleCategories[i][j].UpdatePriorityUI(visibility[index].priority);
					index++;
				}
			}
		}

		public void AdjustScaleSliders()
		{
			if (scaler != null)
			{
				x.SetValueWithoutNotify(scaler.nonUniformNetworkScale.x);
				y.SetValueWithoutNotify(scaler.nonUniformNetworkScale.y);
				z.SetValueWithoutNotify(scaler.nonUniformNetworkScale.z);
			}
		}

		public void Scale()
		{
			if (scaler != null)
			{
				scaler.SetNonUniformScale(new Vector3(x.value, y.value, z.value));
			}
		}

		public void ToggleVariable(int index, bool enabled)
		{
			controller.ToggleVariable(index, enabled);
		}

		public void SetVariableTransparency(int index, float value)
		{
			controller.SetTransparency(index, value);
		}

		public void SetVariablePriority(int index, int priority)
		{
			controller.SetPriority(index, priority);
		}

		public void SetScale(Vector3 value)
		{
			controller.nonUniformScale = value;
		}
	}
}
