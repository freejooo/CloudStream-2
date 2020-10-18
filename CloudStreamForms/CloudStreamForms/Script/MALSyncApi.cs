using CloudStreamForms.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CloudStreamForms.Script
{
	public static class MALSyncApi
	{
		const string clientId = "4fd61fb30dd2931fb6ff39228a087103";

		static int requestId = 0;
		static string code_verifier = "";

		public static async void AuthenticateLogin(string data)
		{
			string state = CloudStreamForms.Core.CloudStreamCore.FindHTML(data + "&", "state=", "&");
			if (state == "RequestID" + requestId) {
				string currentCode = CloudStreamForms.Core.CloudStreamCore.FindHTML(data + "&", "code=", "&");
				string res = await PostRequest("https://myanimelist.net/v1/oauth2/token", $"client_id={clientId}&code={currentCode}&code_verifier={code_verifier}&grant_type=authorization_code");
				StoreToken(res);
				await GetUser();
				App.ShowToast("Login complete");
			}
		}

		static void StoreToken(string response)
		{
			try {
				if (response.IsClean()) {
					ResponseToken token = JsonConvert.DeserializeObject<ResponseToken>(response);
					Settings.MalApiToken = token.access_token;
					Settings.MalApiRefreshToken = token.refresh_token;
					Settings.MalApiTokenUnixTime = CloudStreamCore.UnixTime + token.expires_in;
				}
			}
			catch (Exception) { }
		}

		public static async Task RefreshToken()
		{
			string res = await PostRequest("https://myanimelist.net/v1/oauth2/token", $"client_id={clientId}&grant_type=refresh_token&refresh_token={Settings.MalApiRefreshToken}");
			StoreToken(res);
		}

		public struct ResponseToken
		{
			public string token_type;
			public int expires_in;
			public string access_token;
			public string refresh_token;
		}

		public static async Task<string> SetScoreRequest(int id, string status = null, int? score = null, int? num_watched_episodes = null)
		{
			string arguments = "";
			arguments += status == null ? "" : $"status={status}";
			arguments += (arguments == "" ? "" : "&") + (score == null ? "" : $"score={score}");
			arguments += (arguments == "" ? "" : "&") + (num_watched_episodes == null ? "" : $"num_watched_episodes={num_watched_episodes}");
			return await PostApi($"https://api.myanimelist.net/v2/anime/{id}/my_list_status", arguments);
		}

		public static async Task<Settings.MalUser?> GetUser(bool setSettings = true)
		{
			try {
				string data = await GetApi("https://api.myanimelist.net/v2/users/@me");
				if (data.IsClean()) {
					Settings.MalUser user = JsonConvert.DeserializeObject<Settings.MalUser>(data);
					if (setSettings) {
						Settings.CurrentMalUser = user;
					}
					return user;
				}
				else {
					return null;
				}
			}
			catch (Exception) {
				return null;
			}
		}

		public static async Task CheckToken()
		{
			if (CloudStreamCore.UnixTime > Settings.MalApiTokenUnixTime) {
				await RefreshToken();
			}
		}

		static async Task<string> PostApi(string url, string args)
		{
			await CheckToken();
			return await PostRequest(url, args, "PUT", "Bearer " + Settings.MalApiToken);
		}

		static async Task<string> GetApi(string url)
		{
			await CheckToken();
			return await GetRequest(url, "Bearer " + Settings.MalApiToken);
		}

		public static async Task<string> GetRequest(string url, string auth = null)
		{
			try {
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

				request.Method = "GET";
				if (auth.IsClean()) {
					request.Headers["Authorization"] = auth;
				}
				request.ContentType = "text/html; charset=UTF-8";
				request.UserAgent = CloudStreamCore.USERAGENT;
				request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
				request.Headers.Add("Accept-Encoding", "gzip, deflate");

				request.Headers.Add("TE", "Trailers");

				using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

				using Stream stream = response.GetResponseStream();
				using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
				string result = await reader.ReadToEndAsync();
				return result;
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}
		}

		public static async Task<string> PostRequest(string url, string postdata, string rtype = "POST", string auth = null)
		{
			try {
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

				request.Method = rtype;
				if (auth.IsClean()) {
					request.Headers["Authorization"] = auth;
				}
				byte[] byteArray = Encoding.UTF8.GetBytes(postdata);
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = byteArray.Length;
				Stream dataStream = await request.GetRequestStreamAsync();
				dataStream.Write(byteArray, 0, byteArray.Length);
				dataStream.Close();
				WebResponse response = await request.GetResponseAsync();
				dataStream = response.GetResponseStream();
				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = await reader.ReadToEndAsync();
				reader.Close();
				dataStream.Close();
				response.Close();
				return responseFromServer;
				//"{\"status\":\"on_hold\",\"score\":7,\"num_episodes_watched\":11,\"is_rewatching\":false,\"updated_at\":\"2020-10-17T23:30:29+00:00\",\"priority\":0,\"num_times_rewatched\":0,\"rewatch_value\":0,\"tags\":[],\"comments\":\"\"}"
				//"{\"status\":\"watching\",\"score\":8,\"num_episodes_watched\":11,\"is_rewatching\":false,\"updated_at\":\"2020-10-17T23:26:58+00:00\",\"priority\":0,\"num_times_rewatched\":0,\"rewatch_value\":0,\"tags\":[],\"comments\":\"\"}"
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}
		}

		public static async void Authenticate()
		{
			var rng = RandomNumberGenerator.Create();

			// It is recommended to use a URL-safe string as code_verifier.
			// See section 4 of RFC 7636 for more details.

			var bytes = new byte[96]; // base64 has 6bit per char; (8/6)*96 = 128 
			rng.GetBytes(bytes);

			code_verifier = Convert.ToBase64String(bytes)
				.TrimEnd('=')
				.Replace('+', '-')
				.Replace('/', '_');

			var code_challenge = code_verifier;
			requestId++;
			string request = $"https://myanimelist.net/v1/oauth2/authorize?response_type=code&client_id={clientId}&code_challenge={code_challenge}&state=RequestID{requestId}";
			await App.OpenBrowser(request);
		}
	}
}
