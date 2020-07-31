using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CloudStreamForms.Core.BlotFreeProvider;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core.AnimeProviders
{
    class AnimeFeberBloatFreeProvider : BloatFreeBaseAnimeProvider
    {
        AnimeFeberHelper helper;
        public AnimeFeberBloatFreeProvider(CloudStreamCore _core) : base(_core)
        {
            helper = new AnimeFeberHelper(_core);
        }
        public override string Name => "AnimeFeber";

        public override object StoreData(string year, TempThread tempThred, MALData malData)
        {
            return helper.GetSearchResults(activeMovie.title.name, false);
        } 

        struct AnimbeFeberVideo
        {
            public string mainUrl;
            public string name;
            public List<AdvancedAudioStream> audioStreams;
        }

        public override void LoadLink(string episodeLink, int episode, int normalEpisode, TempThread tempThred, object extraData)
        {
            try {
                if (episodeLink != "") {
                    int id = int.Parse(episodeLink);
                    string d = helper.GetAnimeFeverEpisodeStream(id);
                    if (d == "") return;

                    string[] dSplit = d.Split('\n');
                    bool nextIsVideoUrl = false;
                    string videoData = "";


                    Dictionary<string, AnimbeFeberVideo> streams = new Dictionary<string, AnimbeFeberVideo>();

                    foreach (var _line in dSplit) {
                        var line = _line.Replace(" ", "");
                        if (nextIsVideoUrl) {
                            nextIsVideoUrl = false;
                            string[] data = CoreHelpers.GetStringRegex("BANDWIDTH=?, RESOLUTION=?, AUDIO=\"?\"", videoData);
                            if (data == null) continue;
                            string key = data[2];
                            if (data != null) {
                                if (streams.ContainsKey(key)) {
                                    var _stream = streams[key];
                                    _stream.mainUrl = line;
                                    _stream.name = data[1];
                                    streams[key] = _stream;
                                }
                                else {
                                    error("FATAL EX MISSMATCH IN ANIMEFEBEER:: " + d);
                                }
                            }
                            print("VIDEOURL: " + data[0]);
                        }
                        else {
                            if (line.StartsWith("#EXT-X-STREAM-INF:BANDWIDTH")) { // VIDEO
                                nextIsVideoUrl = true;
                                videoData = line;
                            }
                            else if (line.StartsWith("#EXT-X-MEDIA:TYPE=AUDIO")) { // AUDIO
                                string[] data = CoreHelpers.GetStringRegex("GROUP-ID=\"?\" NAME=\"?\" LANGUAGE=\"?\" URI=\"?\"", line);
                                if (data != null) {
                                    string key = data[0];
                                    string name = data[1];
                                    string url = data[3];
                                    if (streams.ContainsKey(key)) {
                                        streams[key].audioStreams.Add(new AdvancedAudioStream() {
                                            label = name,
                                            url = url,
                                        });
                                    }
                                    else {
                                        streams.Add(key, new AnimbeFeberVideo() {
                                            audioStreams = new List<AdvancedAudioStream>() {
                                                new AdvancedAudioStream() {
                                                    label = name,
                                                    url = url,
                                                }
                                            }
                                        });
                                    }
                                }
                                print("AUDIO: " + data[3] + " | " + data[2]);
                            }
                        }
                    }

                    foreach (var key in streams.Keys) {
                        var _stream = streams[key];
                        BasicLink basicLink = new BasicLink() {
                            isAdvancedLink = true,
                            originSite = Name,
                            mirror = 0,
                            name = "AnimeVibe",
                            label = _stream.name,
                            typeName = "m3u8",
                            baseUrl = _stream.mainUrl,
                            audioStreams = _stream.audioStreams,
                        };
                        AddPotentialLink(normalEpisode, basicLink);
                    }
                }
            }
            catch (Exception) { }
        }

        public override NonBloatSeasonData GetSeasonData(MALSeason ms, TempThread tempThread, string year, object storedData)
        {
            AnimeFeberHelper.AnimeFeberSearchInfo data = (AnimeFeberHelper.AnimeFeberSearchInfo)storedData;
            NonBloatSeasonData setData = new NonBloatSeasonData() { dubEpisodes = new List<string>(), subEpisodes = new List<string>() };
            foreach (var subData in data.data) {
                if (subData.name == ms.engName || subData.alt_name == ms.engName) {
                    try {
                        var mainInfo = helper.GetAnimeFeberEpisodeInfo(subData.id, subData.slug);

                        var emtyList = new string[mainInfo.data.Count].ToList();
                        int index = 0;
                        setData.dubEpisodes = emtyList;
                        setData.subEpisodes = emtyList;
                        foreach (var epInfo in mainInfo.data) {
                            var langs = epInfo.video_meta.audio_languages;
                            if (langs.Contains("eng")) {
                                setData.dubEpisodes[index] = (epInfo.id.ToString());
                            }
                            if (langs.Contains("jap")) {
                                setData.subEpisodes[index] = (epInfo.id.ToString());
                            }
                            index++;
                        }

                    }
                    catch (Exception) {

                    }
                }

            }
            return setData;
        }
    }
}
