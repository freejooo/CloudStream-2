#if DEBUG
using CloudStreamForms.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;

namespace CloudStreamForms.Script
{
	public static class CSDatabaseApi
	{
#pragma warning disable CS0649
		[System.Serializable]
		public struct DataHolder
		{
			public int lenght;
			public DateTime lastUpdated;
			public Title[] titles;
		}

		[System.Serializable]
		public struct Title
		{
			public string imdbId;
			public Season[] seasons;
		}

		[System.Serializable]
		public struct SubSeason
		{
			public int partNumber;
			public int malId;
			/// <summary>
			/// Id is optional, -1 if not found
			/// </summary>
			public int aniListId;
		}

		[System.Serializable]
		public struct Season
		{
			public int seasonNumber;
			public SubSeason[] subSeasons;
			/// <summary>
			/// Opening duration
			/// </summary>
			public int[] openings;
			/// <summary>
			/// Ending duration
			/// </summary>
			public int[] endings;
			public Episode[] episodes;
		}

		[System.Serializable]
		public struct Episode
		{
			/// <summary>
			/// The real episode number, starts at 1
			/// </summary>
			public int episodeNumber;
			/// <summary>
			/// The episode imdb id, not to be confused with the parent imdb
			/// </summary>
			public string imdbId;
			/// <summary>
			/// What opening index from "openings", -1 if not found
			/// </summary>
			public int op;
			/// <summary>
			/// What ending index from "endings", -1 if not found
			/// </summary>
			public int ed;
			/// <summary>
			/// Opening start time, -1 if not found
			/// </summary>
			public int opStart;
			/// <summary>
			/// Ending start time, if edStart + ed Time is less then episode length, 
			/// then it is considered skip to credits, -1 if not found
			/// </summary>
			public int edStart;
		}

		[System.Serializable]
		public struct AddEpsiodeData
		{
			public string imdbId;
			public int season;
			public Episode episode;
		}

		[System.Serializable]
		public struct AddSeasonData
		{
			public string imdbId;
			public Season season;
		}

		[System.Serializable]
		public enum DataUpdate
		{
			AddTitle = 1,
			AddSeason = 2,
			AddEpisode = 3,
		}
#pragma warning disable

		public static async void PostTitle(CloudStreamCore.Title t)
		{
			await PostRequest(JsonConvert.SerializeObject(new Title {
				imdbId = t.id,
			}), DataUpdate.AddTitle);
		}

		public static async void PostSeason(CloudStreamCore.Movie m, int season)
		{
			var t = m.title;
			SubSeason[] sub = new SubSeason[t.MALData.seasonData[season].seasons.Count];
			for (int i = 0; i < sub.Length; i++) {
				var ms = t.MALData.seasonData[season].seasons[i];
				int aniListId = ms.AniListId;
				sub[i] = new SubSeason() {
					aniListId = aniListId <= 0 ? -1 : aniListId,
					malId = ms.MalId,
					partNumber = i + 1,
				};
			}
			await PostRequest(JsonConvert.SerializeObject(new AddSeasonData {
				imdbId = t.id,
				season = new Season() {
					seasonNumber = season,
					subSeasons = sub,
				}
			}), DataUpdate.AddSeason);
		}

		public static async void PostEpisode(string headerImdbId, string epsiodeImdbId, int episode, int season, int opTime, int edTime, int op = 0, int ed = 0)
		{
			await PostRequest(JsonConvert.SerializeObject(new AddEpsiodeData {
				imdbId = headerImdbId,
				season = season,
				episode = new Episode() {
					imdbId = epsiodeImdbId,
					episodeNumber = episode,
					op = op,
					ed = ed,
					edStart = edTime,
					opStart = opTime,
				}
			}), DataUpdate.AddEpisode);
		}

		public static async Task<string> PostRequest(string postdata, DataUpdate _type)
		{ 
			try {
				string url = Settings.PublishDatabaseServerIp;
				CloudStreamCore.print("PUBLISHIP: " + url);

				int waitTime = 400;
				HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
				if (CloudStreamCore.GetRequireCert(url)) { webRequest.ServerCertificateValidationCallback = delegate { return true; }; }
				webRequest.Method = "POST";
				webRequest.UserAgent = "CLOUDSTREAM APP v" + App.GetBuildNumber();
				webRequest.Timeout = waitTime * 10;
				webRequest.ReadWriteTimeout = waitTime * 10;
				webRequest.ContinueTimeout = waitTime * 10;
				webRequest.Headers.Add("TYPE", ((int)_type).ToString());
 
				try {
					HttpWebRequest _webRequest = webRequest;
					Stream postStream = await _webRequest.GetRequestStreamAsync();

					string requestBody = postdata;// --- RequestHeaders ---

					byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

					postStream.Write(byteArray, 0, byteArray.Length);
					postStream.Close(); 
					// BEGIN RESPONSE

					try {
						HttpWebRequest request = webRequest;
						HttpWebResponse response = (HttpWebResponse)(await webRequest.GetResponseAsync());

						using StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream());
						try {
							string s = await httpWebStreamReader.ReadToEndAsync();
							CloudStreamCore.print("RESPONSEF FROM POST::: " + s);
						}
						catch (Exception) {
							return "";
						}
					}
					catch (Exception _ex) {
						CloudStreamCore.error("FATAL EX IN POST2: " + _ex);
					}
				}
				catch (Exception _ex) {
					CloudStreamCore.error("FATAL EX IN POSTREQUEST" + _ex);
				}
				return "";
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}
		}

		public static async Task<string> _PostRequest(string postdata, DataUpdate _type)
		{
			try {
				string url = Settings.PublishDatabaseServerIp;
				CloudStreamCore.print("PUBLISHIP: " + url);
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

				request.Method = "POST";
				byte[] byteArray = Encoding.UTF8.GetBytes(postdata);
				request.ContentType = "application/json";
				request.ContentLength = byteArray.Length;
				request.Headers.Add("TYPE", ((int)_type).ToString());
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
			catch (WebException e) {
				CloudStreamCore.error(e);

				try {
					using (WebResponse response = e.Response) {
						HttpWebResponse httpResponse = (HttpWebResponse)response;
						Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
						using Stream data = response.GetResponseStream();
						using var reader = new StreamReader(data);
						string text = reader.ReadToEnd();
						Console.WriteLine(text);
					}
					return "";
				}
				catch (Exception _ex) {
					CloudStreamCore.error(_ex);
					return "";
				}
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}
		}
	}
}
#endif
