using CollabXR.UI;
using CollabXR.VR;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CollabXR.Desktop
{
	public class DesktopController : SingletonBehavior<DesktopController>
	{
		[SerializeField]
		private float sensitivity = 100f;

		[SerializeField]
		private float speed = 5f;

		[SerializeField]
		private float maxSpeed = 30f;
		private float rotation = 0f;
		public Camera centerEye;
		public Transform leftHandAnchor,
			rightHandAnchor;
		Vector2 movementInput,
			lookInput;
		float flyInput;
		bool lookHeld = false;

		private void Start()
		{
			enabled = HardwareConfig.type == HardwareType.Desktop;

			if (!enabled)
				return;

			Debug.Log("Desktop Controller Active");
			centerEye.transform.position += new Vector3(0, 1, 0);
			leftHandAnchor.transform.position = leftHandAnchor.transform.localPosition + new Vector3(-0.2f, 0.6f, 0);
			rightHandAnchor.transform.position = rightHandAnchor.transform.localPosition + new Vector3(0.2f, 0.6f, 0);
		}

		private void Update()
		{
			Look();
			Move();
		}

		private void Look()
		{
			if (lookHeld)
			{
				float mouseX = lookInput.x * sensitivity;
				float mouseY = lookInput.y * sensitivity;

				rotation -= mouseY;

				rotation = Mathf.Clamp(rotation, -90f, 90f);

				centerEye.transform.localRotation = Quaternion.Euler(rotation, 0f, 0f);
				transform.Rotate(Vector3.up * mouseX);
			}
		}

		private void Move()
		{
			// speed += Input.mouseScrollDelta.y * 0.8f;

			Vector3 move = transform.up * flyInput + transform.forward * movementInput.y + transform.right * movementInput.x;

			move.Normalize();

			transform.position += move * (Time.deltaTime * speed);
		}

		public void OnMove(InputValue value)
		{
			movementInput = value.Get<Vector2>();
		}

		public void OnFly(InputValue value)
		{
			flyInput = value.Get<float>();
		}

		public void OnLook(InputValue value)
		{
			lookInput = value.Get<Vector2>();
		}

		public void OnToggleMenu(InputValue value)
		{
			// todo: don't rely on findobjectoftype
			FindObjectOfType<GameMenu>(true).ToggleVisibility();
		}

		public void OnSpeed(InputValue value) { }

		public void OnLookEnable(InputValue value)
		{
			lookHeld = value.isPressed;
			Cursor.lockState = lookHeld ? CursorLockMode.Locked : CursorLockMode.None;
		}

		public void WarpTo(Transform t)
		{
			centerEye.transform.localRotation = Quaternion.identity;
			transform.rotation = Quaternion.Lerp(transform.rotation, t.rotation, 0.1f);

			Vector3 cameraOffset = centerEye.transform.position - transform.position;
			transform.position = Vector3.Lerp(transform.position, t.position - cameraOffset, 0.1f);
		}
	}
}
