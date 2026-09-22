using System;
using System.Collections.Generic;
using CollabXR.ModExtras;
using CollabXR.ModExtras.Annotation;
using CollabXR.ModExtras.Measurement;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CollabXR.Objects.Linker.Sockets
{
	[CreateAssetMenu(fileName = "SocketLegend", menuName = "CollabXR/Sockets/Legend")]
	public class SocketLegend : SocketBase
	{
		[Header("Objects")]
		public TextMeshProUGUI title;
		public Transform variableList;

		[Header("Prefabs")]
		public GameObject variablePrefab;
		public GameObject variableRangePrefab;
		public GameObject variableColorPrefab;

		private LegendMetadata legend;
		private ToggleController toggleController;

		/// <summary>
		/// A list of known connections between ToggleObjects and displayed variables.
		/// We retain this list so we can remove listeners as needed.
		///
		/// This is keyed by variable instead of ToggleObject,
		/// because one ToggleObject could correspond to multiple variables.
		/// </summary>
		private Dictionary<GameObject, ToggleableObject> toggleListeners = new Dictionary<GameObject, ToggleableObject>();

		protected override void Awake()
		{
			base.Awake();
			flow = SocketFlowDirection.Consumer;
		}

		public override bool CanConnect(SocketBase otherSocket)
		{
			if (base.CanConnect(otherSocket) && otherSocket is SocketOutput)
			{
				SocketOutput output = (SocketOutput)otherSocket;
				if (output == null)
				{
					Debug.Log("Socket Legend: other socket was not an output");
					return false;
				}

				// Ensure scriptable object is legend metadata
				//Debug.Log(string.Format("Legend CanConnect: {0}, {1}", output.behavior == SocketBehavior.ScriptableObject, output.UsesScriptableObjectType<LegendMetadata>()));
				return output.behavior == SocketBehavior.ScriptableObject && output.UsesScriptableObjectType<LegendMetadata>();
			}

			return false;
		}

		protected override void OnConnect(SocketBase otherSocket)
		{
			base.OnConnect(otherSocket);

			// We should only be allowed to get to this step if
			// the other socket is already a SocketOutput
			SocketOutput output = (SocketOutput)otherSocket;
			output.pushScriptableObject.AddListener(BuildLegend);
		}

		protected override void OnDisconnect(SocketBase otherSocket)
		{
			base.OnDisconnect(otherSocket);

			SocketOutput output = (SocketOutput)otherSocket;
			output.pushScriptableObject.RemoveListener(BuildLegend);

			ClearEventListeners();
		}

		private void ClearEventListeners()
		{
			// Remove UnityEvent connections for garbage collection
			foreach (var listener in toggleListeners)
			{
				listener.Value.toggledEvent.RemoveListener(listener.Key.SetActive);
			}
			toggleListeners.Clear();
		}

		private string StringifyNumber(LegendMetadata.Variable legendVariable, float number)
		{
			if (legendVariable.precision != null && legendVariable.precision.Length > 0)
			{
				return number.ToString(legendVariable.precision);
			}

			// TODO: automatic could be more intelligent, like using up to 4 sig-figs
			// However, that kind of sucks to program

			// For now, just use what's provided
			return $"{number}";
		}

		public void BuildLegend(ScriptableObject legendObject, Transform pointOfReference)
		{
			if (legendObject is LegendMetadata)
			{
				legend = (LegendMetadata)legendObject;
			}
			else
			{
				Debug.LogError("Socket Legend: Passed ScriptableObject was not a legend!");
				return;
			}

			title.text = legend.title;

			ClearEventListeners();

			// Remove all children under the legend
			foreach (Transform child in variableList.transform)
			{
#if UNITY_EDITOR
				DestroyImmediate(child.gameObject);
#else
				Destroy(child.gameObject);
#endif
			}

			// Check if there's a toggle controller
			if (pointOfReference != null && pointOfReference.TryGetComponent(out toggleController))
			{
				Debug.Log("Socket Legend: Found ToggleController for SocketLegend");
			}

			// Instantiate a new variable list
			foreach (var variable in legend.variables)
			{
				//Debug.Log("Creating variable " + variable.name);
				var display = Instantiate(variablePrefab, variableList, false);

				GameObject variableLabel = display.transform.GetChild(1).gameObject;
				Transform variableColors = display.transform.GetChild(0);

				// Set variable name
				variableLabel.GetComponent<TextMeshProUGUI>().text = variable.name;
				variableLabel.SetActive(variable.displayLabel);

				// Favor thresholds over ranges if they are provided
				bool useThresholds = variable.displayValues && variable.thresholds != null && variable.thresholds.Count > 0;
				// Otherwise, only use ranges
				bool useRanges = variable.displayValues && !useThresholds;

				// Show range start
				if (useRanges)
				{
					GameObject rangeLabelObj = Instantiate(variableRangePrefab, variableColors, false);

					TextMeshProUGUI rangeLabel = rangeLabelObj.GetComponent<TextMeshProUGUI>();
					rangeLabel.text = StringifyNumber(variable, variable.rangeMinimum) + variable.unit;
					rangeLabel.alignment = TextAlignmentOptions.MidlineRight;
				}

				// Construct color list
				for (int i = 0; i < variable.colors.Count; i++)
				{
					var color = variable.colors[i];
					GameObject colorObj = Instantiate(variableColorPrefab, variableColors, false);
					colorObj.GetComponent<Image>().color = color;

					if (useThresholds && variable.thresholds.Count > i)
					{
						GameObject thresholdLabelObj = Instantiate(variableRangePrefab, colorObj.transform, false);
						TextMeshProUGUI thresholdLabel = thresholdLabelObj.GetComponent<TextMeshProUGUI>();
						thresholdLabel.text = $"{StringifyNumber(variable, variable.thresholds[i])}\n{variable.unit}";
					}
				}

				// Show range end
				if (useRanges)
				{
					var rangeLabelObj = Instantiate(variableRangePrefab, variableColors, false);

					TextMeshProUGUI rangeLabel = rangeLabelObj.GetComponent<TextMeshProUGUI>();
					rangeLabel.text = StringifyNumber(variable, variable.rangeMaximum) + variable.unit;
					rangeLabel.alignment = TextAlignmentOptions.MidlineLeft;
				}

				// If we have a ToggleController, and our variable index is constrained within the number of toggleable children
				if (toggleController != null && variable.toggleIndex >= 0 && variable.toggleIndex < toggleController.toggleableChildren.Count)
				{
					ToggleableObject toggleObj = toggleController.toggleableChildren[variable.toggleIndex];

					// Hide variable as necessary
					display.SetActive(toggleObj.currentlyEnabled);

					// Bind event so variable can be toggled at runtime
					toggleObj.toggledEvent.AddListener(display.SetActive);
					toggleListeners.Add(display, toggleObj);
				}
				else
				{
					// Otherwise, always show variable
					display.SetActive(true);
				}
			}
		}
	}
}
