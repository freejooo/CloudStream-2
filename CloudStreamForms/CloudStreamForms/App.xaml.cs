using Plugin.LocalNotifications;
using Plugin.Settings;
using Plugin.Settings.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xamarin.Essentials;
using Xamarin.Forms;
using static CloudStreamForms.CloudStreamCore;

namespace CloudStreamForms
{


    public partial class App : Application
    {
        public const string baseM3u8Name = @"mirrorlist.m3u8";
        public const string baseSubtitleName = @"subtitles.srt";

        public interface IPlatformDep
        {
            void ToggleRealFullScreen(bool fullscreen);
            void PlayVlc(string url, string name, string subtitleLoc);
            void PlayVlc(List<string> url, List<string> name, string subtitleLoc);
            void ShowToast(string message, double duration);
            string DownloadFile(string file, string fileName, bool mainPath, string extraPath);
            string DownloadUrl(string url, string fileName, bool mainPath, string extraPath, string toast = "", bool isNotification = false, string body = "");
            bool DeleteFile(string path);
            void DownloadUpdate(string update);
            string GetDownloadPath(string path, string extraFolder);
            StorageInfo GetStorageInformation(string path = "");
            int ConvertDPtoPx(int dp);
            string GetExternalStoragePath();
            void HideStatusBar();
            void ShowStatusBar();
            void UpdateStatusBar();
            void UpdateBackground(int color);
            void UpdateBackground();
            void LandscapeOrientation();
            void NormalOrientation();
            void ToggleFullscreen(bool fullscreen);
            void SetBrightness(double opacity);
            double GetBrightness();
            void ShowNotIntent(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "");
            void Test();
            EventHandler<bool> OnAudioFocusChanged { set; get; }
            bool GainAudioFocus();
            void ReleaseAudioFocus();
            void CancelNot(int id);
            string DownloadHandleIntent(int id, List<string> mirrorNames, List<string> mirrorUrls, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "");
            string DownloadAdvanced(int id, string url, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "");
            DownloadProgressInfo GetDownloadProgressInfo(int id, string fileUrl);
        }

        public enum DownloadState { Downloading, Downloaded, NotDownloaded, Paused }
        public enum DownloadType { Normal = 0, YouTube = 1 }

        [System.Serializable]
        public class DownloadInfo
        {
            public DownloadProgressInfo state;
            public DownloadEpisodeInfo info;
        }

        public class DownloadProgressInfo
        {
            public DownloadState state;

            public long bytesDownloaded;
            public long totalBytes;
            public double ProcentageDownloaded { get { return ((bytesDownloaded*100.0) / totalBytes); } }
        }

        [System.Serializable]
        public class DownloadEpisodeInfo
        {
            /// <summary>
            /// Youtube is the hashed id, else the IMDB id
            /// </summary> 
            public string source;

            public string name;
            public string description;
            public int id;
            public int episode;
            public int season;
            public string fileUrl;
            public int downloadHeader;
            public DownloadType dtype;
        }

        [System.Serializable]
        public class DownloadHeader
        {
            public string name;
            public string ogName;
            //public string altName;
            public string id;
            public int RealId { get { return int.Parse(id.Replace("tt", "")); } }
            public string year;
            public string ogYear => year.Substring(0, 4);
            public string rating;
            public string runtime;
            public string posterUrl;
            public string description;
            //public int seasons;
            public string hdPosterUrl;
            public CloudStreamCore.MovieType movieType;

            //public CloudStreamCore.Title title; 
        }

        static string GetPathFromType(DownloadHeader header)
        {
            string path = "Movies";
            if (header.movieType == MovieType.Anime) {
                path = "Anime";
            }
            else if (header.movieType == MovieType.TVSeries) {
                path = "TVSeries";
            }
            return path;
        }


