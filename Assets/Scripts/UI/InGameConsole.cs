using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace CollabXR.UI
{
	public class InGameConsole : MonoBehaviour
	{
		private const int MaxCharacters = 5000;
		const string Start = "--- start ---";
		private static string log = Start;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void OnBeforeSceneLoadRuntimeMethod()
		{
			Application.logMessageReceived += OnLog;
			onLogUpdated = delegate { };
		}

		private void OnApplicationQuit()
		{
			Application.logMessageReceived -= OnLog;
		}

		private static void OnLog(string condition, string trace, LogType type)
		{
			Color entryColor = Color.white;

			bool isError = type == LogType.Error || type == LogType.Exception;

			if (type == LogType.Warning)
			{
				entryColor = new Color(1, 1, 0.5f);
			}
			else if (isError)
			{
				entryColor = new Color(1, 0.5f, 0.5f);
			}

			string entryBody = isError ? $"\n{trace}" : "";

			log += $"\n\n<color #{ColorUtility.ToHtmlStringRGB(entryColor)}><b>{condition}</b>{entryBody}</color>";

			log = log.Substring(Math.Max(log.Length - MaxCharacters, 0), Math.Min(log.Length, MaxCharacters));

			onLogUpdated.Invoke();
		}

		private static Action onLogUpdated = delegate { };

		[SerializeField]
		private TextMeshProUGUI consoleText;

		[SerializeField]
		private ScrollRect rect;

		private void OnEnable()
		{
			UpdateText();
			onLogUpdated += UpdateText;

			StartCoroutine(ApplyScrollPosition());
		}

		public void Clear()
		{
			log = Start;
			UpdateText();
		}

		private void UpdateText()
		{
			bool atBottom = rect.verticalNormalizedPosition < 0.0001f;

			consoleText.text = log;

			if (atBottom)
			{
				Canvas.ForceUpdateCanvases();
				StartCoroutine(ApplyScrollPosition());
			}
		}

		IEnumerator ApplyScrollPosition()
		{
			yield return new WaitForEndOfFrame();
			rect.verticalNormalizedPosition = 0;
			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)rect.transform);
		}

		private void OnDisable()
		{
			onLogUpdated -= UpdateText;
		}
	}
}
