using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollabXR
{
	public enum EaseType
	{
		Linear,
		EaseIn,
		EaseOut,
		EaseInOut,
		EaseInCubic,
		EaseOutCubic,
		EaseInOutCubic,
	}

	public static class UiTweens
	{
		private static readonly Dictionary<object, (Coroutine, Action)> _tweens = new();

		public static Func<float, float> GetEaseFunction(EaseType easeType) =>
			easeType switch
			{
				EaseType.Linear => t => t,
				EaseType.EaseIn => t => t * t,
				EaseType.EaseOut => t => 1f - (1f - t) * (1f - t),
				EaseType.EaseInOut => t => t * t * (3 - 2 * t),
				EaseType.EaseInCubic => t => t * t * t,
				EaseType.EaseOutCubic => t => 1f - (1f - t) * (1f - t) * (1f - t),
				EaseType.EaseInOutCubic => t => t * t * t * (3 - 2 * t),
				_ => t => t,
			};

		public static void GenericTween<T>(
			this MonoBehaviour host,
			object key,
			T initial,
			T final,
			float duration,
			AnimationCurve curve,
			Action<T> setter,
			Func<T, T, float, T> lerpFunction,
			Action onComplete = null
		)
		{
			if (_tweens.ContainsKey(key))
			{
				host.StopCoroutine(_tweens[key].Item1);
				_tweens[key].Item2?.Invoke();
				_tweens.Remove(key);
			}

			_tweens[key] = (host.StartCoroutine(GenericTween(initial, final, duration, curve, setter, lerpFunction, onComplete)), onComplete);
		}

		private static IEnumerator GenericTween<T>(T initial, T final, float duration, AnimationCurve easeCurve, Action<T> setter, Func<T, T, float, T> lerpFunction, Action onComplete)
		{
			setter(initial);

			float elapsed = 0f;
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = easeCurve.Evaluate(elapsed / duration);
				T target = lerpFunction(initial, final, t);
				setter(target);
				yield return null;
			}

			setter(final);

			onComplete?.Invoke();

			yield break;
		}

		public static void GenericTween<T>(
			this MonoBehaviour host,
			object key,
			T initial,
			T final,
			float duration,
			EaseType easeType,
			Action<T> setter,
			Func<T, T, float, T> lerpFunction,
			Action onComplete = null
		)
		{
			if (_tweens.ContainsKey(key))
			{
				host.StopCoroutine(_tweens[key].Item1);
				_tweens[key].Item2?.Invoke();
				_tweens.Remove(key);
			}
			_tweens[key] = (host.StartCoroutine(GenericTween(initial, final, duration, GetEaseFunction(easeType), setter, lerpFunction, onComplete)), onComplete);
		}

		private static IEnumerator GenericTween<T>(T initial, T final, float duration, Func<float, float> easeFunction, Action<T> setter, Func<T, T, float, T> lerpFunction, Action onComplete)
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

			onComplete?.Invoke();

			yield break;
		}
	}
}
