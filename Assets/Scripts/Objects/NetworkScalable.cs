using System;
using CollabXR.Networking;
using CollabXR.Objects.Components;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace CollabXR.Objects
{
	public class NetworkScalable : NetworkBehaviour
	{
		[Networked, OnChangedRender(nameof(NetworkScaleChanged))]
		public Vector3 baseNetworkScale { get; set; } = Vector3.one;

		[Networked, OnChangedRender(nameof(NetworkScaleChanged))]
		public float uniformNetworkScale { get; set; } = 1.0f;

		[Networked, OnChangedRender(nameof(NetworkScaleChanged))]
		public Vector3 nonUniformNetworkScale { get; set; } = Vector3.zero; // the sliders start at zero lol

		CollabObjectData data;
		NetworkGrabbable grabbable;
		public event Action OnNonUniformScaleChange;

		public override void Spawned()
		{
			data = GetComponent<CollabObject>().GetData();
			grabbable = GetComponent<NetworkGrabbable>();
			if (Object.HasStateAuthority)
			{
				baseNetworkScale = transform.localScale;
				uniformNetworkScale = 1.0f;
				nonUniformNetworkScale = Vector3.zero;
			}
			SetLocalScale();
		}

		private void NetworkScaleChanged()
		{
			SetLocalScale();
		}

		/// <summary>
		/// Constrains a scale value within the objects minimum and maximum scaling parameters.
		/// </summary>
		/// <param name="newScale"></param>
		/// <returns>Constrained scaling value</returns>
		private float ConstrainUniformScale(float newScale)
		{
			// TODO: use a single float for min/max uniform scaling. Requires mod packaging/loading updates
			return Mathf.Clamp(newScale, Mathf.Max(data.minScale.x, data.minScale.y, data.minScale.z), Mathf.Min(data.maxScale.x, data.maxScale.y, data.maxScale.z));
		}

		/// <summary>
		/// Adds a flat value to the object's uniform scale.
		/// </summary>
		/// <param name="addAmount"></param>
		public void AddUniformScale(float addAmount)
		{
			uniformNetworkScale = ConstrainUniformScale(uniformNetworkScale + addAmount);
			SetLocalScale();
		}

		/// <summary>
		/// Adds the joystick forward value to the object's uniform scale, multiplied by time.
		/// </summary>
		/// <param name="addAmount"></param>
		public void AddUniformScaleJoystick(Vector2 joystickInput)
		{
			AddUniformScale(joystickInput.y * Time.deltaTime);
		}

		/// <summary>
		/// Directly sets the object's uniform scale to the given value.
		/// </summary>
		/// <param name="scale"></param>
		public void SetUniformScale(float scale)
		{
			uniformNetworkScale = ConstrainUniformScale(scale);
			SetLocalScale();
		}

		public void SetNonUniformScale(Vector3 sliderScale)
		{
			nonUniformNetworkScale = sliderScale;
			SetLocalScale();
			OnNonUniformScaleChange.Invoke();
		}

		public void SetLocalScale()
		{
			// changing non uniform scale to exponential since it uses a slider for input (bad)
			Vector3 effectiveNonUniformScale = new Vector3(Mathf.Pow(10.0f, nonUniformNetworkScale.x), Mathf.Pow(10.0f, nonUniformNetworkScale.y), Mathf.Pow(10.0f, nonUniformNetworkScale.z));
			transform.localScale = Vector3.Scale(baseNetworkScale * uniformNetworkScale, effectiveNonUniformScale);
		}
	}
}
