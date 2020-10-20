using CloudStreamForms.Core;
using Newtonsoft.Json;
using System;
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

		static async Task<string> GetApi(string url)
		{
			if (await CheckToken()) {
				return "";
			}
			return await GetRequest(url, "Bearer " + Settings.AniListToken);
		}

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