        public static string RequestDownload(int id, string name, string description, int episode, int season, List<string> mirrorUrls, List<string> mirrorNames, string downloadTitle, string poster, CloudStreamCore.Title title)
        {
            DownloadHeader header = ConvertTitleToHeader(title);
            string extraPath = "/" + GetPathFromType(header);
            print("HEADERID::: " + header.RealId);
            App.SetKey(nameof(DownloadHeader), "id" + header.RealId, header);
            bool isMovie = header.movieType == MovieType.AnimeMovie || header.movieType == MovieType.Movie;

            string fileUrl = platformDep.DownloadHandleIntent(id, mirrorNames, mirrorUrls, downloadTitle, name, true, extraPath, true, true, false, poster, isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));
            App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.Normal, source = header.id, description = description, downloadHeader = header.RealId, episode = episode, season = season, fileUrl = fileUrl, id = id, name = name });

            App.SetKey("DownloadIds", id.ToString(), id);

            return fileUrl;
            // (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n
            //  string dpath = App.DownloadAdvanced(GetCorrectId(episodeResult), mirrorUrl, episodeResult.Title + ".mp4", isMovie ? currentMovie.title.name : $"{currentMovie.title.name} · {episodeResult.OgTitle}", true, "/" + GetPathFromType(), true, true, false, episodeResult.PosterUrl, (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n");
            //   string dpath = App.DownloadAdvanced(GetCorrectId(episodeResult), mirrorUrl, episodeResult.Title + ".mp4", isMovie ? currentMovie.title.name : $"{currentMovie.title.name} · {episodeResult.OgTitle}", true, "/" + GetPathFromType(), true, true, false, episodeResult.PosterUrl, (isMovie) ? $"{mirrorName}\n" : $"S{currentSeason}:E{episodeResult.Episode} - {mirrorName}\n");
        }


        public static DownloadHeader ConvertTitleToHeader(CloudStreamCore.Title title)
        {
            return new DownloadHeader() { description = title.description, hdPosterUrl = title.hdPosterUrl, id = title.id, name = title.name, ogName = title.ogName, posterUrl = title.posterUrl, rating = title.rating, runtime = title.runtime, year = title.year,movieType=title.movieType };
        }

        public static DownloadInfo GetDownloadInfo(int id)
        {
            var info = GetDownloadEpisodeInfo(id);
            if (info == null) return null;
            return new DownloadInfo() { info = info, state = platformDep.GetDownloadProgressInfo(id,info.fileUrl) };
        }

        public static DownloadEpisodeInfo GetDownloadEpisodeInfo(int id)
        {
            return App.GetKey<DownloadEpisodeInfo>(nameof(DownloadEpisodeInfo), "id" + id, null);
        }

        public static DownloadHeader GetDownloadHeaderInfo(int id)
        {
            print("HEADERIDFRONM::: " + id);
            return App.GetKey<DownloadHeader>(nameof(DownloadHeader), "id" + id, null);
        }

        public class StorageInfo
        {
            public long TotalSpace = 0;
            public long AvailableSpace = 0;
            public long FreeSpace = 0;
            public long UsedSpace { get { return TotalSpace - AvailableSpace; } }
            /// <summary>
            /// From 0-1
            /// </summary>
            public double UsedProcentage { get { return ConvertBytesToGB(UsedSpace, 4) / ConvertBytesToGB(TotalSpace, 4); } }
        }

        public static void OnDownloadProgressChanged(string path, DownloadProgressChangedEventArgs progress)
        {
            // Main.print("PATH: " + path + " | Progress:" + progress.ProgressPercentage);
        }

        public static void Test()
        {
            platformDep.Test();
        }

        private static IPlatformDep _platformDep;
        public static IPlatformDep platformDep
        {
            set {
                _platformDep = value;
                _platformDep.OnAudioFocusChanged += (o, e) => { OnAudioFocusChanged?.Invoke(o, e); };
            }
            get { return _platformDep; }
        }

        public static EventHandler<bool> OnAudioFocusChanged;

        public static bool GainAudioFocus()
        {
            return platformDep.GainAudioFocus();
        }

        public static void ReleaseAudioFocus()
        {
            platformDep.ReleaseAudioFocus();
        }

        public static void UpdateStatusBar()
        {
            platformDep.UpdateStatusBar();
        }

        public static void ToggleFullscreen(bool fullscreen)
        {
            platformDep.ToggleFullscreen(fullscreen);
        }

        public static void ToggleRealFullScreen(bool fullscreen)
        {
            platformDep.ToggleRealFullScreen(fullscreen);
        }

        public static double GetBrightness()
        {
            return platformDep.GetBrightness();
        }

        public static string DownloadAdvanced(int id, string url, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "")
        {
            return platformDep.DownloadAdvanced(id, url, fileName, titleName, mainPath, extraPath, showNotification, showNotificationWhenDone, openWhenDone, poster, beforeTxt);
        }

        public static void SetBrightness(double brightness)
        {
            platformDep.SetBrightness(brightness);
        }

        public static void ShowNotIntent(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "")
        {
            platformDep.ShowNotIntent(title, body, id, titleId, titleName, time, bigIconUrl);
        }

        public static void UpdateBackground(int color = -1)
        {
            if (color == -1) {
                color = Math.Max(0, Settings.BlackColor - 10);
            }
            CloudStreamForms.MainPage.mainPage.BarBackgroundColor = new Color(color / 255.0, color / 255.0, color / 255.0, 1);
            platformDep.UpdateBackground(color);
        }

        public static void UpdateToTransparentBg()
        {
            platformDep.UpdateBackground();
        }

        public static void HideStatusBar()
        {
            platformDep.HideStatusBar();
        }
        public static void ShowStatusBar()
        {
            platformDep.ShowStatusBar();
        }

        public static void LandscapeOrientation()
        {
            platformDep.LandscapeOrientation();
        }

        public static void NormalOrientation()
        {
            platformDep.NormalOrientation();
        }

        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        public static int ConvertDPtoPx(int dp)
        {
            return platformDep.ConvertDPtoPx(dp);
        }

        public static StorageInfo GetStorage()
        {
            return platformDep.GetStorageInformation();
        }

        public static double ConvertBytesToGB(long bytes, int digits = 2)
        {
            return ConvertBytesToAny(bytes, digits, 3);
        }

        public static double ConvertBytesToAny(long bytes, int digits = 2, int steps = 3)
        {
            int div = GetSizeOfJumpOnSystem();
            return Math.Round((bytes / Math.Pow(div, steps)), digits);
        }

        public static int GetSizeOfJumpOnSystem()
        {
            return Device.RuntimePlatform == Device.UWP ? 1024 : 1000;
        }

        public static bool DeleteFile(string path)
        {
            return platformDep.DeleteFile(path);
        }
        public static void PlayVLCWithSingleUrl(string url, string name = "", string subtitleLoc = "", bool? overrideSelectVideo = null)
        {
            //PlayVlc?.Invoke(null, url);

            bool useVideo = overrideSelectVideo ?? Settings.UseVideoPlayer;

            if (useVideo) {
                Page p = new VideoPage(new VideoPage.PlayVideo() { descript = "", name = "", isSingleMirror = true, episode = -1, season = -1, MirrorNames = new List<string>() { name }, MirrorUrls = new List<string>() { url }, Subtitles = new List<string>(), SubtitlesNames = new List<string>() });//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
                ((MainPage)CloudStreamCore.mainPage).Navigation.PushModalAsync(p, false);
            }
            else {
                platformDep.PlayVlc(url, name, subtitleLoc);
            }

            //platformDep.PlayVlc(url, name, subtitleLoc);
        }

        public static void ShowToast(string message, double duration = 2.5)
        {
            platformDep.ShowToast(message, duration);
        }

        public static string GetBuildNumber()
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        public static void DownloadNewGithubUpdate(string update)
        {
            platformDep.DownloadUpdate(update);
        }

        public static string GetDownloadPath(string path, string extraFolder)
        {
            return platformDep.GetDownloadPath(path, extraFolder);
        }


        public static void PlayVLCWithSingleUrl(List<string> url, List<string> name, string subtitleLoc = "", string publicName = "", int episode = -1, int season = -1, bool? overrideSelectVideo = null)
        {
            bool useVideo = overrideSelectVideo ?? Settings.UseVideoPlayer;

            if (useVideo) {
                Page p = new VideoPage(new VideoPage.PlayVideo() { descript = "", name = publicName, isSingleMirror = false, episode = episode, season = season, MirrorNames = name, MirrorUrls = url, Subtitles = new List<string>(), SubtitlesNames = new List<string>() });//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
                ((MainPage)CloudStreamCore.mainPage).Navigation.PushModalAsync(p, false);
            }
            else {
                platformDep.PlayVlc(url, name, subtitleLoc);
            }
            //PlayVlc?.Invoke(null, url);

        }

        static string GetKeyPath(string folder, string name = "")
        {
            string _s = ":" + folder + "-";
            if (name != "") {
                _s += name + ":";
            }
            return _s;
        }

        public static void SetKey(string folder, string name, object value)
        {
            string path = GetKeyPath(folder, name);
            string _set = ConvertToString(value);
            if (myApp.Properties.ContainsKey(path)) {
                CloudStreamCore.print("CONTAINS KEY");
                myApp.Properties[path] = _set;
            }
            else {
                CloudStreamCore.print("ADD KEY");
                myApp.Properties.Add(path, _set);
            }
        }

        public static T GetKey<T>(string folder, string name, T defVal)
        {
            string path = GetKeyPath(folder, name);
            return GetKey<T>(path, defVal);
        }

        public static void RemoveFolder(string folder)
        {
            List<string> keys = App.GetKeysPath(folder);
            for (int i = 0; i < keys.Count; i++) {
                RemoveKey(keys[i]);
            }
        }

        public static T GetKey<T>(string path, T defVal)
        {
            try {
                if (myApp.Properties.ContainsKey(path)) {
                    // CloudStreamCore.print("GETKEY::" + myApp.Properties[path]);
                    // CloudStreamCore.print("GETKEY::" + typeof(T).ToString() + "||" + ConvertToObject<T>(myApp.Properties[path] as string, defVal));
                    return (T)ConvertToObject<T>(myApp.Properties[path] as string, defVal);
                }
                else {
                    return defVal;
                }
            }
            catch (Exception) {
                return defVal;
            }

        }

        public static List<T> GetKeys<T>(string folder)
        {
            List<string> keyNames = GetKeysPath(folder);

            List<T> allKeys = new List<T>();
            foreach (var key in keyNames) {
                allKeys.Add((T)myApp.Properties[key]);
            }

            return allKeys;
        }

        public static int GetKeyCount(string folder)
        {
            return GetKeysPath(folder).Count;
        }


        public static List<string> GetKeysPath(string folder)
        {
            List<string> keyNames = myApp.Properties.Keys.Where(t => t.StartsWith(GetKeyPath(folder))).ToList();
            return keyNames;
        }

        public static bool KeyExists(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            return KeyExists(path);
        }
        public static bool KeyExists(string path)
        {
            return (myApp.Properties.ContainsKey(path));
        }
        public static void RemoveKey(string folder, string name)
        {
            string path = GetKeyPath(folder, name);
            RemoveKey(path);
        }
        public static void RemoveKey(string path)
        {
            if (myApp.Properties.ContainsKey(path)) {
                myApp.Properties.Remove(path);
            }
        }
        static Application myApp { get { return Application.Current; } }

        public static T ConvertToObject<T>(string str, T defValue)
        {
            try {
                return FromByteArray<T>(Convert.FromBase64String(str));

            }
            catch (Exception) {
                return defValue;
            }
        }

        public static T FromByteArray<T>(byte[] rawValue)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream(rawValue)) {
                return (T)bf.Deserialize(ms);
            }
        }

        public static string ConvertToString(object o)
        {
            return Convert.ToBase64String(ToByteArray(o));
        }

        public static byte[] ToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream()) {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }


        public static void ShowNotification(string title, string body)
        {
            CrossLocalNotifications.Current.Show(title, body);
        }

        public static void ShowNotification(string title, string body, int id, int sec)
        {
            CrossLocalNotifications.Current.Show(title, body, id, DateTime.Now.AddSeconds(sec));
        }

        public static void ShowNotification(string title, string body, int id, DateTime time)
        {
            CrossLocalNotifications.Current.Show(title, body, id, time);
        }

        public static void CancelNotifaction(int id)
        {
            CrossLocalNotifications.Current.Cancel(id);
            platformDep.CancelNot(id);
        }

        private static ISettings AppSettings =>
    CrossSettings.Current;
        public static ImageSource GetImageSource(string inp)
        {
            return ImageSource.FromResource("CloudStreamForms.Resource." + inp, Assembly.GetExecutingAssembly());
        }

        public static string DownloadUrl(string url, string fileName, bool mainPath = true, string extraPath = "", string toast = "", bool isNotification = false, string body = "")
        {
            return platformDep.DownloadUrl(url, fileName, mainPath, extraPath, toast, isNotification, body);
        }

        public static string DownloadFile(string file, string fileName, bool mainPath = true, string extraPath = "")
        {
            return platformDep.DownloadFile(file, fileName, mainPath, extraPath);
        }

        public static string ConvertPathAndNameToM3U8(List<string> path, List<string> name, bool isSubtitleEnabled = false, string beforePath = "", string overrideSubtitles = null)
        {
            string _s = "#EXTM3U";
            if (isSubtitleEnabled) {
                _s += "\n";
                _s += "\n";
                //  _s += "#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"English\",DEFAULT=YES,AUTOSELECT=YES,FORCED=NO,LANGUAGE=\"en\",CHARACTERISTICS=\"public.accessibility.transcribes-spoken-dialog, public.accessibility.describes-music-and-sound\",URI=" + beforePath + baseSubtitleName + "\"";
                _s += "#EXTVLCOPT:sub-file=" + (overrideSubtitles ?? (beforePath + baseSubtitleName));
                _s += "\n";
            }
            for (int i = 0; i < path.Count; i++) {
                _s += "\n#EXTINF:" + ", " + name[i].Replace("-", "").Replace("  ", " ") + "\n" + path[i]; //+ (isSubtitleEnabled ? ",SUBTITLES=\"subs\"" : "");
            }
            return _s;
        }

        public static byte[] ConvertPathAndNameToM3U8Bytes(List<string> path, List<string> name, bool isSubtitleEnabled = false, string beforePath = "")
        {
            return Encoding.UTF8.GetBytes(ConvertPathAndNameToM3U8(path, name, isSubtitleEnabled, beforePath));
        }

        public static void OpenBrowser(string url)
        {
            CloudStreamCore.print("Trying to open: " + url);
            if (CloudStreamCore.CheckIfURLIsValid(url)) {
                try {
                    Launcher.OpenAsync(new Uri(url));
                }
                catch (Exception) {
                    CloudStreamCore.print("BROWSER LOADED ERROR, SHOULD NEVER HAPPEND!!");
                }
            }
        }
        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }


}
