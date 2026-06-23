using System.Collections;
using System.Collections.Generic;
using CollabXR.UI;
using UnityEngine;

namespace CollabXR.Objects
{
	public class CollabContext : MonoBehaviour
	{
		protected CollabObject dataObj;
		protected CollabContextMenu menuRef;

		public Sprite menuIcon;

		protected virtual void Update()
		{
			if (dataObj != null)
			{
				menuRef.SetAuthorityDetails(dataObj.Object.StateAuthority);
			}
		}

		public virtual void GiveContext(CollabObject context, CollabContextMenu menu)
		{
			this.dataObj = context;

			this.menuRef = menu;
		}

		public virtual void OnStateAuthorityChanged() { }

		public void RequestAuthority()
		{
			this.dataObj.Object.RequestStateAuthority();
		}
	}
}
