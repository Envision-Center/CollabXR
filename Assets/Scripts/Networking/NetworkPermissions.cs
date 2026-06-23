using System;
using System.Collections;
using System.Collections.Generic;
using CollabXR.Avatar;
using CollabXR.Oculus;
using CollabXR.VR;
using Fusion;
using Oculus.Platform.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace CollabXR.Networking
{
	public class NetworkPermissions : SingletonNetworkBehavior<NetworkPermissions>
	{
		// Students Can Interact

		[Networked, OnChangedRender(nameof(internalStudentsCanInteractChanged))]
		public NetworkBool StudentsCanInteract { get; set; } = true;

		public UnityEvent<bool> StudentsCanInteractChanged;

		private void internalStudentsCanInteractChanged()
		{
			Instance.StudentsCanInteractChanged.Invoke(StudentsCanInteract);
		}

		public void SetStudentsCanInteract(bool value)
		{
			StartCoroutine(SetStudentsCanInteractCoroutine(value));
		}

		private IEnumerator SetStudentsCanInteractCoroutine(bool value)
		{
			if (!HasStateAuthority)
			{
				Object.RequestStateAuthority();
			}

			while (!HasStateAuthority)
			{
				yield return null;
			}

			StudentsCanInteract = value;

			Object.ReleaseStateAuthority();
		}

		// Students Can Draw

		[Networked, OnChangedRender(nameof(internalStudentsCanDrawChanged))]
		public NetworkBool StudentsCanDraw { get; set; } = true;

		public UnityEvent<bool> StudentsCanDrawChanged;

		private void internalStudentsCanDrawChanged()
		{
			Instance.StudentsCanDrawChanged.Invoke(StudentsCanDraw);
		}

		public void SetStudentsCanDraw(bool value)
		{
			StartCoroutine(SetStudentsCanDrawCoroutine(value));
		}

		private IEnumerator SetStudentsCanDrawCoroutine(bool value)
		{
			if (!HasStateAuthority)
				Object.RequestStateAuthority();

			while (!HasStateAuthority)
			{
				yield return null;
			}

			StudentsCanDraw = value;

			Object.ReleaseStateAuthority();
		}

		// Students Can Place

		[Networked, OnChangedRender(nameof(internalStudentsCanPlaceChanged))]
		public NetworkBool StudentsCanPlace { get; set; } = true;

		public UnityEvent<bool> StudentsCanPlaceChanged;

		private void internalStudentsCanPlaceChanged()
		{
			Instance.StudentsCanPlaceChanged.Invoke(StudentsCanPlace);
		}

		public void SetStudentsCanPlace(bool value)
		{
			StartCoroutine(SetStudentsCanPlaceCoroutine(value));
		}

		private IEnumerator SetStudentsCanPlaceCoroutine(bool value)
		{
			if (!HasStateAuthority)
				Object.RequestStateAuthority();

			while (!HasStateAuthority)
			{
				yield return null;
			}

			StudentsCanPlace = value;

			Object.ReleaseStateAuthority();
		}

		// Students Can Delete

		[Networked, OnChangedRender(nameof(internalStudentsCanDeleteChanged))]
		public NetworkBool StudentsCanDelete { get; set; } = true;

		public UnityEvent<bool> StudentsCanDeleteChanged;

		private void internalStudentsCanDeleteChanged()
		{
			Instance.StudentsCanDeleteChanged.Invoke(StudentsCanDelete);
		}

		public void SetStudentsCanDelete(bool value)
		{
			StartCoroutine(SetStudentsCanDeleteCoroutine(value));
		}

		private IEnumerator SetStudentsCanDeleteCoroutine(bool value)
		{
			if (!HasStateAuthority)
				Object.RequestStateAuthority();

			while (!HasStateAuthority)
			{
				yield return null;
			}

			StudentsCanDelete = value;

			Object.ReleaseStateAuthority();
		}
	}
}
