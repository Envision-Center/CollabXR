using CollabXR.ModExtras;
using CollabXR.Networking;
using CollabXR.Objects;
using CollabXR.UI;
using Fusion;
using UnityEngine;

namespace CollabXR.Cycles
{
	public class NetworkPlaybackDirector : ModNetworkBehaviour, IStateAuthorityChanged
	{
		[Networked]
		public PlaybackClockAnchor Anchor { get; set; }

		// Kept separate from the struct so Fusion can interpolate Anchor's float fields in Render()
		[Networked, Capacity(10)]
		public NetworkDictionary<int, int> ComponentIndices { get; }

		private PlaybackViewModel _viewModel;
		private CollabContextMenu _menu;

		protected override void CheckForScripts()
		{
			TryFindViewModel();
		}

		private void TryFindViewModel()
		{
			if (_viewModel != null)
				return;
			_viewModel = GetComponentInChildren<PlaybackViewModel>();
			if (_viewModel == null)
			{
				var playbackContext = GetComponentInChildren<PlaybackContextProvider>();
				if (playbackContext != null)
					Destroy(playbackContext);
				enabled = false;
				return;
			}

			SubscribeToViewModel();

			var director = GetComponentInChildren<PlaybackDirector>();
			if (director != null && director.PlayOnAwake && HasStateAuthority)
			{
				_viewModel.RequestPlay();
			}
		}

		private void SubscribeToViewModel()
		{
			_viewModel.OnPlayRequested += NetworkRequestPlay;
			_viewModel.OnPauseRequested += NetworkRequestPause;
			_viewModel.OnStopRequested += NetworkRequestStop;
			_viewModel.OnSeekRequested += NetworkRequestSeek;
			_viewModel.OnComponentChangeRequested += NetworkRequestSetComponentSet;
			_viewModel.OnSpeedChangeRequested += NetworkRequestSetSpeed;
		}

		private void UnsubscribeFromViewModel()
		{
			_viewModel.OnPlayRequested -= NetworkRequestPlay;
			_viewModel.OnPauseRequested -= NetworkRequestPause;
			_viewModel.OnStopRequested -= NetworkRequestStop;
			_viewModel.OnSeekRequested -= NetworkRequestSeek;
			_viewModel.OnComponentChangeRequested -= NetworkRequestSetComponentSet;
			_viewModel.OnSpeedChangeRequested -= NetworkRequestSetSpeed;
		}

		private void PushToViewModel()
		{
			if (Object == null || _viewModel == null)
				return;

			UnsubscribeFromViewModel();

			if (!Mathf.Approximately(Anchor.AnchorPercent, _viewModel.Percent.Value))
				_viewModel.RequestSeek(Anchor.AnchorPercent);

			float anchorSpeed = Anchor.Speed > 0f ? Anchor.Speed : 1f;
			if (!Mathf.Approximately(anchorSpeed, _viewModel.Speed.Value))
				_viewModel.RequestSetSpeed(anchorSpeed);

			foreach (var kvp in ComponentIndices)
			{
				if (!_viewModel.ActiveSetByComponent.Value.TryGetValue(kvp.Key, out var activeIndex) || activeIndex != kvp.Value)
					_viewModel.RequestSetComponentSet(kvp.Key, kvp.Value);
			}

			SubscribeToViewModel();
		}

		public override void Render()
		{
			base.Render();
			PushToViewModel();
		}

		#region Updating Network State

		private void UpdateAnchor(PlaybackClockAnchor newAnchor)
		{
			if (HasStateAuthority)
				Anchor = newAnchor;
			else
				RPC_UpdateAnchor(newAnchor);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_UpdateAnchor(PlaybackClockAnchor newAnchor)
		{
			Anchor = newAnchor;
		}

		private void NetworkRequestPlay()
		{
			PlaybackClockAnchor newAnchor = Anchor;
			newAnchor.IsPlaying = true;
			UpdateAnchor(newAnchor);
		}

		private void NetworkRequestPause()
		{
			PlaybackClockAnchor newAnchor = Anchor;
			newAnchor.IsPlaying = false;
			UpdateAnchor(newAnchor);
		}

		private void NetworkRequestStop()
		{
			PlaybackClockAnchor newAnchor = Anchor;
			newAnchor.IsPlaying = false;
			newAnchor.AnchorPercent = 0f;
			UpdateAnchor(newAnchor);
		}

		private void NetworkRequestSeek(float percent)
		{
			PlaybackClockAnchor newAnchor = Anchor;
			newAnchor.AnchorPercent = percent;
			UpdateAnchor(newAnchor);
		}

		private void NetworkRequestSetSpeed(float speed)
		{
			PlaybackClockAnchor newAnchor = Anchor;
			newAnchor.Speed = speed;
			UpdateAnchor(newAnchor);
		}

		private void NetworkRequestSetComponentSet(int componentId, int index)
		{
			if (HasStateAuthority)
				SetComponentIndex(componentId, index);
			else
				RPC_SetComponentIndex(componentId, index);
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void RPC_SetComponentIndex(int componentId, int index)
		{
			SetComponentIndex(componentId, index);
		}

		private void SetComponentIndex(int componentId, int index)
		{
			if (ComponentIndices.TryGet(componentId, out _))
				ComponentIndices.Set(componentId, index);
			else
				ComponentIndices.Add(componentId, index);
		}

		#endregion

		public void RegisterMenu(CollabContextMenu menu)
		{
			_menu = menu;
		}

		public void StateAuthorityChanged()
		{
			_menu?.StateAuthorityChangedAll();

			var director = GetComponentInChildren<PlaybackDirector>();
			if (director == null)
				return;

			if (HasStateAuthority)
			{
				float anchorSpeed = Anchor.Speed > 0f ? Anchor.Speed : 1f;
				director.SetSpeed(anchorSpeed);
				director.Seek(Anchor.AnchorPercent);
				if (Anchor.IsPlaying)
					director.Play();
			}
			else
			{
				director.Pause();
			}
		}
	}
}
