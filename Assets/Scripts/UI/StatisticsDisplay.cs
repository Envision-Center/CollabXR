using System.Collections.Generic;
using CollabXR.Networking;
using Fusion.Statistics;
using TMPro;
using UnityEngine;
using static Unity.Collections.Unicode;

namespace CollabXR.UI
{
	public class StatisticsDisplay : MonoBehaviour
	{
		public TextMeshProUGUI statisticsInfo;
		private FusionStatisticsManager stats;
		float lastUpdate;

		private void OnEnable()
		{
			NetworkManager.Runner.TryGetFusionStatistics(out stats);
		}

		private void Update()
		{
			if (stats != null && Time.time - lastUpdate > 0.1)
			{
				FusionStatisticsSnapshot snapshot = stats.CompleteSnapshot;
				string statsStr = "";
				statsStr += "---General Memory---\n";
				statsStr += $"Free: {snapshot.GeneralAllocMemoryFreeInBytes / 1000} KB\n";
				statsStr += $"Used: {snapshot.GeneralAllocMemoryUsedInBytes / 1000} KB\n";
				statsStr += "\n---Object Memory---\n";
				statsStr += $"Free: {snapshot.ObjectsAllocMemoryFreeInBytes / 1000} KB\n";
				statsStr += $"Used: {snapshot.ObjectsAllocMemoryUsedInBytes / 1000} KB\n";
				statsStr += "\n---Network---\n";
				statsStr += $"Ping: {Mathf.RoundToInt((float)NetworkManager.Runner.GetPlayerRtt(NetworkManager.Runner.LocalPlayer) * 1000)} ms\n";
				statsStr += $"Bandwith In: {snapshot.InBandwidth}\n";
				statsStr += $"Bandwith Out: {snapshot.OutBandwidth}\n";
				statisticsInfo.text = statsStr;
				lastUpdate = Time.time;
			}
		}
	}
}
