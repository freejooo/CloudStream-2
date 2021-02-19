using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.CloudStreamCore;

#nullable enable

namespace CloudStreamForms.Core.BaseProviders
{
	class MelomovieBaseProvider : BaseProvider
	{
		public MelomovieBaseProvider(CloudStreamCore _core) : base(_core) { }

#pragma warning disable CS0649
		public struct MelomovieRoot
		{
			public int id;
			public string imdb_code;
		}
#pragma warning restore CS0649

		public MelomovieRoot[]? Search(string search)
		{
			try {
				string d = DownloadString($"https://melomovie.com/movie/search/?name={search}");
				if (!d.IsClean()) return null;
				return JsonConvert.DeserializeObject<MelomovieRoot[]>(d);
			}
			catch (Exception) {
				return null;
			}
		}

		public string[]? GetLinks(int id)
		{
			try {
				string d = DownloadString($"https://melomovie.com/movie/{id}/");
				if (!d.IsClean()) return null;
				const string lookFor = "data-lnk=\"";
				List<string> links = new List<string>();
				while (d.Contains(lookFor)) {
					string s = FindHTML(d, lookFor, "\"").Replace(" ", "%20");
					if (s.IsClean()) {
						if (!s.StartsWith("http")) {
							s = "http://" + s;
						}
						links.Add(s);
					}
					d = RemoveOne(d, lookFor);
				}
				if (links.Count > 0) {
					return links.ToArray();
				}
				else {
					return null;
				}
			}
			catch (Exception) {
				return null;
			}
		}
	}
}