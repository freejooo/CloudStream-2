using CloudStreamForms.Core.BaseProviders;
using System;
using System.Collections.Generic;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
	class MelomovieMovieProvider : BloatFreeMovieProvider
	{
		public override string Name => "Melomovie";

		readonly MelomovieBaseProvider baseProvider;

		public MelomovieMovieProvider(CloudStreamCore _core) : base(_core) { baseProvider = new MelomovieBaseProvider(_core); }

		public override bool HasAnimeMovie => true;
		public override bool HasMovie => true;
		public override bool HasTvSeries => true;

		public override object StoreData(bool isMovie, TempThread tempThred)
		{
			var search = baseProvider.Search(ActiveMovie.title.name);
			foreach (var s in search) {
				if (s.imdb_code == ActiveMovie.title.id) {
					return s.id;
				}
			}
			return null;
		}

		readonly Dictionary<int, string[]> loadedMovies = new Dictionary<int, string[]>();
		readonly static string[] videoRez = {
			"","240p","360p","480p","720p","1080p","1152p","2160p",
		};

		public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
		{
			try {
				int id = (int)metadata;
				string[] links = null;
				if (loadedMovies.ContainsKey(id)) {
					links = loadedMovies[id];
				}
				else {
					links = baseProvider.GetLinks(id);
					if (links != null) {
						loadedMovies[id] = links;
					}
				}

				static int GetPrioFromLink(string link)
				{
					for (int i = 1; i < videoRez.Length; i++) {
						if (link.Contains($"{videoRez[i]}")) {
							return i;
						}
					}
					return 0;
				}

				if (links != null) {
					if (isMovie) {
						for (int i = 0; i < links.Length; i++) {
							int prio = GetPrioFromLink(links[i]);
							AddPotentialLink(normalEpisode, links[i], "Melomovie", 5 + prio, videoRez[prio]);
						}
					}
					else {
						string look = $".S{MultiplyString("0", 2 - season.ToString().Length)}{season}E{MultiplyString("0", 2 - episode.ToString().Length)}{episode}.";
						for (int i = 0; i < links.Length; i++) {
							if (links[i].Contains(look)) {
								int prio = GetPrioFromLink(links[i]);
								AddPotentialLink(normalEpisode, links[i], "Melomovie", 5 + prio, videoRez[prio]);
							}
						}
					}
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}
	}
}
