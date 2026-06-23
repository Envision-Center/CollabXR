using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CollabXR.ADSB
{
	public static class OpenSkyJson
	{
		public static OpenSkyStatesResponse Parse(string json)
		{
			var root = JObject.Parse(json);

			var resp = new OpenSkyStatesResponse();
			resp.time = root["time"]?.Value<long>() ?? 0;

			var statesArr = root["states"] as JArray;
			if (statesArr == null)
				return resp;

			var states = new List<OpenSkyStateVector>(statesArr.Count);

			foreach (var token in statesArr)
			{
				if (token is not JArray a)
					continue;

				// 0 icao24, 1 callsign, 5 lon, 6 lat, 7 baro_alt, 8 on_ground, 9 velocity, 10 true_track, 13 geo_altitude
				string icao24 = a.Count > 0 ? (a[0]?.Value<string>() ?? "").Trim() : "";
				if (string.IsNullOrEmpty(icao24))
					continue;

				double? lon = a.Count > 5 ? a[5]?.Value<double?>() : null;
				double? lat = a.Count > 6 ? a[6]?.Value<double?>() : null;
				if (!lat.HasValue || !lon.HasValue)
					continue;

				var sv = new OpenSkyStateVector
				{
					icao24 = icao24,
					callsign = a.Count > 1 ? (a[1]?.Value<string>() ?? "").Trim() : "",
					longitude = lon,
					latitude = lat,
					baroAltitudeM = a.Count > 7 ? a[7]?.Value<double?>() : null,
					onGround = a.Count > 8 ? a[8]?.Value<bool?>() : null,
					velocityMS = a.Count > 9 ? a[9]?.Value<double?>() : null,
					trueTrackDeg = a.Count > 10 ? a[10]?.Value<double?>() : null,
					geoAltitudeM = a.Count > 13 ? a[13]?.Value<double?>() : null,

					category = ParseCategoryToken(a.Count > 17 ? a[17] : null), // extract category
				};

				states.Add(sv);
			}

			resp.states = states;
			return resp;
		}

		private static int? ParseCategoryToken(JToken tok)
		{
			if (tok == null || tok.Type == JTokenType.Null)
				return null;

			// Normal case: integer as per OpenSky REST docs
			if (tok.Type == JTokenType.Integer)
			{
				int v = tok.Value<int>();
				return (v >= 0 && v <= 20) ? v : null;
			}

			// other: string like "A3"
			if (tok.Type == JTokenType.String)
			{
				string s = (tok.Value<string>() ?? "").Trim().ToUpperInvariant();
				if (s.Length >= 2 && s[0] == 'A' && int.TryParse(s.Substring(1), out int aIndex))
				{
					// Website A1 corresponds to API 2, A2->3, A3->4, A7->8 etc.
					int apiCategory = aIndex + 1;
					return (apiCategory >= 0 && apiCategory <= 20) ? apiCategory : null;
				}
			}

			return null;
		}
	}

	public sealed class OpenSkyStatesResponse
	{
		public long time;
		public List<OpenSkyStateVector> states = new();
	}

	public struct OpenSkyStateVector
	{
		public int? category;
		public string icao24;
		public string callsign;

		public double? latitude;
		public double? longitude;

		public double? baroAltitudeM;
		public double? geoAltitudeM;

		public bool? onGround;

		public double? velocityMS;
		public double? trueTrackDeg;
	}
}
