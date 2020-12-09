using CloudStreamForms.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CloudStreamForms.Script.OAuthPostGet;

namespace CloudStreamForms.Script
{
	public static class AniListSyncApi
	{
		const string clientId = "4216";
		public static async void AuthenticateLogin(string data)
		{
			try {
				string token = CloudStreamCore.FindHTML(data + "&", "access_token=", "&");
				string expires_in = CloudStreamCore.FindHTML(data + "&", "expires_in=", "&");
				Settings.AniListTokenUnixTime = CloudStreamCore.UnixTime + int.Parse(expires_in);
				Settings.AniListToken = token;
				await GetUser();
				App.SaveData();
				App.ShowToast("Login complete");
			}
			catch (Exception) {
				App.ShowToast("Login failed");
			}
		}

		public static async Task<bool> CheckToken()
		{
			if (CloudStreamCore.UnixTime >= Settings.AniListTokenUnixTime) {
				App.RemoveFolder("AniListAccount");
				var acc = await ActionPopup.DisplayActionSheet("AniList token has expired", "Login", "Cancel");
				if (acc == "Login") {
					Authenticate();
				}
				return true;
			}
			return false;
		}

		public struct AniListAvatar
		{
			public string large;
		}

		public struct AniListViewer
		{
			public int id;
			public string name;
			public AniListAvatar avatar;
		}

		public struct AniListData
		{
			public AniListViewer Viewer;
		}

		public struct AniListRoot
		{
			public AniListData data;
		}

		[System.Serializable]
		public struct AniListUser
		{
			public int id;
			public string name;
			public string picture;
		}



		public struct LikeNode
		{
			public int id;
			//public int idMal;
		}

		public struct LikePageInfo
		{
			public int total;
			public int currentPage;
			public int lastPage;
			public int perPage;
			public bool hasNextPage;
		}

		public struct LikeAnime
		{
			public List<LikeNode> nodes;
			public LikePageInfo pageInfo;
		}

		public struct LikeFavourites
		{
			public LikeAnime anime;
		}

		public struct LikeViewer
		{
			public LikeFavourites favourites;
		}

		public struct LikeData
		{
			public LikeViewer Viewer;
		}

		public struct LikeRoot
		{
			public LikeData data;
		}

		//static readonly Dictionary<int, AniListTitleHolder> isLiked = new Dictionary<int, AniListTitleHolder>();

		public enum AniListStatusType
		{
			Watching = 0, Completed = 1, OnHold = 2, Dropped = 3, PlanToWatch = 4, ReWatching = 5, none = -1
		}

		public static readonly string[] AniListStatusString = { "CURRENT", "COMPLETED", "PAUSED", "DROPPED", "PLANNING", "REPEATING" };

		[System.Serializable]
		public struct AniListTitleHolder
		{
			public bool isFavourite;
			public int id;
			public int progress;
			public int score;
			public AniListStatusType type;
		}

		public struct GetDataMediaListEntry
		{
			public int progress;
			public string status;
			public int score;
		}

		public struct GetDataMedia
		{
			public bool isFavourite;
			public GetDataMediaListEntry mediaListEntry;
		}

		public struct GetDataData
		{
			public GetDataMedia Media;
		}

		public struct GetDataRoot
		{
			public GetDataData data;
		}

		public static async Task<AniListTitleHolder?> GetDataAboutId(int id)
		{
			const string q = @"query ($id: Int) { # Define which variables will be used in the query (id)
			Media (id: $id, type: ANIME) { # Insert our variables into the query arguments (id) (type: ANIME is hard-coded in the query)
				#id
				isFavourite
				mediaListEntry {
					progress
					status
					score (format: POINT_10)
				}
			}
			}&variables={ ""id"":""{0}"" }";

			try {
				var data = await PostApi("https://graphql.anilist.co", ($"&query={q}").Replace("{0}", id.ToString()));
				GetDataRoot d = JsonConvert.DeserializeObject<GetDataRoot>(data);
				var main = d.data.Media;
				CloudStreamCore.print(data);
				return new AniListTitleHolder() {
					id = id,
					isFavourite = main.isFavourite,
					progress = main.mediaListEntry.progress,
					score = main.mediaListEntry.score,
					type = (AniListStatusType)AniListStatusString.IndexOf(main.mediaListEntry.status),
				};
			}
			catch (Exception) {
				return null;
			}
		}

		public static async Task<bool> ToggleLike(int id)
		{
			try {
				const string q = @"mutation ($animeId: Int) {
				ToggleFavourite (animeId: $animeId) {
					anime {
						nodes {
							id
							title {
								romaji
							}
						}
					}
				}
				}&variables={
					""animeId"": {0}
				}";
				var data = await PostApi("https://graphql.anilist.co", ($"&query={q}").Replace("{0}", id.ToString()));
				return data != "";
			}
			catch (Exception) {
				return false;
			}
		}

		public static async Task<bool> PostDataAboutId(int id, AniListStatusType type, int score, int progress)
		{
			const string q = @"mutation ($id: Int, $status: MediaListStatus, $scoreRaw: Int, $progress: Int) {
			SaveMediaListEntry (mediaId: $id, status: $status, scoreRaw: $scoreRaw, progress: $progress) {
				id
				status
				progress
				score
			}
			}&variables={
				""id"": {0},
				""status"":""{1}"",
				""scoreRaw"":{2},
				""progress"":{3}
			}";
			try {
				var fullQ = ($"&query={q}")
					.Replace("{0}", id.ToString())
					.Replace("{1}", AniListStatusString[(int)type])
					.Replace("{2}", (score * 10).ToString())
					.Replace("{3}", progress.ToString());
				CloudStreamCore.print(fullQ);
				var data = await PostApi("https://graphql.anilist.co", fullQ
					);
				//"{\"data\":{\"SaveMediaListEntry\":{\"id\":144884500,\"status\":\"PLANNING\",\"progress\":8,\"score\":7}}}"
				return data != "";
			}
			catch (Exception) {
				return false;
			}
		}

		public static async Task<AniListUser?> GetUser(bool setSettings = true)
		{
			try {
				const string q = @"
				{
  					Viewer {
    					id
    					name
						avatar {
							large
						}
  					}
				}";
				var data = await PostApi("https://graphql.anilist.co", $"&query={q}");
				if (!data.IsClean()) return null;
				AniListRoot userData = JsonConvert.DeserializeObject<AniListRoot>(data);
				var u = userData.data.Viewer;
				var user = new AniListUser() {
					id = u.id,
					name = u.name,
					picture = u.avatar.large,
				};
				if (setSettings) {
					Settings.CurrentAniListUser = user;
				}
				App.SaveData();
				return user;
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return null;
			}
		}

		/*
		static async Task<string> GetApi(string url)
		{
			if (await CheckToken()) {
				return "";
			}
			return await GetRequest(url, "Bearer " + Settings.AniListToken);
		}*/

		static async Task<string> PostApi(string url, string args)
		{
			if (await CheckToken()) {
				return "";
			}
			return await PostRequest(url, args, "Post", "Bearer " + Settings.AniListToken);
		}

		public static async void Authenticate()
		{
			string request = $"https://anilist.co/api/v2/oauth/authorize?client_id={clientId}&response_type=token";
			await App.OpenBrowser(request);
		}
	}
}
