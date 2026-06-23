using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace CollabXR.ADSB
{
	public sealed class OpenSkyClient
	{
		private const string StatesUrl = "https://opensky-network.org/api/states/all";

		// OAuth2 token endpoint from OpenSky docs
		private const string TokenUrl = "https://auth.opensky-network.org/auth/realms/opensky-network/protocol/openid-connect/token";

		private readonly string _clientId;
		private readonly string _clientSecret;

		// cached bearer token
		private string _accessToken;
		private DateTime _tokenExpiryUtc = DateTime.MinValue;
		private bool _isTokenRequestInFlight = false;

		public OpenSkyClient(string clientId, string clientSecret)
		{
			_clientId = clientId?.Trim();
			_clientSecret = clientSecret?.Trim();
		}

		public async Task<OpenSkyTrackResponse> GetTrackAsync(string icao24, long timeUnix, CancellationToken ct)
		{
			await EnsureValidTokenAsync(ct);

			string hex = (icao24 ?? "").Trim().ToLowerInvariant();
			string url = $"https://opensky-network.org/api/tracks/all?icao24={hex}&time={timeUnix.ToString(CultureInfo.InvariantCulture)}";

			using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
			req.SetRequestHeader("Authorization", $"Bearer {_accessToken}");
			req.timeout = 15;

			var op = req.SendWebRequest();
			while (!op.isDone)
			{
				ct.ThrowIfCancellationRequested();
				await Task.Yield();
			}

			if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
			{
				if (req.responseCode == 401)
					throw new OpenSkyUnauthorizedException();
				if (req.responseCode == 429)
				{
					int retryAfterSeconds = 0;
					string retryAfter = req.GetResponseHeader("X-Rate-Limit-Retry-After-Seconds") ?? req.GetResponseHeader("Retry-After");
					if (!string.IsNullOrEmpty(retryAfter))
						int.TryParse(retryAfter, out retryAfterSeconds);
					throw new OpenSkyRateLimitException(retryAfterSeconds);
				}
				return null;
			}
			return OpenSkyTrackResponse.Parse(req.downloadHandler.text);
		}

		public async Task<OpenSkyStatesResponse> GetStatesAsync(double lamin, double lamax, double lomin, double lomax, CancellationToken ct)
		{
			// Ensure a valid token
			await EnsureValidTokenAsync(ct);

			// Try request; if token expired mid-flight, refresh once.
			try
			{
				return await GetStatesInternalAsync(lamin, lamax, lomin, lomax, ct);
			}
			catch (OpenSkyUnauthorizedException)
			{
				// Force refresh and retry once
				_accessToken = null;
				_tokenExpiryUtc = DateTime.MinValue;
				await EnsureValidTokenAsync(ct);
				return await GetStatesInternalAsync(lamin, lamax, lomin, lomax, ct);
			}
		}

		private async Task<OpenSkyStatesResponse> GetStatesInternalAsync(double lamin, double lamax, double lomin, double lomax, CancellationToken ct)
		{
			string url =
				$"{StatesUrl}?lamin={lamin.ToString(CultureInfo.InvariantCulture)}"
				+ $"&lamax={lamax.ToString(CultureInfo.InvariantCulture)}"
				+ $"&lomin={lomin.ToString(CultureInfo.InvariantCulture)}"
				+ $"&lomax={lomax.ToString(CultureInfo.InvariantCulture)}";

			using var req = UnityWebRequest.Get(url);

			if (!string.IsNullOrEmpty(_accessToken))
				req.SetRequestHeader("Authorization", $"Bearer {_accessToken}");

			req.timeout = 15;

			var op = req.SendWebRequest();
			while (!op.isDone)
			{
				ct.ThrowIfCancellationRequested();
				await Task.Yield();
			}

			if (req.result != UnityWebRequest.Result.Success)
			{
				if (req.responseCode == 401)
					throw new OpenSkyUnauthorizedException();

				if (req.responseCode == 429)
				{
					int retryAfterSeconds = 0;
					string retryAfter = req.GetResponseHeader("X-Rate-Limit-Retry-After-Seconds") ?? req.GetResponseHeader("Retry-After");
					if (!string.IsNullOrEmpty(retryAfter))
						int.TryParse(retryAfter, out retryAfterSeconds);

					throw new OpenSkyRateLimitException(retryAfterSeconds);
				}

				throw new Exception($"OpenSky request failed: {req.responseCode} {req.error}");
			}

			return OpenSkyJson.Parse(req.downloadHandler.text);
		}

		private async Task EnsureValidTokenAsync(CancellationToken ct)
		{
			// If user hasn’t provided OAuth credentials
			if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
				throw new Exception("OpenSky OAuth2 requires client_id and client_secret (new accounts do not support basic auth).");

			// Use cached token if still valid
			if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryUtc.AddMinutes(-2))
				return;

			// Prevent stampede if multiple calls happen around same time.
			if (_isTokenRequestInFlight)
			{
				// wait briefly for in-flight request to finish
				for (int i = 0; i < 50; i++)
				{
					ct.ThrowIfCancellationRequested();
					if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiryUtc.AddMinutes(-2))
						return;
					await Task.Delay(50, ct);
				}
			}

			_isTokenRequestInFlight = true;
			try
			{
				await RequestTokenAsync(ct);
			}
			finally
			{
				_isTokenRequestInFlight = false;
			}
		}

		private async Task RequestTokenAsync(CancellationToken ct)
		{
			// application/x-www-form-urlencoded:
			// grant_type=client_credentials&client_id=...&client_secret=...
			string body = "grant_type=client_credentials" + $"&client_id={UnityWebRequest.EscapeURL(_clientId)}" + $"&client_secret={UnityWebRequest.EscapeURL(_clientSecret)}";

			byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

			using var req = new UnityWebRequest(TokenUrl, "POST");
			req.uploadHandler = new UploadHandlerRaw(bodyRaw);
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
			req.timeout = 15;

			var op = req.SendWebRequest();
			while (!op.isDone)
			{
				ct.ThrowIfCancellationRequested();
				await Task.Yield();
			}

			if (req.result != UnityWebRequest.Result.Success)
				throw new Exception($"OpenSky token request failed: {req.responseCode} {req.error}");
			//Parse Json
			var json = JObject.Parse(req.downloadHandler.text);
			_accessToken = json["access_token"]?.Value<string>();

			int expiresIn = json["expires_in"]?.Value<int>() ?? (30 * 60);
			if (string.IsNullOrEmpty(_accessToken))
				throw new Exception("OpenSky token response missing access_token.");

			_tokenExpiryUtc = DateTime.UtcNow.AddSeconds(expiresIn);
		}
	}

	public sealed class OpenSkyRateLimitException : Exception
	{
		public readonly int RetryAfterSeconds;

		public OpenSkyRateLimitException(int retryAfterSeconds)
			: base($"OpenSky rate limited (429). Retry-After={retryAfterSeconds}s")
		{
			RetryAfterSeconds = retryAfterSeconds;
		}
	}

	public sealed class OpenSkyUnauthorizedException : Exception
	{
		public OpenSkyUnauthorizedException()
			: base("OpenSky unauthorized (401). Token invalid/expired.") { }
	}
}
