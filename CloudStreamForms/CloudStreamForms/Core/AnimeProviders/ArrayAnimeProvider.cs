using Newtonsoft.Json;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class ArrayAnimeProvider : BloatFreeBaseAnimeProvider
	{
		public ArrayAnimeProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "ArrayAnime";

#pragma warning disable CS0649
		[System.Serializable]
		struct ArrayAnimeSearchResultRoot
		{
			public ArrayAnimeSearchResult[] results;
		}

		[System.Serializable]
		struct ArrayAnimeSearchResult
		{
			public string title;
			public string id;
			//public string image;
		}

		[System.Serializable]
		struct ArrayAnimeItemResultRoot
		{
			public ArrayAnimeItemResult[] results;
		}

		[System.Serializable]
		struct ArrayAnimeItemResult
		{
			//public string title;
			//public string image;
			//public string type;
			//public string summary;
			//public string relased;
			//public string genres;
			//public string status;
			public string totalepisode;
			//public string Othername;
		}

		[System.Serializable]
		public struct ArrayAnimeEpisodeLink
		{
			public string link;
			public string name;
		}

		[System.Serializable]
		public struct ArrayAnimeEpisodeRoot
		{
			public List<ArrayAnimeEpisodeLink> links;
			//public string link; // VIDSTREAM LINK
			//public string totalepisode;
		}

		[System.Serializable]
		public struct ArrayAnimeEpisodeRoot2
		{
			public List<string> links;
			//public string link; // VIDSTREAM LINK
			//public string totalepisode;
		}
#pragma warning restore CS0649

		readonly static string[] videoRez = {
			"","240p","360p","480p","720p","1080p","1152p","2160p",
		};

		static int GetPrioFromName(string name)
		{
			for (int i = 1; i < videoRez.Length; i++) {
				if (name.ToLower().Contains($"{videoRez[i]}")) {
					return i;
				}
			}
			return 0;
		}

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			try {
				string d = DownloadString(episodeLink, tempThred, referer: "https://www.arrayanime.com/");
				if (!d.IsClean()) return;
				try {
					ArrayAnimeEpisodeRoot episodeResult = JsonConvert.DeserializeObject<ArrayAnimeEpisodeRoot>(d);
					foreach (var link in episodeResult.links) {
						int prio = GetPrioFromName(link.name);
						AddPotentialLink(normalEpisode, link.link, Name, 5 + prio, videoRez[prio]);
					}
				}
				catch (System.Exception) {
					ArrayAnimeEpisodeRoot2 episodeResult = JsonConvert.DeserializeObject<ArrayAnimeEpisodeRoot2>(d);
					foreach (var link in episodeResult.links) {
						AddPotentialLink(normalEpisode, link, Name, 5);
					}
				}
				
				//TODO ADD VIDSTREAM episodeResult.link
			}
			catch (System.Exception _ex) {
				error(_ex);
			}
		}


		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			try {
				string d = DownloadString("https://t-arrayapi.vercel.app/api/search/overlord/1", tempThred, referer: "https://www.arrayanime.com/");
				if (d.IsClean()) {
					ArrayAnimeSearchResultRoot searchResultsRoot = JsonConvert.DeserializeObject<ArrayAnimeSearchResultRoot>(d);
					if (searchResultsRoot.results != null && searchResultsRoot.results.Length > 0) {
						return searchResultsRoot.results;
					}
				}
			}
			catch (System.Exception _ex) {
				error(_ex);
			}

			return null;
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			ArrayAnimeSearchResult[] data = (ArrayAnimeSearchResult[])storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			foreach (var subData in data) {
				bool isDub = subData.title.Contains(" (Dub)");
				if ((!setData.SubExists && !isDub) || (!setData.DubExists && isDub)) {
					string realTitle = subData.title.Replace(" (Dub)", "");
					string realDownTitle = ToDown(realTitle);

					bool synoExist = false;
					if (ms.synonyms != null && ms.synonyms.Count > 0) {
						foreach (var syno in ms.synonyms) {
							if (ToDown(syno) == realDownTitle) {
								synoExist = true;
								break;
							}
						}
					}

					if (ToDown(ms.engName) == realDownTitle || synoExist) {
						try {
							string d = DownloadString("https://arrayanimeapi.vercel.app/api/details/" + subData.id, tempThread, referer: "https://www.arrayanime.com/");
							if (d.IsClean()) {
								ArrayAnimeItemResultRoot itemRoot = JsonConvert.DeserializeObject<ArrayAnimeItemResultRoot>(d);
								if (itemRoot.results != null && itemRoot.results.Length > 0) {
									int totalEpisodes = int.Parse(itemRoot.results[0].totalepisode);

									List<string> episodes = new List<string>(totalEpisodes);
									for (int i = 0; i < totalEpisodes; i++) {
										episodes.Add($"https://t-arrayapi.vercel.app/api/watching/{subData.id}/{(i + 1)}");
									}

									if (isDub) {
										setData.dubEpisodes = episodes;
									}
									else {
										setData.subEpisodes = episodes;
									}
								}
							}
						}
						catch (System.Exception _ex) {
							error(_ex);
						}
					}
				}
			}

			return setData;
		}
	}
}
