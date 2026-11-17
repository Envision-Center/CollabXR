using System;
using CollabXR.Objects;
using CollabXR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CollabXR
{
	/// <summary>
	/// Context menu tab for changing a physics object's gravity, drag, bounce, friction, freeze and throw strength at runtime.
	/// </summary>
	public class PhysicsBodyContext : CollabContext
	{
		[SerializeField]
		private Toggle useGravity;

		[SerializeField]
		private Slider gravityScale;

		[SerializeField]
		private Slider drag;

		[SerializeField]
		private Slider bounciness;

		[SerializeField]
		private Slider friction;

		[SerializeField]
		private Slider throwStrength;

		[SerializeField]
		private Toggle freeze;

		[SerializeField]
		private Button resetButton;

		private NetworkRigidbodyAdapter body;
		private TMP_Text gravityScaleLabel;
		private TMP_Text dragLabel;
		private TMP_Text bouncinessLabel;
		private TMP_Text frictionLabel;
		private TMP_Text throwStrengthLabel;

		private string gravityScaleName;
		private string dragName;
		private string bouncinessName;
		private string frictionName;
		private string throwStrengthName;

		public override void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			base.GiveContext(context, menu);

			body = dataObj.GetComponent<NetworkRigidbodyAdapter>();

			if (body == null)
			{
				Debug.LogWarning("[PhysicsBodyContext] No NetworkRigidbodyAdapter found!");
				return;
			}

			SetUpRow(gravityScale, body.SetGravityScale, () => body.AuthoredProperties.GravityScale, out gravityScaleLabel, out gravityScaleName);
			SetUpRow(drag, body.SetDrag, () => body.AuthoredProperties.LinearDrag, out dragLabel, out dragName);
			SetUpRow(bounciness, body.SetBounciness, () => body.AuthoredProperties.Bounciness, out bouncinessLabel, out bouncinessName);
			SetUpRow(friction, body.SetFriction, () => body.AuthoredProperties.Friction, out frictionLabel, out frictionName);
			SetUpRow(throwStrength, body.SetThrowStrength, () => body.AuthoredProperties.ThrowStrength, out throwStrengthLabel, out throwStrengthName);

			useGravity.onValueChanged.AddListener(body.SetUseGravity);
			freeze.onValueChanged.AddListener(body.SetFrozen);
			resetButton.onClick.AddListener(body.ResetProperties);

			body.OnPropertiesChanged += UpdateDisplay;
			UpdateDisplay();
		}

		public override void OnStateAuthorityChanged()
		{
			UpdateDisplay();
		}

		private void OnDestroy()
		{
			if (body != null)
				body.OnPropertiesChanged -= UpdateDisplay;
		}

		private void SetUpRow(Slider slider, UnityAction<float> setValue, Func<float> authoredValue, out TMP_Text label, out string rowName)
		{
			Transform row = slider.transform.parent;
			label = row.GetComponentInChildren<TMP_Text>(true);
			rowName = label.text;

			slider.onValueChanged.AddListener(setValue);
			row.GetComponentInChildren<Button>(true).onClick.AddListener(() => setValue(authoredValue()));
		}

		private void UpdateDisplay()
		{
			if (body == null)
				return;

			PhysicsBodyProperties properties = body.CurrentProperties;

			useGravity.SetIsOnWithoutNotify(properties.UseGravity);
			freeze.SetIsOnWithoutNotify(properties.Frozen);

			gravityScale.SetValueWithoutNotify(properties.GravityScale);
			drag.SetValueWithoutNotify(properties.LinearDrag);
			bounciness.SetValueWithoutNotify(properties.Bounciness);
			friction.SetValueWithoutNotify(properties.Friction);
			throwStrength.SetValueWithoutNotify(properties.ThrowStrength);

			gravityScaleLabel.text = $"{gravityScaleName}  {properties.GravityScale:F1}x";
			dragLabel.text = $"{dragName}  {properties.LinearDrag:F1}";
			bouncinessLabel.text = $"{bouncinessName}  {properties.Bounciness:F2}";
			frictionLabel.text = $"{frictionName}  {properties.Friction:F2}";
			throwStrengthLabel.text = $"{throwStrengthName}  {properties.ThrowStrength:F1}x";
		}
	}
}
