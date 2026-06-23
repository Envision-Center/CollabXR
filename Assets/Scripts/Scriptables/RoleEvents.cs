using UnityEngine.Events;

namespace CollabXR.Scriptables
{
	public class RoleEvents : GenericScriptableVariableEvents<int>
	{
		public UnityEvent<bool> isStudent;
		public UnityEvent<bool> isAdmin;
		public UnityEvent<bool> isSpectator;

		protected override void Awake()
		{
			base.Awake();

			onChange.AddListener(
				delegate(int val)
				{
					isStudent.Invoke(val == 0);
					isAdmin.Invoke(val == 1);
					isSpectator.Invoke(val == 2);
				}
			);
		}
	}
}
