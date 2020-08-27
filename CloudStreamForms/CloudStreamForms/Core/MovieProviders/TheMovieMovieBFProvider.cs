using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
    class TheMovieMovieBFProvider : BloatFreeMovieProvider
    {
        public override string Name => "TheMovie";
        public TheMovieMovieBFProvider(CloudStreamCore _core) : base(_core) { }

        public override object StoreData(bool isMovie, TempThread tempThred)
        {
            try {
                var list = TheMovieHelper.SearchQuary(activeMovie.title.name, core);
                if (!GetThredActive(tempThred)) { return null; }; // COPY UPDATE PROGRESS
                MovieType mType = activeMovie.title.movieType;
                string compare = ToDown(activeMovie.title.name, true, "");
                var watchMovieSeasonsData = new Dictionary<int, string>();

                if (mType.IsMovie()) {
                    string mustContain = mType == MovieType.AnimeMovie ? "/anime-info/" : "/series/";
                    TheMovieHelper.TheMovieTitle[] matching = list.Where(t => ToDown(t.name, true, "") == compare && t.season == -1 && t.href.Contains(mustContain)).ToArray();
                    if (matching.Length > 0) {
                        TheMovieHelper.TheMovieTitle title = matching[0];
                        print("LOADED:::::::::-->>>1 " + title.href);

                        string d = DownloadString(title.href);
                        int maxEp = TheMovieHelper.GetMaxEp(d, title.href);
                        if (maxEp == 0 || maxEp == 1) {
                            string rEp = title.href + "-episode-" + maxEp;
                            watchMovieSeasonsData[-1] = rEp;
                            print("LOADED:::::::::-->>>2 " + rEp);
                        }
                    }
                }
                else { 
                    var episodes = list.Where(t => !t.isDub && t.season != -1 && ToDown(t.name, true, "") == compare && t.href.Contains("/series/")).ToList().OrderBy(t => t.season).ToArray();

                    for (int i = 0; i < episodes.Length; i++) {
                        watchMovieSeasonsData[episodes[i].season] = episodes[i].href;
                    }
                }
                return watchMovieSeasonsData;
            }
            catch { return null; }
        }

        public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
        {
            try {
                var watchMovieSeasonsData = (Dictionary<int, string>)metadata;
                void GetFromUrl(string url)
                {
                    string d = DownloadString(url,tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    AddEpisodesFromMirrors(tempThred, d, normalEpisode, "Watch", "");
                    LookForFembedInString(tempThred, normalEpisode, d);
                }

                if (isMovie) {
                    if (watchMovieSeasonsData.ContainsKey(-1)) {
                        GetFromUrl(watchMovieSeasonsData[-1].Replace("/anime-info/", "/anime/"));
                    }
                }
                else {
                    if (watchMovieSeasonsData.ContainsKey(season)) {
                        GetFromUrl(watchMovieSeasonsData[season].Replace("/anime-info/", "/anime/") + "-episode-" + episode);
                    }
                }
            }
            catch (Exception _ex) {
                print("PROVIDER ERROR: " + _ex);
            }
        }
    }
}
