using System;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

namespace CollabXR.Oculus
{
	public class PlatformSetup : SingletonBehavior<PlatformSetup>
	{
		public static User OculusUser { get; private set; }

		private void Start()
		{
			try
			{
				InitOculusID();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private static void InitOculusID()
		{
			Debug.Log("Initializing Oculus platform...");
			Core.AsyncInitialize()
				.OnComplete(platformInitMessage =>
				{
					if (platformInitMessage.IsError)
					{
						return;
					}

					Debug.Log("Checking Oculus entitlement...");
					Entitlements
						.IsUserEntitledToApplication()
						.OnComplete(entitlementMessage =>
						{
							if (entitlementMessage.IsError)
							{
								Debug.LogError("Entitlement check FAILED: " + entitlementMessage.GetString());
							}
							else
							{
								Debug.Log("Entitlement check PASSED!");
							}
						});

					Debug.Log("Getting Oculus ID");
					Users
						.GetLoggedInUser()
						.OnComplete(getUserMessage =>
						{
							if (getUserMessage.IsError)
							{
								Debug.Log("Getting Oculus ID FAILED: " + getUserMessage);
								return;
							}

							// client send host user id
							var user = getUserMessage.GetUser();

							if (user == null || user.ID == 0)
							{
								Debug.Log("Getting Oculus ID FAILED: " + getUserMessage);
								return;
							}

							OculusUser = user;
							Debug.Log("Got Oculus ID: " + user.ID);
						});
				});
		}
	}
}
