using System.Collections.Generic;
using CollabXR.Colocation;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.Oculus;
using CollabXR.Tools;
using CollabXR.Tools.Drawing;
using CollabXR.Tools.Palette;
using CollabXR.VR;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

namespace CollabXR.UI
{
	public class GameMenu : MonoBehaviour
	{
		[SerializeField]
		private SingleChildActivator pageSelector;
		private Vector3 arbitraryDeletionPosition = new Vector3(-1000, -1000, -1000);
		public List<Toggle> permissionToggles;

		private void Start()
		{
			RigHand.Left.Controller.SubscribeToOnPressEvent(CommonUsages.primaryButton, ToggleVisibility);
			RigHand.Right.Controller.SubscribeToOnPressEvent(CommonUsages.primaryButton, ToggleVisibility);
			gameObject.SetActive(false);
			SessionConfig.Instance.InvokeWhenReady(SubscribeToPermissionToggles);
		}

		private void OnDestroy()
		{
			RigHand.Left.Controller.UnsubscribeToOnPressEvent(CommonUsages.primaryButton, ToggleVisibility);
			RigHand.Right.Controller.UnsubscribeToOnPressEvent(CommonUsages.primaryButton, ToggleVisibility);
		}

		public void TurnToPage(int i)
		{
			pageSelector.SetActiveChild(i);
		}

		public void ToggleVisibility()
		{
			gameObject.SetActive(!gameObject.activeSelf);
		}

		public void ActionDisconnect()
		{
			if (NetworkManager.Instance != null)
				NetworkManager.Instance.DisconnectFromRoom();
			else
				SceneManager.LoadSceneAsync("Menu");
		}

		public void ActionResetRoom()
		{
			//StartCoroutine(DeleteAllObjects());
			CollabObject[] dataInScene = FindObjectsOfType<CollabObject>();
			foreach (CollabObject data in dataInScene)
				data.MarkForDeletion();
			BrushSubStroke[] brushStrokes = FindObjectsOfType<BrushSubStroke>();
			foreach (BrushSubStroke stroke in brushStrokes)
				stroke.MarkForDeletion();
		}

		public void ActionKickAllPlayers()
		{
			SessionManager.Instance.KickAllExcept(this);
		}

		// delayed deletion coroutine
		//IEnumerator DeleteAllObjects()
		//      {
		//          CollabObject[] dataInScene = FindObjectsOfType<CollabObject>();
		//          BrushSubStroke[] brushStrokes = FindObjectsOfType<BrushSubStroke>();
		//          foreach (CollabObject data in dataInScene)
		//	{
		//		data.MarkForDeletion();
		//		yield return new WaitForSeconds(0.05f);
		//          }
		//          foreach (BrushSubStroke stroke in brushStrokes)
		//	{
		//              stroke.MarkForDeletion(false);
		//              yield return new WaitForSeconds(0.05f);
		//          }
		//      }

		public void ActionSetPassthrough(bool b)
		{
			PassthroughManager.PassthroughOn.Value = b;
		}

		public void ActionForcePassthrough(bool b)
		{
			ColocationDriver.Instance.RPC_TogglePassthrough(b);
		}

		public void ActionMapRoom()
		{
			FindObjectOfType<OVRSceneManager>()?.RequestSceneCapture();
		}

		public void ActionLoadRoom()
		{
			FindObjectOfType<OVRSceneManager>()?.LoadSceneModel();
		}

		public void ActionUseAnchorTool(XRNode node)
		{
			ActionSetPassthrough(true);

			bool rightClicked = node == XRNode.RightHand;

			var calibratorTools = FindObjectsOfType<ColocationCalibrator>(true);

			foreach (var calibratorTool in calibratorTools)
			{
				if (calibratorTool.GetRigHandRef().Hand.Value.isRight == rightClicked)
				{
					calibratorTool.gameObject.SetActive(true);
					return;
				}
			}

			gameObject.SetActive(false);
		}

		public void SubscribeToPermissionToggles()
		{
			Debug.Log($"Subscribing to network permission changes. {NetworkPermissions.Instance != null}");
			NetworkPermissions.Instance.StudentsCanInteractChanged.AddListener(
				delegate(bool enabled)
				{
					UpdateToggle(0, enabled);
				}
			);
			NetworkPermissions.Instance.StudentsCanDrawChanged.AddListener(
				delegate(bool enabled)
				{
					UpdateToggle(1, enabled);
				}
			);
			NetworkPermissions.Instance.StudentsCanPlaceChanged.AddListener(
				delegate(bool enabled)
				{
					UpdateToggle(2, enabled);
				}
			);
			NetworkPermissions.Instance.StudentsCanDeleteChanged.AddListener(
				delegate(bool enabled)
				{
					UpdateToggle(3, enabled);
				}
			);
		}

		public void SetStudentsCanInteract(bool value)
		{
			Debug.Log("Changing permission bool...");
			NetworkPermissions.Instance.SetStudentsCanInteract(value);
		}

		public void SetStudentsCanDraw(bool value)
		{
			NetworkPermissions.Instance.SetStudentsCanDraw(value);
		}

		public void SetStudentsCanDelete(bool value)
		{
			NetworkPermissions.Instance.SetStudentsCanDelete(value);
		}

		public void SetStudentsCanPlace(bool value)
		{
			NetworkPermissions.Instance.SetStudentsCanPlace(value);
		}

		public void UpdateToggle(int index, bool value)
		{
			Debug.Log($"Updating permissions {index} = {value}");
			permissionToggles[index].isOn = value;
		}
	}
}
