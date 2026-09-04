using System.IO;
using CollabXR.ModExtras.Annotation;
using CollabXR.ModExtras.Measurement;
using CollabXR.Objects.Linker.Sockets;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Testing
{
	public class TestLegend
	{
		private const string pathToScene = "Assets/Tests/Linker/TestLegend.unity";

		[SetUp]
		public void Setup()
		{
			if (!File.Exists(pathToScene))
			{
				Assert.Inconclusive("The path to the Scene is not correct. Set the correct path for the pathToScene variable.");
			}
			EditorSceneManager.OpenScene(pathToScene);
		}

		[Test]
		public void LegendSockets()
		{
			// Test mod setup/initialization
			GameObject mod = GameObject.Find("Mod");

			// Annotation should be properly configured, should not have lost annotations
			SocketAnnotation annotation = mod.GetComponent<SocketAnnotation>();
			Assert.That(annotation.scriptableObject, Is.Not.Null);
			Assert.That(annotation.scriptableObject, Is.TypeOf<LegendMetadata>());

			// Perform SocketOutput setup
			SocketOutput output = mod.AddComponent<SocketOutput>();

			// Assert that socket annotation has event objects
			Assert.That(output.pushScriptableObject, Is.Not.Null);
			Assert.That(output.pushFloat, Is.Not.Null);
			Assert.That(output.pushTexture, Is.Not.Null);
			Assert.That(output.pushVolumetric, Is.Not.Null);

			// Assert that socket output was configured properly
			output.Initialize(annotation);

			// Socket output should be properly configured
			Assert.That(output.behavior, Is.EqualTo(annotation.behavior));
			Assert.True(output.UsesScriptableObjectType<LegendMetadata>());

			// Ensure legend prefab is set up correctly
			GameObject legendObject = GameObject.Find("Legend");
			SocketLegend legend = legendObject.GetComponentInChildren<SocketLegend>();

			Assert.That(legend, Is.Not.Null);
			Assert.That(legend.title, Is.Not.Null);
			Assert.That(legend.variableList, Is.Not.Null);
			Assert.That(legend.variablePrefab, Is.Not.Null);

			// Data should flow INTO the legend socket
			Assert.That(legend.flow, Is.EqualTo(SocketFlowDirection.Input));

			// Ensure connections list exists and is empty
			Assert.That(legend.connections, Is.Not.Null);
			Assert.That(legend.connections.Count, Is.Zero);

			// Ensure that SocketOutput can connect to SocketLegend
			Assert.True(output.CanConnect(legend), "can connect: SocketOutput -> SocketLegend");
			Assert.True(legend.CanConnect(output), "can connect: SocketLegend -> SocketOutput");

			// NOW, connect the SocketLegend to receive from the SocketOutput
			legend.Connect(output);

			// Our legend metadata should have been applied
			Assert.That(legend.title.text, Is.EqualTo("For Testing"));

			// We should have the right number of children (variable descriptors)
			foreach (Transform child in legend.variableList.transform)
			{
				Debug.Log(string.Format("Transform List Child: {0}", child.gameObject));
			}
			Assert.That(legend.variableList.childCount, Is.EqualTo(2), "Unexpected child count. Either the previous variables were not cleared, or new variables failed to create.");

			// Only the first should should be active (second variable is disabled)
			//Assert.True(legend.variableList.GetChild(0).gameObject.activeInHierarchy);
			Assert.False(legend.variableList.GetChild(1).gameObject.activeInHierarchy);

			// First variable should have a label for its name
			Assert.That(legend.variableList.GetChild(0).Find("Label").GetComponent<TextMeshProUGUI>().text, Is.EqualTo("enabled variable"));
		}

		[TearDown]
		public void Teardown()
		{
			EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		}
	}
}
