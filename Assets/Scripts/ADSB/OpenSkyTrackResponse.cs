using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CollabXR.ADSB
{
	public sealed class OpenSkyTrackResponse
	{
		public readonly List<TrackPoint> path = new();

		public struct TrackPoint
		{
			public double lat;
			public double lon;
			public double altM;
		}

		public static OpenSkyTrackResponse Parse(string json)
		{
			if (string.IsNullOrEmpty(json))
				return null;

			var root = JObject.Parse(json);
			var pathArr = root["path"] as JArray;
			if (pathArr == null)
				return null;

			var resp = new OpenSkyTrackResponse();

			foreach (var t in pathArr)
			{
				if (t is JArray a && a.Count >= 4)
				{
					double? lat = a[1]?.Value<double?>();
					double? lon = a[2]?.Value<double?>();
					double? alt = a[3]?.Value<double?>();

					if (lat.HasValue && lon.HasValue && alt.HasValue)
						resp.path.Add(
							new TrackPoint
							{
								lat = lat.Value,
								lon = lon.Value,
								altM = alt.Value,
							}
						);
				}
				else if (t is JObject o)
				{
					double? lat = o["lat"]?.Value<double?>();
					double? lon = o["lon"]?.Value<double?>();
					double? alt = o["baro_altitude"]?.Value<double?>() ?? o["altitude"]?.Value<double?>();

					if (lat.HasValue && lon.HasValue && alt.HasValue)
						resp.path.Add(
							new TrackPoint
							{
								lat = lat.Value,
								lon = lon.Value,
								altM = alt.Value,
							}
						);
				}
			}

			return resp;
		}
	}
}
