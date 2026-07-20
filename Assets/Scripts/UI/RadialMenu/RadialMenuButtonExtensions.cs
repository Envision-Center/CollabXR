using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR.UI
{
	public enum EaseType
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut,
	}

	public static class UiTweens
	{
		private static readonly Dictionary<object, Coroutine> _tweens = new();

		public static Func<float, float> GetEaseFunction(EaseType easeType) =>
			easeType switch
			{
				EaseType.Linear => t => t,
				EaseType.EaseIn => t => t * t,
				EaseType.EaseOut => t => 1f - (1f - t) * (1f - t),
				EaseType.EaseInOut => t => t * t * (3 - 2 * t),
				_ => t => t,
			};

		public static void GenericTween<T>(this MonoBehaviour host, T target, T initial, T final, float duration, EaseType easeType, Func<T, T, float, T> lerpFunction)
		{
			if (_tweens.ContainsKey(target))
			{
				host.StopCoroutine(_tweens[target]);
				_tweens.Remove(target);
			}
			_tweens[target] = host.StartCoroutine(GenericTween(initial, final, duration, GetEaseFunction(easeType), (t) => target = t, lerpFunction));
		}

		private static IEnumerator GenericTween<T>(T initial, T final, float duration, Func<float, float> easeFunction, Action<T> setter, Func<T, T, float, T> lerpFunction)
		{
			setter(initial);

			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = easeFunction(elapsed / duration);
				T target = lerpFunction(initial, final, t);
				setter(target);
				yield return null;
			}

			setter(final);

			yield break;
		}
	}
}
