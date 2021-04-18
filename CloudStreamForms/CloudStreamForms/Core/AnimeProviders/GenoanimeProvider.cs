using HtmlAgilityPack.CssSelectors.NetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
	class GenoanimeProvider : BloatFreeBaseAnimeProvider
	{
		public GenoanimeProvider(CloudStreamCore _core) : base(_core) { }

		public override string Name => "Genoanime";

		public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData, bool isDub)
		{
			string d = DownloadString(episodeLink);
			string src = FindHTML(d, "allow=\"fullscreen\" src=\"", "\"");

			d = DownloadString(src);
			const string lookFor = "source src=\"";

			while (d.Contains(lookFor)) {
				string link = FindHTML(d, lookFor, "\"");
				d = RemoveOne(d, lookFor);
				string size = FindHTML(d, "size=\"", "\"");
				string type = FindHTML(d, "type=\"", "\"");

				if (size.EndsWith("0")) {
					size += "p";
				}

				if (type == "video/mp4") {
					AddPotentialLink(normalEpisode, link, Name, 7, size);
				}
			}

			var f1 = FindHTML(d, "src = \'", "\'");
			if(f1.IsClean()) {
				AddPotentialLink(normalEpisode, f1, Name, 7);
			}

			if(d.Contains("genovids.php")) {
				var id = FindHTML(d, "data: {id: \'", "\'");
				if(id.IsClean()) {
					var s = core.PostRequest("https://genoanime.com/player/genovids.php", src, $"id={id}").Replace("\\","");
					if(s.IsClean()) {
						var link = FindHTML(s, "src\":\"", "\"");
						if (link.IsClean()) {
							AddPotentialLink(normalEpisode, link, Name, 7);
						}
					}
				}
			}
		}

		[System.Serializable]
		struct GenoanimeSearchItem
		{
			public string link;
			public string name;
		}

		public override object StoreData(string year, TempThread tempThred, MALData malData)
		{
			try {
				string d = core.PostRequest("https://genoanime.com/data/searchdata.php", "https://genoanime.com/search", $"anime={malData.engName}");
				if (!d.IsClean()) return null;

				var doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(d);

				var mtxt = doc.QuerySelectorAll("div.product__item__text > h5");

				GenoanimeSearchItem[] items = new GenoanimeSearchItem[mtxt.Count];
				for (int i = 0; i < mtxt.Count; i++) {
					var stxt = mtxt[i];
					var txt = stxt.QuerySelectorAll("> a")[0];
					items[i] = new GenoanimeSearchItem() {
						name = txt.InnerText,
						link = txt.GetAttributeValue("href", ""),
					};
				}

				return items.Length > 0 ? items : null;
			}
			catch (Exception _ex) {
				error(_ex);
				return null;
			}
		}

		public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
		{
			GenoanimeSearchItem[] data = (GenoanimeSearchItem[])storedData;
			NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
			string cName = ToDown(ms.engName);
			string[] cSyno = ms.synonyms.Select(t => ToDown(t)).ToArray();
			foreach (var subData in data) {
				bool isDub = subData.name.Contains(" (Dub)");
				string name = subData.name.Replace(" (Dub)", "");
				string dName = ToDown(name);
				if (dName == cName || cSyno.Contains(dName)) {
					if ((!setData.DubExists && isDub) || (!setData.SubExists && !isDub)) {
						string d = DownloadString("https://genoanime.com" + subData.link[1..]);
						var doc = new HtmlAgilityPack.HtmlDocument();
						doc.LoadHtml(d);

						var eps = doc.QuerySelectorAll("a.episode");
						List<string> episodes = new List<string>();

						foreach (var ep in eps) {
							episodes.Add("https://genoanime.com" + ep.GetAttributeValue("href", "")[1..]);
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
			return setData;
		}
	}
}