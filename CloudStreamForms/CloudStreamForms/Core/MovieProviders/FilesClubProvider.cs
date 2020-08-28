using System;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.MovieProviders
{
    class FilesClubProvider : BloatFreeMovieProvider
    {
        public override string Name => "123FilesClub";
        public override bool NullMetadata => true;
        public FilesClubProvider(CloudStreamCore _core) : base(_core) { }


        public override void LoadLink(object metadata, int episode, int season, int normalEpisode, bool isMovie, TempThread tempThred)
        {
            string imdbId = activeMovie.title.id;
            string random = rng.Next(0, 10000).ToString();
            string random2 = rng.Next(0, 10000).ToString();
            string bound = $"----WebKitFormBoundary{random}";
            string data = $"--{bound}\nContent-Disposition: form-data; name=\"referer\"\n\nffmovie.fun\n--{bound}\nContent-Disposition: form-data; name=\"SubmitButtoncolors\"\n\n{random2}\n--{bound}--";

            string year = "";
            try {
                year = $"&y={activeMovie.title.year[0..4]}";
            }
            catch (Exception) {

            }

            string request = (isMovie ? "https://123files.club/imdb/play/?id=" : "https://123files.club/imdb/tv/?id=") + imdbId + year + (isMovie ? "" : $"&s={season}&e={episode}");
            string d = core.PostRequest(request, request, data, tempThred, $"multipart/form-data; boundary={bound}");

            string _downloadLink = FindHTML(d, "<a href=\"/download/", "\"");
            if (_downloadLink != "") {
                _downloadLink = "https://123files.club/download/" + _downloadLink;
                string dSource = core.PostRequest(_downloadLink, _downloadLink, $"imdb={imdbId}&imdbGO=");
                const string lookFor = "<a href=\"";
                while (dSource.Contains(lookFor)) {
                    string link = FindHTML(dSource, lookFor, "\"");

                    // DONT USE mixdrop.co, THEY HAVE RECAPTCHA TO GET TOKEN
                    if (link.Contains("googleusercontent")) {
                        AddPotentialLink(normalEpisode, link, "GoogleVideo Files", 13);
                    }

                    dSource = RemoveOne(dSource, lookFor);
                }
            }
        }
    }
}