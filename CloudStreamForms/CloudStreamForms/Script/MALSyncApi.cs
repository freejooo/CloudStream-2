using Acr.UserDialogs;
using AniListAPI.Model;
using CloudStreamForms.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms.Internals;

namespace CloudStreamForms.Script
{


	//https://myanimelist.net/apiconfig/references/api/v2
	public static class MALSyncApi
	{

		public static Dictionary<int, MalTitleHolder> allTitles = new Dictionary<int, MalTitleHolder>();

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
				App.SaveData();
				await OnLoginOrStart();
				App.ShowToast("Login complete");
			}
		}

		public static async Task OnLoginOrStart()
		{
			await FetchAllTitles();
		}

		public static async Task FetchAllTitles()
		{
			var allUserData = await GetAllMalData();
			allTitles.Clear();
			foreach (var data in allUserData) {
				allTitles[data.id] = data;
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

		public static async Task<MalStatus?> SetScoreRequestAndGetTitle(int id, MalStatusType? status = null, int? score = null, int? num_watched_episodes = null, bool update = true)
		{
			//curl 'https://api.myanimelist.net/v2/anime/30230?fields=id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,rank,popularity,num_list_users,num_scoring_users,nsfw,created_at,updated_at,media_type,status,genres,my_list_status,num_episodes,start_season,broadcast,source,average_episode_duration,rating,pictures,background,related_anime,related_manga,recommendations,studios,statistics' \

			var res = await SetScoreRequest(id, status == null ? null : statusAsString[(int)status], score, num_watched_episodes);
			if (res.IsClean()) {
				MalStatus title = JsonConvert.DeserializeObject<MalStatus>(res);
				if (allTitles.ContainsKey(id)) {
					var currentTitle = allTitles[id];
					currentTitle.status = title;
					allTitles[id] = currentTitle;
				}
				else {
					allTitles[id] = new MalTitleHolder() {
						id = id,
						name = "",
						status = title,
					};
				}

				return title;
			}
			else {
				return null;
			}
		}

		public static async Task<string> SetScoreRequest(int id, string status = null, int? score = null, int? num_watched_episodes = null)
		{
			string arguments = "";
			arguments += status == null ? "" : $"&status={status}";
			arguments += score == null ? "" : $"&score={score}";
			arguments += num_watched_episodes == null ? "" : $"&num_watched_episodes={num_watched_episodes}";

			/*
			arguments += status == null ? "" : $"status={status}";
			arguments += (arguments == "" ? "" : "&") + (score == null ? "" : $"score={score}");
			arguments += (arguments == "" ? "" : "&") + (num_watched_episodes == null ? "" : $"num_watched_episodes={num_watched_episodes}");*/
			return await PostApi($"https://api.myanimelist.net/v2/anime/{id}/my_list_status", arguments);
		}

		//{\"status\":\"on_hold\",\"score\":7,\"num_episodes_watched\":11,\"is_rewatching\":false,\"updated_at\":\"2020-10-17T23:30:29+00:00\",\"priority\":0,\"num_times_rewatched\":0,\"rewatch_value\":0,\"tags\":[],\"comments\":\"\"}"
		[System.Serializable]
		public struct MalStatus
		{
			public string status;
			public int score;
			public int num_episodes_watched;
			public bool is_rewatching;
			public DateTime updated_at;
			public MalStatusType MalStatusType { get { return (MalStatusType)statusAsString.IndexOf(status); } }
		}

		public struct MainPicture
		{
			public string medium;
			public string large;
		}

		public struct Node
		{
			public int id;
			public string title;
			public MainPicture main_picture;
		}

		public struct Datum
		{
			public Node node;
			public MalStatus list_status;
		}

		[System.Serializable]
		public struct MalTitleHolder
		{
			public MalStatus status;
			public int id;
			public string name;
		}

		public struct Paging
		{
		}

		public struct MalRoot
		{
			public List<Datum> data;
			public Paging paging;
		}


		//"{\"id\":[USERID],\"name\":\"[USERNAME]\",\"location\":\"\",\"joined_at\":\"2019-08-01T23:52:54+00:00\",\"picture\":\"https:\\/\\/api-cdn.myanimelist.net\\/images\\/userimages\\/[USERID].jpg?t=1603022400\"}"
		[System.Serializable]
		public struct MalUser
		{
			public int id;
			public string name;
			public string location;
			public string joined_at;
			public string picture;
			public DateTime JoinedAt { get { return DateTime.Parse(joined_at); } }
		}

		public static readonly string[] StatusNames = { "Watching", "Completed", "On-Hold", "Dropped", "Plan To Watch" };
		public static readonly string[] MalRatingNames = { "No Rating", "1 - Appalling", "2 - Horrible", "3 - Very Bad", "4 - Bad", "5 - Average", "6 - Fine", "7 - Good", "8 - Very Good", "9 - Great", "10 - Masterpiece" };
		static readonly string[] statusAsString = { "watching", "completed", "on-hold", "dropped", "plan_to_watch" };

		public enum MalStatusType { Watching = 0, Completed = 1, OnHold = 2, Dropped = 3, PlanToWatch = 4, none = -1 };

		public static async Task<MalTitleHolder[]?> GetAllMalData(string user = "@me")
		{
			try {
				List<MalTitleHolder> data = new List<MalTitleHolder>();
				bool done = false;
				while (!done) {
					string res = await GetApi($"https://api.myanimelist.net/v2/users/{user}/animelist?fields=list_status&limit=1000&offset=0");
					var root = JsonConvert.DeserializeObject<MalRoot>(res);
					var titles = root.data.Select(t => new MalTitleHolder() { id = t.node.id, name = t.node.title, status = t.list_status }).ToArray();

					data.AddRange(titles);
					if (titles.Length < 1000) {
						done = true;
					}
				}
				return data.ToArray();
			}
			catch (Exception _ex) {
				return null;
			}
		}

		public static async Task<MalUser?> GetUser(bool setSettings = true)
		{
			try {
				string data = await GetApi("https://api.myanimelist.net/v2/users/@me");
				if (data.IsClean()) {
					MalUser user = JsonConvert.DeserializeObject<MalUser>(data);
					if (user.picture.Contains("?t=")) {
						user.picture = user.picture[..user.picture.IndexOf("?")];
					}
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
			if (CloudStreamCore.UnixTime >= Settings.MalApiTokenUnixTime) {
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
