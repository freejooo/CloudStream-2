using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using Jint;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
//using Android.Util;
//using Android.Content;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using static CloudStreamForms.CloudStreamCore;
using Application = Xamarin.Forms.Application;
using Button = Xamarin.Forms.Button;

namespace CloudStreamForms
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer


    [DesignTimeVisible(false)]
    public partial class MainPage : Xamarin.Forms.TabbedPage
    {
        public const string DARK_BLUE_COLOR = "#303F9F";
        public const string LIGHT_BLUE_COLOR = "#829eff";
        public const string LIGHT_BLACK_COLOR = "#595959";
        public const string LIGHT_LIGHT_BLACK_COLOR = "#e6e6e6";
        public const string BLACK_COLOR = "#111111";
        public const string ITEM_COLOR = "#617eff";
        public const string LIGHT_DARK_BLUE_COLOR = "#1976D2";

        public const bool IS_EMTY_BUILD = false;
        public const bool IS_TEST_VIDEO = false;

        public static string intentData = "";
        public static MainPage mainPage;

        [Serializable]
        public struct BookmarkPoster
        {
            public string name;
            public string posterUrl;
            public string id;
            //  public Button button;
        }

        public static bool NewGithubUpdate
        {
            get {
                if (githubUpdateTag == "") { return false; }
                else { return ("v" + App.GetBuildNumber() != githubUpdateTag); }

            }
        }

        public static string githubUpdateTag = "";
        public static string githubUpdateText = "";

        public static void CheckGitHubUpdate()
        {
            try {
                if (Device.RuntimePlatform == Device.Android) { // ONLY ANDROID CAN UPDATE
                    TempThred tempThred = new TempThred();
                    tempThred.typeId = 4; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                    tempThred.Thread = new System.Threading.Thread(() => {
                        try {
                            string d = DownloadString("https://github.com/LagradOst/CloudStream-2/releases", tempThred);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string look = "/LagradOst/CloudStream-2/releases/tag/";
                            //   float bigf = -1;
                            //     string bigUpdTxt = "";
                            // while (d.Contains(look)) {
                            githubUpdateTag = FindHTML(d, look, "\"");
                            githubUpdateText = FindHTML(d, look + githubUpdateTag + "\">", "<");
                            print("UPDATE SEARCHED: " + githubUpdateTag + "|" + githubUpdateText);
                        }
                        finally {
                            JoinThred(tempThred);
                        }
                    });
                    tempThred.Thread.Name = "GitHub Update Thread";
                    tempThred.Thread.Start();
                }
            }
            catch (Exception _ex) {
                print("Github ex::" + _ex);
            }

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            App.Test();
        }

        public MainPage()
        {
            InitializeComponent(); mainPage = this; CloudStreamCore.mainPage = mainPage;

            if (IS_TEST_VIDEO) {
                Page p = new VideoPage(new VideoPage.PlayVideo() { descript = "", name = "Black Bunny", episode = -1, season = -1, MirrorNames = new List<string>() { "Googlevid" }, MirrorUrls = new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, Subtitles = new List<string>(), SubtitlesNames = new List<string>() });//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
                Navigation.PushModalAsync(p, false);
            }
            // Page _p = new ChromeCastPage();// { mainPoster = mainPoster };
            // Navigation.PushModalAsync(_p, false);

            if (IS_EMTY_BUILD) return;


            List<string> names = new List<string>() { "Home", "Search", "Downloads", "Settings" };
            //List<string> icons = new List<string>() { "homeIcon.png", "searchIcon.png", "downloadIcon.png", "baseline_settings_applications_white_48dp_inverted_big.png" };
            List<string> icons = new List<string>() { "outline_home_white_48dp.png", "searchIcon.png", "outline_get_app_white_48dp.png", "outline_settings_white_48dp.png" };
            List<Page> pages = new List<Page>() { new Home(), new Search(), new Download(), new Settings(), };


            for (int i = 0; i < names.Count; i++) {
                Children.Add(pages[i]);
                Children[i].Title = names[i];
                Children[i].IconImageSource = icons[i];
            }

            LateCheck();

            On<Xamarin.Forms.PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);


            // BarBackgroundColor = Color.Black;
            //   BarTextColor = Color.OrangeRed;

            //PushPageFromUrlAndName("tt4869896", "Overlord");
            //  PushPageFromUrlAndName("tt0409591", "Naruto");
            //  PushPageFromUrlAndName("tt10885406", "Ascendance of a Bookworm");
            // PushPageFromUrlAndName("tt9054364", "That Time I Got Reincarnated as a Slime");
            //PushPageFromUrlAndName("tt0371746", "Iron Man");
            // PushPageFromUrlAndName("tt10954274", "ID: Invaded");
        }


        async void LateCheck()
        {
            await Task.Delay(5000);
            try {
                CheckGitHubUpdate();
                MainChrome.StartImageChanger();
                MainChrome.GetAllChromeDevices();
            }
            catch (Exception _ex) {
                print("ERROR IN LATECHECK::: " + _ex);
            }

        }

        public static void PushPageFromUrlAndName(string url, string name)
        {
            try {
                Poster _p = new Poster() { url = url, name = name };
                Search.PushPage(_p, MainPage.mainPage.Navigation);
            }
            catch (Exception) {

            }
        }

        public static async Task PushPageFromUrlAndName(string intentData)
        {
            string url = FindHTML(intentData, "cloudstreamforms:", "Name=");
            string name = FindHTML(intentData, "Name=", "=EndAll");
            //Task.Delay(10000);
            if (name != "" && url != "") {
                PushPageFromUrlAndName(url, System.Web.HttpUtility.UrlDecode(name));
            }
        }        /// <summary>
                 /// Creates color with corrected brightness.
                 /// </summary>
                 /// <param name="color">Color to correct.</param>
                 /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
                 /// Negative values produce darker colors.</param>
                 /// <returns>
                 /// Corrected <see cref="Color"/> structure.
                 /// </returns>
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0) {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromRgba((int)red, (int)green, (int)blue, color.A);
        }
        // -------------------------------- END --------------------------------
    }

    public static class MainChrome
    {
        public static event EventHandler OnDisconnected;
        public static event EventHandler<bool> OnVideoCastingChanged;
        public static event EventHandler OnConnected;
        public static event EventHandler OnChromeDevicesFound;
        public static event EventHandler<string> OnChromeImageChanged;
        public static event EventHandler<bool> OnPauseChanged;

        private static ChromeNotification _notification = new ChromeNotification() { isCasting = false, title = "", body = "", isPaused = false, posterUrl = "" };
        public static ChromeNotification Notification
        {
            set {
                _notification = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, value);
                });
            }
            get { return _notification; }
        }

        public class ChromeNotification
        {
            public string title;
            public string body; // MIRRORNAME
            public string posterUrl;
            public bool isCasting;
            public bool isPlaying;
            public bool isPaused;
        }

        public static event EventHandler<ChromeNotification> OnNotificationChanged;


        public static bool IsChromeDevicesOnNetwork
        {
            get {
                if (allChromeDevices == null) { return false; }
                foreach (IReceiver r in allChromeDevices) {
                    return true;
                }
                return false;
            }
        }

        public static bool IsConnectedToChromeDevice { set; get; }
        public static bool IsPendingConnection { set; get; }
        public static bool IsCastingVideo
        {
            set {
                _isCastingVideo = value; Notification.isCasting = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnVideoCastingChanged?.Invoke(null, value); OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return _isCastingVideo; }
        }
        private static bool _isCastingVideo;
        static bool _IsPaused = false;



        //            Notification.isPaused = IsPaused;

        private static bool isPaused;
        private static bool isPlaying;
        public static bool IsPaused
        {
            set {
                isPaused = value; Notification.isPaused = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return isPaused; }
        }
        public static bool IsPlaying
        {
            set {
                isPlaying = value; Notification.isPlaying = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return isPlaying; }
        }

        public static bool IsBuffering { set; get; }
        public static bool IsIdle { set; get; }

        public static double CurrentCastingDuration { get; set; }

        public static IEnumerable<IReceiver> allChromeDevices;
        public static IMediaChannel CurrentChannel;
        public static MediaStatus CurrentChromeMedia;
        public static Sender chromeSender;
        public static IReceiver chromeRecivever;
        static DateTime castUpdatedNow;
        static double castLastUpdate;

        private static float _Volume = 1;
        public static float Volume
        {
            set {
                SetVolumeAsync(value); _Volume = value;
            }
            get { return _Volume; }
        }

        public static double CurrentTime
        {
            get {
                try {
                    // double test = CurrentChannel.Status.First().CurrentTime; // WILL CAUSE CRASH IF STOPPED BY EXTRARNAL
                    if (RequestNextTime != -1 && IsBuffering) {
                        return RequestNextTime;
                    }

                    if (IsPaused) {
                        return PausedTime;
                    }
                    else if (IsBuffering || IsIdle) {
                        return BufferTime;
                    }
                    else {
                        TimeSpan t = DateTime.Now.Subtract(castUpdatedNow);
                        double currentTime = castLastUpdate + t.TotalSeconds;
                        return currentTime;
                    }
                }
                catch (System.Exception) {
                    return CurrentCastingDuration; // CAST STOPPED FROM EXTERNAL
                }
            }
        }

        public static double PausedTime { set; get; } = 0;
        public static double BufferTime { set; get; } = 0;
        public static double RequestNextTime { set; get; } = 0;

        private static bool IsStopped
        {
            get {
                return CurrentChannel.Status == null || !string.IsNullOrEmpty(CurrentChannel.Status.FirstOrDefault()?.IdleReason);
            }
        }

        static int CurrentImage = 0;

        public static string MultiplyString(string s, int times)
        {
            return String.Concat(Enumerable.Repeat(s, times));
        }

        public static string ConvertScoreToArcadeScore(object inp, int maxLetters = 8, string multiString = "0")
        {
            string inpS = inp.ToString();
            inpS = MultiplyString(multiString, maxLetters - inpS.Length) + inpS;
            return inpS;
        }

        public static string GetSourceFromInt(int inp = -1)
        {
            if (inp == -1) {
                inp = 4;
            }
            if (inp == 0) {
                return "round_cast_white_48dp2_0.png";
            }
            return "round_cast_white_48dp_blue_" + inp + ".png";//"ic_media_route_connected_dark_" + ConvertScoreToArcadeScore(inp, 2) + "_mtrl.png";
        }

        public static string CurrentImageSource { get { return GetSourceFromInt(CurrentImage); } }

        public static async Task StartImageChanger()
        {
            while (true) {
                int lastImage = int.Parse(CurrentImage.ToString());
                if (IsPendingConnection) {
                    CurrentImage++;
                    if (CurrentImage > 3) {
                        CurrentImage = 1;
                    }
                }
                else {
                    CurrentImage += IsConnectedToChromeDevice ? 1 : -1;
                }
                if (CurrentImage < 0) CurrentImage = 0;
                if (CurrentImage > 4) CurrentImage = 4;
                if (!IsChromeDevicesOnNetwork) {
                    CurrentImage = 0;
                }
                if (lastImage != CurrentImage) {
                    Device.BeginInvokeOnMainThread(() => {
                        OnChromeImageChanged.Invoke(null, CurrentImageSource);
                    });
                }
                await Task.Delay(500);
            }
        }

        public static async void GetAllChromeDevices()
        {
            try {
                print("SCANNING");
                allChromeDevices = await new DeviceLocator().FindReceiversAsync();
                print("SCANNED");
                print("FOUND " + allChromeDevices.ToList().Count + " CHROME DEVICES");
                if (IsChromeDevicesOnNetwork) {
                    Device.BeginInvokeOnMainThread(() => {
                        OnChromeDevicesFound?.Invoke(null, null);
                    });
                }
            }
            catch (Exception _ex) {
                print("ERROR LOADING CHROME::" + _ex);
            }

        }

        public static List<string> GetChromeDevicesNames()
        {
            if (allChromeDevices == null) {
                return new List<string>();
            }
            List<string> allNames = new List<string>();
            foreach (IReceiver r in allChromeDevices) {
                allNames.Add(r.FriendlyName);
            }
            return allNames;
        }

        private static void ChromeChannel_StatusChanged(object sender, EventArgs e)
        {
            MediaStatus mm = CurrentChannel.Status.FirstOrDefault();

            IsPlaying = (mm.PlayerState == "PLAYING");
            IsBuffering = (mm.PlayerState == "BUFFERING");
            IsIdle = (mm.PlayerState == "IDLE");
            IsPaused = (mm.PlayerState == "PAUSED");

            BufferTime = CurrentTime;
            PausedTime = CurrentTime;

            if (IsPlaying && RequestNextTime != -1) {
                print("SET NEXT REQUEST TO -1");
                RequestNextTime = -1;
            }

            if (_IsPaused != IsPaused) {
                _IsPaused = IsPaused;
                Device.BeginInvokeOnMainThread(() => {
                    OnPauseChanged?.Invoke(null, IsPaused);
                });
            }


            print("STATE::" + mm.PlayerState);



            castUpdatedNow = DateTime.Now;
            castLastUpdate = mm.CurrentTime;
        }

        static Dictionary<string, string> subtitleParsed = new Dictionary<string, string>();

        // Subtitle Url https://static.movies123.pro/files/tracks/JhUzWRukqeuUdRrPCe0R3lUJ1SmknAVSv670Ep0cXipm1JfMgNWa379VKKAz8nvFMq2ksu7bN5tCY5tXXKS4Lrr1tLkkipdLJNArNzVSu2g.srt
        public static async Task<bool> CastVideo(string url, string mirrorName, double setTime = -1, string subtitleUrl = "", string subtitleName = "", string posterUrl = "", string movieTitle = "")
        {
            try {
                if (setTime == -2) {
                    setTime = CurrentTime;
                }

                GenericMediaMetadata mediaMetadata = new GenericMediaMetadata();
                RequestNextTime = setTime;

                bool validSubtitle = false;
                var mediaInfo = new MediaInformation() { ContentId = url, Metadata = mediaMetadata };
                //   subtitleUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/CastVideos/tracks/DesigningForGoogleCast-en.vtt";
                /*
                if (subtitleParsed.ContainsKey(subtitleUrl)) {
                    subtitleUrl = subtitleParsed[subtitleUrl];
                    print("SUBURL:::" + subtitleUrl);
                }
                else {
                    //  print("SUBTITLEURL:::: " + subtitleUrl);
                    if (subtitleUrl != "") { 
                        string _subtitleUrl = GetUrlFromUploadSubtitles(subtitleUrl, ".vtt");
                        subtitleParsed.Add(subtitleUrl, _subtitleUrl);
                        print("SUBURL:::" + subtitleUrl);
                        subtitleUrl = _subtitleUrl;
                    }
                }*/

                // SUBTITLES
                if (subtitleUrl != "") {
                    validSubtitle = true;
                    mediaInfo.Tracks = new Track[]
                                {
                                 new Track() {  TrackId = 1, Language = "en-US" , Name = subtitleName, TrackContentId = subtitleUrl,SubType=TextTrackType.Subtitles,Type=TrackType.Text}
                                };
                    mediaInfo.TextTrackStyle = new TextTrackStyle() {
                        BackgroundColor = System.Drawing.Color.Transparent,//.Color.Transparent,
                        EdgeColor = System.Drawing.Color.Black,
                        EdgeType = TextTrackEdgeType.DropShadow
                    };
                }
                mediaMetadata.Title = mirrorName;

                if (validSubtitle) {
                    CurrentChromeMedia = await CurrentChannel.LoadAsync(mediaInfo, true, 1);
                }
                else {
                    CurrentChromeMedia = await CurrentChannel.LoadAsync(mediaInfo);
                }

                print("START!!");

                CurrentCastingDuration = (double)CurrentChromeMedia.Media.Duration;
                if (setTime != -1) {
                    SetChromeTime(setTime);
                }
                CurrentChannel.StatusChanged += ChromeChannel_StatusChanged;
                print("!3");

                castUpdatedNow = DateTime.Now;
                castLastUpdate = 0;
                IsCastingVideo = true;
                Notification.body = mirrorName;
                Notification.title = movieTitle;
                Notification.posterUrl = posterUrl;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
                print("!4");
                return true;
            }
            catch (System.Exception) {
                print("ERROR");
                await Task.CompletedTask;
                return false;
            }
        }

        public static EventHandler<double> OnForceUpdateTime;


        public static void SetChromeTime(double time)
        {
            print("Seek To: " + time);

            BufferTime = time;
            PausedTime = time;

            IsBuffering = true;
            IsPlaying = false;

            castUpdatedNow = DateTime.Now;
            castLastUpdate = time;

            OnForceUpdateTime?.Invoke(null, time);
            CurrentChannel.SeekAsync(time);
        }

        public static void SeekMedia(double sec)
        {
            SetChromeTime(CurrentTime + sec);
        }

        public static void PauseAndPlay(bool paused)
        {
            try {
                if (paused) {
                    PausedTime = CurrentTime;
                    CurrentChannel.PauseAsync();
                }
                else {
                    CurrentChannel.PlayAsync();
                }
            }
            catch (Exception _ex) {
                print("EX PAUSED::: " + _ex);
            }

        }

        public static async void StopCast()
        {
            try {
                await CurrentChannel.StopAsync();
            }
            catch (System.Exception) {
                await Task.CompletedTask;
            }
            try {
                CurrentChannel.Sender.Disconnect();
            }
            catch (System.Exception) {

            }
            Device.BeginInvokeOnMainThread(() => {
                OnDisconnected?.Invoke(null, null);
            });
            IsConnectedToChromeDevice = false;
            IsCastingVideo = false;
            print("STOP CASTING!");
        }

        public static async void ConnectToChromeDevice(string name)
        {
            if (name == "Disconnect") {
                StopCast();
                return;
            }

            // if (chromeRecivers.Count() > 0) {
            foreach (IReceiver r in allChromeDevices) {
                if (r.FriendlyName == name) {
                    chromeSender = new Sender();

                    // Connect to the Chromecast
                    try {
                        IsPendingConnection = true;
                        await chromeSender.ConnectAsync(r);
                        chromeRecivever = r;
                        Console.WriteLine("CONNECTED");
                        CurrentChannel = chromeSender.GetChannel<IMediaChannel>();
                        await chromeSender.LaunchAsync(CurrentChannel);
                        IsConnectedToChromeDevice = true;
                        Device.BeginInvokeOnMainThread(() => {
                            OnConnected?.Invoke(null, null);
                        });
                    }
                    catch (System.Exception) {
                        await Task.CompletedTask; // JUST IN CASE
                    }
                    IsPendingConnection = false;
                    return;
                }
            }
            //}
        }

        private static async Task SetVolumeAsync(float level) // 0 = 0%, 1 = 100%
        {
            if (IsCastingVideo) {
                await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null, async c => await c.SetVolumeAsync(level));
            }
            else {
                await Task.CompletedTask;
            }
        }

        private static async Task SendChannelCommandAsync<TChannel>(bool condition, Func<TChannel, Task> action, Func<TChannel, Task> otherwise) where TChannel : IChannel => await InvokeAsync(condition ? action : otherwise);

        private static async Task InvokeAsync<TChannel>(Func<TChannel, Task> action) where TChannel : IChannel
        {
            if (action != null) {
                await action.Invoke(chromeSender.GetChannel<TChannel>());
            }
        }


        static string GetUrlFromUploadSubtitles(string fullData, string ending = ".srt")
        {
            string boundary = Guid.NewGuid().ToString();

            WebRequest request = WebRequest.Create("https://uguu.se/api.php?d=upload-tool");
            request.Method = "POST";
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            //request.Headers.Add(HttpRequestHeader.Cookie, header);
            string ff = string.Format("--{0}", boundary) + "\n";

            string textMode = "plain";
            if (ending == ".vtt") {
                textMode = "vtt";
            }

            string _1 = $"{ff}Content-Disposition: form-data; name=\"MAX_FILE_SIZE\"\n\n150000000\n";
            string _2 = $"{ff}Content-Disposition: form-data; name=\"file\"; filename=\"subtitles{ending}\"\nContent-Type: text/{textMode}\n\n{fullData}\n\n";
            string _3 = $"{ff}Content-Disposition: form-data; name=\"name\"\n\n\n";
            string _4 = $"--{boundary}--";

            byte[] _data = Encoding.UTF8.GetBytes(_1 + _2 + _3 + _4);

            if (_data != null) {
                request.ContentLength = _data.Length;
            }

            Stream dataStream = request.GetRequestStream();

            if (_data != null && _data.Length > 0) {
                dataStream.Write(_data, 0, _data.Length);
            }

            dataStream.Close();

            WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);

            string responseReader = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseReader;
        }
    }
    // -------------------------------- END CHROMECAST --------------------------------

    [Serializable]
    public static class CloudStreamCore
    {
        public static object mainPage;


        // ========================================================= CONSTS =========================================================
        #region CONSTS
        public const bool MOVIES_ENABLED = true;
        public const bool TVSERIES_ENABLED = true;
        public const bool ANIME_ENABLED = true;

        public const bool CHROMECAST_ENABLED = true;
        public const bool DOWNLOAD_ENABLED = true;
        public const bool SEARCH_FOR_UPDATES_ENABLED = true;

        public const bool INLINK_SUBTITLES_ENABLED = false;
        public static bool globalSubtitlesEnabled { get { return Settings.SubtitlesEnabled; } }
        public const bool GOMOSTEAM_ENABLED = true;
        public const bool SUBHDMIRROS_ENABLED = true;
        public const bool FMOVIES_ENABLED = false;
        public const bool BAN_SUBTITLE_ADS = true;

        public const bool PLAY_SELECT_ENABLED = false;

        public const bool DEBUG_WRITELINE = true;
        public const string USERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.87 Safari/537.36";
        public const int MIRROR_COUNT = 10; // SUB HD MIRRORS


        public const string VIDEO_IMDB_IMAGE_NOT_FOUND = "emtyPoster.png";// "https://i.giphy.com/media/u2Prjtt7QYD0A/200.webp"; // from https://media0.giphy.com/media/u2Prjtt7QYD0A/200.webp?cid=790b7611ff76f40aaeea5e73fddeb8408c4b018b6307d9e3&rid=200.webp

        public const bool REPLACE_IMDBNAME_WITH_POSTERNAME = true;
        public static double posterRezMulti = 1.0;
        public const string GOMOURL = "gomo.to";
        #endregion

        // ========================================================= THREDS =========================================================

        #region Threads
        public static List<int> activeThredIds = new List<int>();
        public static List<TempThred> tempThreds = new List<TempThred>();
        public static int thredNumber = 0; // UNIQUE THRED NUMBER FOR EATCH THREAD CREATED WHEN TEMPTHREDS THRED IS SET

        /// <summary>
        /// Same as thred.Join(); but removes refrense in tempThreds 
        /// </summary>
        /// <param name="tempThred"></param>
        public static void JoinThred(TempThred tempThred)
        {
            try {
                activeThredIds.Remove(tempThred.ThredId);

            }
            catch (Exception) {
            }
            GetThredActive(tempThred);
        }

        public static bool GetThredActive(TempThred tempThred, bool autoPurge = true)
        {
            bool active = activeThredIds.Contains(tempThred.ThredId);
            if (!active && autoPurge) { PurgeThred(tempThred); }
            return active;
        }

        public static void PurgeThred(TempThred tempThred)
        {
            try {
                // print("Purged: " + tempThred.ThredId);
                activeThredIds.Remove(tempThred.ThredId);
                tempThreds.Remove(tempThred);
                // print(tempThred.Thread.Name);
                /*
                try {

                    if (DeviceInfo.Platform == DevicePlatform.UWP) {
                        tempThred.Thread.Join();
                        tempThred.Thread.Abort();

                    }
                    else {
                        //tempThred.Thread.Join();

                    }

                }
                catch (Exception) {
                }*/


            }
            catch (Exception) {

            }

        }

        /// <summary>
        ///  THRED ID IS THE THREDS POURPOSE
        ///  0=Normal, 1=SEARCHTHRED, 2=GETTITLETHREAD, 3=LINKTHRED, 4=DOWNLOADTHRED, 5=TRAILERTHREAD, 6=EPISODETHREAD
        /// </summary>
        public static void PurgeThreds(int typeId)
        {
            print("PURGING ALL THREADS TYPE OF: " + typeId);
            if (typeId == -1) {
                activeThredIds = new List<int>();
                tempThreds = new List<TempThred>();
            }
            else {
                //  List<int> _activeThredIds = new List<int>();
                List<TempThred> _tempThreds = new List<TempThred>();
                foreach (TempThred t in tempThreds) {
                    if (t.typeId == typeId) {
                        //  PurgeThred(t);
                        _tempThreds.Add(t);
                    }
                    /*
                    if (t.typeId != typeId) {
                        _tempThreds.Add(t);
                        _activeThredIds.Add(t.ThredId);
                    }
                    else {
                        try {

                        }
                        catch (Exception) {

                            throw;
                        }
                        t.Thread.Abort();
                    }
                    */
                }
                for (int i = 0; i < _tempThreds.Count; i++) {
                    PurgeThred(_tempThreds[i]);
                }
                //activeThredIds = _activeThredIds;
                //  tempThreds = _tempThreds;
            }
        }
        #endregion

        // ========================================================= DATA =========================================================

        #region Data

        [System.Serializable]
        public struct MirrorInfo
        {
            public string name;
            public string url;
        }
        [Serializable]
        public enum MovieType { Movie, TVSeries, Anime, AnimeMovie, YouTube }
        [Serializable]
        public enum PosterType { Imdb, Raw }

        [Serializable]
        public struct FMoviesData
        {
            public string url;
            public int season;
        }

        [Serializable]
        public struct TempThred
        {
            /// <summary>
            /// THRED ID IS THE THREDS POURPOSE
            /// 0=Normal, 1=SEARCHTHRED, 2=GETTITLETHREAD, 3=LINKTHRED, 4=DOWNLOADTHRED, 5=TRAILERTHREAD, 6=EPISODETHREAD
            /// </summary>
            public int typeId;

            private int _thredId;
            /// <summary>
            /// THR ID IF THE THRED (UNIQUE)
            /// </summary>
            public int ThredId { private set { _thredId = value; } get { return _thredId; } }

            public System.Threading.Thread _thread;
            public System.Threading.Thread Thread
            {
                set {
                    if (_thread == null) {
                        thredNumber++; _thredId = thredNumber; activeThredIds.Add(_thredId); tempThreds.Add(this);
                    }
                    _thread = value;
                }
                get { return _thread; }
            }
        }

        [Serializable]
        public struct IMDbTopList
        {
            public string name;
            public string id;
            public string img;
            public string runtime;
            public string rating;
            public string genres;
            public string descript;
            public int place;
            public List<int> contansGenres;
        }

        [Serializable]
        public struct Trailer
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public string PosterUrl { get; set; }
        }

        [Serializable]
        public struct FishWatch
        {
            public string imdbScore;
            public string title;
            public string removedTitle;
            public int season;
            public string released;
            public string href;
        }

        [Serializable]
        public struct Movies123
        {
            public string year;
            public string imdbRating;
            public string genre;
            public string plot;
            public string runtime;
            public string posterUrl;
            public string name;
            public MovieType type;
        }

        [Serializable]
        public struct MALSeason
        {
            public string malUrl;
            public string name;
            public string japName;
            public string engName;
            public List<string> synonyms;

            public GogoAnimeData gogoData;
            public DubbedAnimeData dubbedAnimeData;
            public KickassAnimeData kickassAnimeData;
            public AnimeFlixData animeFlixData;
            public DubbedAnimeNetData dubbedAnimeNetData;
        }

        [System.Serializable]
        public struct DubbedAnimeNetData
        {
            public bool dubExists;
            public bool subExists;
            public DubbedAnimeNetEpisode[] EpisodesUrls;
        }

        [Serializable]
        public struct DubbedAnimeNetEpisode
        {
            public string href;
            public bool dubExists;
            public bool subExists;
        }

        [Serializable]
        public struct AnimeFlixData
        {
            public bool dubExists;
            public bool subExists;
            public AnimeFlixEpisode[] EpisodesUrls;
        }

        public struct AnimeFlixEpisode
        {
            public int id;
            public bool dubExists;
            public bool subExists;
        }


        [Serializable]
        public struct GogoAnimeData
        {
            public bool dubExists;
            public bool subExists;
            public string subUrl;
            public string dubUrl;
        }
        [Serializable]
        public struct KickassAnimeData
        {
            public bool dubExists;
            public bool subExists;
            public string subUrl;
            public string dubUrl;
            public string[] dubEpisodesUrls;
            public string[] subEpisodesUrls;
        }

        [Serializable]

        public struct DubbedAnimeData
        {
            public bool dubExists;
            public string slug;
        }

        [Serializable]
        public struct MALSeasonData
        {
            public List<MALSeason> seasons;
            public string malUrl;
        }

        [Serializable]
        public struct MALData
        {
            public string japName;
            public string engName;
            public string firstName;
            public List<MALSeasonData> seasonData;
            public bool done;
            public bool loadSeasonEpCountDone;
            public List<int> currentActiveGoGoMaxEpsPerSeason;
            public List<int> currentActiveDubbedMaxEpsPerSeason;
            public List<int> currentActiveKickassMaxEpsPerSeason;
            public string currentSelectedYear;
        }



        [Serializable]
        public struct Title
        {
            public string name;
            public string ogName;
            //public string altName;
            public string id;
            public string year;
            public string ogYear => year.Substring(0, 4);
            public string rating;
            public string runtime;
            public string posterUrl;
            public string description;
            public int seasons;
            public string hdPosterUrl;

            public MALData MALData;

            public MovieType movieType;
            public List<string> genres;
            public List<Trailer> trailers;
            public List<Poster> recomended;

            public Movies123MetaData movies123MetaData;
            public List<YesmoviessSeasonData> yesmoviessSeasonDatas; // NOT SORTED; MAKE SURE TO SEARCH ALL

            public List<WatchSeriesHdMetaData> watchSeriesHdMetaData;// NOT SORTED; MAKE SURE TO SEARCH ALL
            public List<FMoviesData> fmoviesMetaData;// NOT SORTED; MAKE SURE TO SEARCH ALL

            public string shortEpView;

            public bool IsMovie { get { return (movieType == MovieType.AnimeMovie || movieType == MovieType.Movie); } }
        }

        [Serializable]
        public struct YesmoviessSeasonData
        {
            public string url;
            public int id;
        }

        [Serializable]
        public struct Movies123MetaData
        {
            public string movieLink;
            public List<Movies123SeasonData> seasonData;
        }

        [Serializable]
        public struct WatchSeriesHdMetaData
        {
            public string url;
            public int season;
        }

        [Serializable]
        public struct Movies123SeasonData
        {
            public string seasonUrl;
            public List<string> episodeUrls;
        }

        [Serializable]
        public struct Poster
        {
            public string name;
            public string extra; // (Tv-Series) for exampe
            public string posterUrl;
            public string year;
            public string rank;
            //public string id; // IMDB ID

            public string url;
            public PosterType posterType; // HOW DID YOU GET THE POSTER, IMDB SEARCH OR SOMETHING ELSE
        }

        #region QuickSearch
        [System.Serializable]
        public struct DubbedAnimeEpisode
        {
            public string rowid;
            public string title;
            public string desc;
            public string status;
            public object skips;
            public int totalEp;
            public string ep;
            public int NextEp;
            public string slug;
            public string wideImg;
            public string year;
            public string showid;
            public string Epviews;
            public string TotalViews;
            public string serversHTML;
            public string preview_img;
            public string tags;
        }
        [System.Serializable]
        public struct DubbedAnimeSearchResult
        {
            public List<DubbedAnimeEpisode> anime;
            public bool error;
            public object errorMSG;
        }
        [System.Serializable]
        public struct DubbedAnimeSearchRootObject
        {
            public DubbedAnimeSearchResult result;
        }
        [System.Serializable]
        struct MALSearchPayload
        {
            public string media_type;
            public int start_year;
            public string aired;
            public string score;
            public string status;
        }

        [System.Serializable]
        struct MALSearchItem
        {
            public int id;
            public string type;
            public string name;
            public string url;
            public string image_url;
            public string thumbnail_url;
            public MALSearchPayload payload;
            public string es_score;
        }

        [System.Serializable]
        struct MALSearchCategories
        {
            public string type;
            public MALSearchItem[] items;
        }

        [System.Serializable]
        struct MALQuickSearch
        {
            public MALSearchCategories[] categories;
        }
        [System.Serializable]
        struct IMDbSearchImage
        {
            public int height;
            /// <summary>
            /// Image
            /// </summary>
            public string imageUrl;
            public int width;
        }

        [System.Serializable]
        struct IMDbSearchTrailer
        {
            /// <summary>
            /// Image
            /// </summary>
            public IMDbSearchImage i;
            /// <summary>
            /// Video ID
            /// </summary>
            public string id;
            /// <summary>
            /// Trailer Name
            /// </summary>
            public string l;
            /// <summary>
            /// Duration
            /// </summary>
            public string s;
        }

        [System.Serializable]
        struct IMDbSearchItem
        {
            /// <summary>
            /// Poster
            /// </summary>
            public IMDbSearchImage i;
            /// <summary>
            /// Id
            /// </summary>
            public string id;
            /// <summary>
            /// Title Name
            /// </summary>
            public string l;
            /// <summary>
            /// feature, TV series, video
            /// </summary>
            public string q;
            /// <summary>
            /// Rank
            /// </summary>
            public int rank;
            /// <summary>
            /// Actors
            /// </summary>
            public string s;
            /// <summary>
            /// Trailers
            /// </summary>
            public IMDbSearchTrailer[] v;
            /// <summary>
            /// IDK
            /// </summary>
            public int vt;
            /// <summary>
            /// Year
            /// </summary>
            public int y;
            /// <summary>
            /// YearString
            /// </summary>
            public string yr;
        }

        [System.Serializable]
        struct IMDbQuickSearch
        {
            /// <summary>
            /// Search Items
            /// </summary>
            public IMDbSearchItem[] d;
            /// <summary>
            /// Search Term
            /// </summary>
            public string q;
            public int v;
        }
        #endregion

        [Serializable]
        public struct Link
        {
            public string name;
            public string url;
            public int priority;
        }

        [Serializable]
        public struct Episode
        {
            public List<Link> links;
            public string name;
            public string rating;
            public string description;
            public string date;
            public string posterUrl;
            public string id;

            //private int _progress;
            // public int Progress { set { _progress = value; linkAdded?.Invoke(null, value); } get { return _progress; } }
        }

        [Serializable]
        public struct Subtitle
        {
            public string name;
            //public string url;
            public string data;
        }

        [Serializable]
        public struct Movie
        {
            public Title title;
            public List<Subtitle> subtitles;
            public List<Episode> episodes;
            public List<MoeEpisode> moeEpisodes;
        }

        //  public struct NotificationData

        [Serializable]
        public struct MoeEpisode
        {
            public DateTime timeOfRelease;
            public DateTime timeOfMesure;
            public TimeSpan DiffTime { get { return timeOfRelease.Subtract(timeOfMesure); } }

            public string episodeName;
            public int number;
        }


        [Serializable]
        struct MoeService
        {
            public string service;
            public string serviceId;
        }
        [Serializable]
        struct MoeLink
        {
            public string Title;
            public string URL;
        }

        [Serializable]
        struct MoeMediaTitle
        {
            public string Canonical;
            public string Romaji;
            public string English;
            public string Japanese;
            public string Hiragana;
            public string[] Synonyms;
        }

        [Serializable]
        struct MoeApi
        {
            public string id;
            public string type;
            public MoeMediaTitle title;
            public string summary;
            public string status;
            public string[] genres;
            public string startDate;
            public string endDate;
            public int episodeCount;
            public int episodeLength;
            public string source;
            public MoeService[] mappings;
            //  Image image;
            public string firstChannel;
            //   AnimeRating rating;
            //  AnimePopularity popularity;
            //  ExternalMedia[] trailers;
            public string[] episodes;
            public string[] studios;
            public string[] producers;
            public string[] licensors;
            public MoeLink[] links;
        }
        #endregion

        // ========================================================= EVENTS =========================================================

        #region Events
        public static List<Poster> activeSearchResults = new List<Poster>();
        public static Movie activeMovie = new Movie();
        public static string activeTrailer = "";

        public static event EventHandler<Poster> addedSeachResult;
        public static event EventHandler<Movie> titleLoaded;
        public static event EventHandler<List<Poster>> searchLoaded;
        public static event EventHandler<List<Trailer>> trailerLoaded;
        public static event EventHandler<List<Episode>> episodeLoaded;
        public static event EventHandler<Link> linkAdded;
        public static event EventHandler<MALData> malDataLoaded;
        public static event EventHandler<Episode> linksProbablyDone;

        public static event EventHandler<Movie> movie123FishingDone;
        public static event EventHandler<Movie> yesmovieFishingDone;
        public static event EventHandler<Movie> watchSeriesFishingDone;
        public static event EventHandler<Movie> fmoviesFishingDone;
        public static event EventHandler<Movie> fishingDone;
        public static event EventHandler<List<MoeEpisode>> moeDone;

        public struct FishLoaded
        {
            public string name;
            public double progress;
        }

        public static event EventHandler<FishLoaded> fishProgressLoaded;
        //public static event EventHandler<Movie> yesmovieFishingDone;

        private static Random rng = new Random();
        #endregion

        // ========================================================= ALL METHODS =========================================================

        public static IMovieProvider[] movieProviders = new IMovieProvider[] { new FullMoviesProvider(), new TMDBProvider(), new WatchTVProvider(), new FMoviesProvider(), new LiveMovies123Provider(), new TheMovies123Provider(), new YesMoviesProvider(), new WatchSeriesProvider(), new GomoStreamProvider(), new Movies123Provider() };

        public static IAnimeProvider[] animeProviders = new IAnimeProvider[] { new GogoAnimeProvider(), new KickassAnimeProvider(), new DubbedAnimeProvider(), new AnimeFlixProvider(), new DubbedAnimeNetProvider() };

        public interface IMovieProvider // FOR MOVIES AND SHOWS
        {
            void FishMainLinkTSync();
            void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred);
        }

        public interface IAnimeProvider
        {
            string Name { get; }
            void FishMainLink(string year, TempThred tempThred, MALData malData);
            void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred);

            //List<string> GetAllLinks(Movie currentMovie, int currentSeason, bool isDub);

            int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred);
        }

        #region =================================================== ANIME PROVIDERS ===================================================


        static class AnimeProviderHelper
        {
            public static object _lock = new object();

            public static void ConvertEpisodeToSeasonPart(int episode, int season)
            {

            }
        }

        class GogoAnimeProvider : IAnimeProvider
        {
            public string Name { get => "Gogoanime"; }

            public void FishMainLink(string year, TempThred tempThred, MALData malData)
            {
                print("start");
                if (activeMovie.title.MALData.japName != "error") {
                    print("DOWNLOADING");
                    string d = DownloadString("https://www9.gogoanime.io/search.html?keyword=" + activeMovie.title.MALData.japName.Substring(0, Math.Min(5, activeMovie.title.MALData.japName.Length)), tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    string look = "<p class=\"name\"><a href=\"/category/";

                    while (d.Contains(look)) {
                        string ur = FindHTML(d, look, "\"").Replace("-dub", "");
                        print("S" + ur);
                        string adv = FindHTML(d, look, "</a");
                        string title = FindHTML(adv, "title=\"", "\"").Replace(" (TV)", ""); // TO FIX BLACK CLOVER
                        string animeTitle = title.Replace(" (Dub)", "");
                        string __d = RemoveOne(d, look);
                        string __year = FindHTML(__d, "Released: ", " ");
                        int ___year = int.Parse(__year);
                        int ___year2 = int.Parse(year);

                        if (___year >= ___year2) {

                            // CHECKS SYNONYMES
                            /*
                            for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                                for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                                    MALSeason ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                                }
                            }*/

                            // LOADS TITLES
                            for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                                for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                                    MALSeason ms;
                                    lock (AnimeProviderHelper._lock) {
                                        ms = activeMovie.title.MALData.seasonData[i].seasons[q];
                                    }

                                    bool containsSyno = false;
                                    for (int s = 0; s < ms.synonyms.Count; s++) {
                                        if (ToLowerAndReplace(ms.synonyms[s]) == ToLowerAndReplace(animeTitle)) {
                                            containsSyno = true;
                                        }
                                        //  print("SYNO: " + ms.synonyms[s]);
                                    }

                                    //  print(ur + "|" + animeTitle.ToLower() + "|" + ms.name.ToLower() + "|" + ms.engName.ToLower() + "|" + ___year + "___" + ___year2 + "|" + containsSyno);

                                    if (ToLowerAndReplace(ms.name) == ToLowerAndReplace(animeTitle) || ToLowerAndReplace(ms.engName) == ToLowerAndReplace(animeTitle) || containsSyno) {
                                        // print("ADDED:::" + ur);

                                        lock (AnimeProviderHelper._lock) {

                                            var baseData = activeMovie.title.MALData.seasonData[i].seasons[q];
                                            if (animeTitle == title) {
                                                baseData.gogoData.subExists = true;
                                                baseData.gogoData.subUrl = ur;

                                            }
                                            else {
                                                baseData.gogoData.dubExists = true;
                                                baseData.gogoData.dubUrl = ur.Replace("-dub", "") + "-dub";
                                            }

                                            /*
                                            if (animeTitle == title) {
                                                 //= new MALSeason() { name = ms.name, subUrl = ur, dubUrl = ms.dubUrl, subExists = true, dubExists = ms.dubExists, japName = ms.japName, engName = ms.engName, synonyms = ms.synonyms };
                                            }
                                            else {
                                                activeMovie.title.MALData.seasonData[i].seasons[q] //= new MALSeason() { name = ms.name, dubUrl = ur.Replace("-dub", "") + "-dub", subUrl = ms.subUrl, dubExists = true, subExists = ms.subExists, japName = ms.japName, engName = ms.engName, synonyms = ms.synonyms };
                                            }*/
                                            activeMovie.title.MALData.seasonData[i].seasons[q] = baseData;
                                        }

                                    }
                                }
                            }
                        }
                        d = d.Substring(d.IndexOf(look) + 1, d.Length - d.IndexOf(look) - 1);
                    }
                    for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                        for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                            var ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                            if (ms.gogoData.dubExists) {
                                print(i + ". " + ms.name + " | Dub E " + ms.gogoData.dubUrl);
                            }
                            if (ms.gogoData.subExists) {
                                print(i + ". " + ms.name + " | Sub E " + ms.gogoData.subUrl);
                            }
                        }
                    }
                }
            }

            public List<string> GetAllLinks(Movie currentMovie, int currentSeason, bool isDub)
            {
                List<string> baseUrls = new List<string>();

                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].gogoData;

                        if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                            //  dstring = ms.baseUrl;
                            string burl = isDub ? ms.dubUrl : ms.subUrl;
                            if (!baseUrls.Contains(burl)) {
                                baseUrls.Add(burl);
                            }
                            //print("BASEURL " + ms.baseUrl);
                        }
                    }
                }
                catch (Exception) { }
                return baseUrls;
            }

            public int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred)
            {
                List<string> baseUrls = GetAllLinks(currentMovie, currentSeason, isDub);
                if (baseUrls.Count > 0) {
                    List<int> saved = new List<int>();

                    for (int i = 0; i < baseUrls.Count; i++) {
                        string dstring = baseUrls[i];
                        dstring = dstring.Replace("-dub", "") + (isDub ? "-dub" : "");
                        string d = DownloadString("https://www9.gogoanime.io/category/" + dstring);
                        if (d != "") {
                            if (tempThred != null) {
                                if (!GetThredActive((TempThred)tempThred)) { return 0; }; // COPY UPDATE PROGRESS
                            }
                            string subMax = FindHTML(d, "class=\"active\" ep_start = \'", ">");
                            string maxEp = FindHTML(subMax, "ep_end = \'", "\'");//FindHTML(d, "<a href=\"#\" class=\"active\" ep_start = \'0\' ep_end = \'", "\'");
                            print(i + "MAXEP" + maxEp);
                            print(baseUrls[i]);
                            int _epCount = (int)Math.Floor(decimal.Parse(maxEp));
                            //max += _epCount;
                            try {
                                saved.Add(_epCount);
                            }
                            catch (Exception) {

                            }
                        }
                    }
                    activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason = saved;
                    return saved.Sum();

                }
                else {
                    return 0;
                }
            }
            public static List<string> GetAllGogoLinksFromAnime(Movie currentMovie, int currentSeason, bool isDub)
            {
                List<string> baseUrls = new List<string>();

                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].gogoData;

                        if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                            //  dstring = ms.baseUrl;
                            string burl = isDub ? ms.dubUrl : ms.subUrl;
                            if (!baseUrls.Contains(burl)) {
                                baseUrls.Add(burl);
                            }
                            //print("BASEURL " + ms.baseUrl);
                        }
                    }
                }
                catch (Exception) {
                }
                return baseUrls;
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred)
            {
                /*
                TempThred tempThred = new TempThred();
                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/

                try {
                    if (episode <= activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Sum()) {
                        string fwordLink = "";
                        List<string> fwords = GetAllGogoLinksFromAnime(activeMovie, season, isDub);
                        // for (int i = 0; i < fwords.Count; i++) {
                        // print("FW: " + fwords[i]);
                        //  }

                        // --------------- GET WHAT SEASON THE EPISODE IS IN ---------------

                        int sel = -1;
                        int floor = 0;
                        int subtract = 0;
                        // print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason);
                        if (activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason != null) {
                            for (int i = 0; i < activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Count; i++) {
                                int seling = floor + activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];

                                if (episode > floor && episode <= seling) {
                                    sel = i;
                                    subtract = floor;
                                }
                                //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                                floor += activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];
                            }
                        }
                        //print("sel: " + sel);
                        if (sel != -1) {
                            try {
                                fwordLink = fwords[sel].Replace("-dub", "") + (isDub ? "-dub" : "");
                            }
                            catch (Exception) {

                            }
                        }

                        if (fwordLink != "") { // IF FOUND
                            string dstring = "https://www3.gogoanime.io/" + fwordLink + "-episode-" + (episode - subtract);
                            print("DSTRING: " + dstring);
                            string d = DownloadString(dstring, tempThred);

                            AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                        }
                    }
                }
                catch (Exception) {
                    print("GOGOANIME ERROR");
                }
                //if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                /* }
                 finally {
                     JoinThred(tempThred);
                 }
             });
             tempThred.Thread.Name = "QuickSearch";
             tempThred.Thread.Start();*/



            }
        }

        class KickassAnimeProvider : IAnimeProvider
        {
            public string Name { get => "Kickassanime"; }

            public void FishMainLink(string year, TempThred tempThred, MALData malData)
            {
                string query = malData.firstName;
                string url = "https://www.kickassanime.rs/search?q=" + query;//activeMovie.title.name.Replace(" ", "%20");
                print("COMPAREURL:" + url);
                string d = DownloadString(url);
                print("DOWNLOADEDDDD::" + d);
                const string lookfor = "\"name\":\"";
                while (d.Contains(lookfor)) {
                    string animeTitle = FindHTML(d, lookfor, "\"");
                    const string dubTxt = "(Dub)";
                    const string cenTxt = "(Censored)";
                    bool isDub = animeTitle.Contains(dubTxt);
                    bool cencored = animeTitle.Contains(cenTxt);

                    animeTitle = animeTitle.Replace(cenTxt, "").Replace(dubTxt, "").Replace(" ", "");

                    d = RemoveOne(d, lookfor);
                    string slug = "https://www.kickassanime.rs" + FindHTML(d, "\"slug\":\"", "\"").Replace("\\/", "/");

                    for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                        for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                            MALSeason ms;

                            lock (AnimeProviderHelper._lock) {
                                ms = activeMovie.title.MALData.seasonData[i].seasons[q];
                            }

                            string compareName = ms.name.Replace(" ", "");
                            bool containsSyno = false;
                            for (int s = 0; s < ms.synonyms.Count; s++) {
                                if (ToLowerAndReplace(ms.synonyms[s]) == ToLowerAndReplace(animeTitle)) {
                                    containsSyno = true;
                                }
                                //  print("SYNO: " + ms.synonyms[s]);
                            }

                            //  print(animeTitle.ToLower() + "|" + ms.name.ToLower() + "|" + ms.engName.ToLower() + "|" + ___year + "___" + ___year2 + "|" + containsSyno);
                            print("COMPARE: " + compareName + "|" + animeTitle);
                            if (ToLowerAndReplace(compareName) == ToLowerAndReplace(animeTitle) || ToLowerAndReplace(ms.engName.Replace(" ", "")) == ToLowerAndReplace(animeTitle) || containsSyno || (animeTitle.ToLower().Replace(compareName.ToLower(), "").Length / (float)animeTitle.Length) < 0.3f) { // OVER 70 MATCH
                                print("FINISHED:::::" + slug);

                                string _d = DownloadString(slug);
                                // print(d);
                                const string _lookfor = "\"epnum\":\"";


                                int slugCount = Regex.Matches(_d, _lookfor).Count;
                                string[] episodes = new string[slugCount];
                                print("SLIGCOUNT:::DA" + slugCount);
                                //  Stopwatch s = new Stopwatch();
                                //  s.Start();



                                while (_d.Contains(_lookfor)) {
                                    try {
                                        //epnum":"Preview","name":null,"slug":"\/anime\/dr-stone-901389\/preview-170620","createddate":"2019-05-30 00:27:49"
                                        string epNum = FindHTML(_d, _lookfor, "\"");
                                        _d = RemoveOne(_d, _lookfor);

                                        string _slug = "https://www.kickassanime.rs" + FindHTML(_d, "\"slug\":\"", "\"").Replace("\\/", "/");
                                        //print("SLUGOS:" + _slug + "|" + epNum);
                                        string createDate = FindHTML(_d, "\"createddate\":\"", "\"");
                                        // string name = FindHTML(d, lookfor, "\"");
                                        //string slug = FindHTML(d, "\"slug\":\"", "\"").Replace("\\/", "/");
                                        if (epNum.StartsWith("Episode")) {
                                            int cEP = int.Parse(epNum.Replace("Episode ", ""));
                                            //   int change = Math.Max(cEP - episodes.Length, 0);
                                            episodes[cEP - 1] = _slug;
                                        }
                                        // print("SSLIUGPSPSOSO::" + epNum + "|" + slug + "|" + createDate);
                                    }
                                    catch (Exception) {
                                        print("SOMETHING LIKE 25.5");
                                    }

                                }
                                //    s.Stop();
                                print("EPISODES::::" + episodes.Length);

                                lock (AnimeProviderHelper._lock) {
                                    var baseData = activeMovie.title.MALData.seasonData[i].seasons[q];
                                    if (!isDub) {
                                        baseData.kickassAnimeData.subExists = true;
                                        baseData.kickassAnimeData.subUrl = slug;
                                        baseData.kickassAnimeData.subEpisodesUrls = episodes;

                                    }
                                    else {
                                        baseData.kickassAnimeData.dubExists = true;
                                        baseData.kickassAnimeData.dubUrl = slug;
                                        baseData.kickassAnimeData.dubEpisodesUrls = episodes;
                                    }

                                    activeMovie.title.MALData.seasonData[i].seasons[q] = baseData;
                                }
                            }
                        }
                    }
                    print(slug + "|" + animeTitle);
                }
                /*
                for (int i = 0; i < activeMovie.title.MALData.seasonData.Count; i++) {
                    for (int q = 0; q < activeMovie.title.MALData.seasonData[i].seasons.Count; q++) {
                        var ms = activeMovie.title.MALData.seasonData[i].seasons[q];

                        if (ms.kickassAnimeData.dubExists) {
                            print(i + ". " + ms.name + " | Dub E" + ms.kickassAnimeData.dubUrl);
                        }
                        if (ms.kickassAnimeData.subExists) {
                            print(i + ". " + ms.name + " | Sub E" + ms.kickassAnimeData.subUrl);
                        }
                    }
                }*/
            }

            public List<string> GetAllLinks(Movie currentMovie, int currentSeason, bool isDub)
            {
                List<string> baseUrls = new List<string>();
                print("CURRENSTSEASON:::" + currentSeason + "|" + isDub + "|" + currentMovie.title.MALData.seasonData.Count);
                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].kickassAnimeData;

                        if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                            //  dstring = ms.baseUrl;
                            baseUrls.AddRange(isDub ? ms.dubEpisodesUrls : ms.subEpisodesUrls);
                            print("BASEURL dada.:::" + (isDub ? ms.dubEpisodesUrls : ms.subEpisodesUrls));
                        }
                    }
                }
                catch (Exception) {
                }
                return baseUrls;
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred)
            {
                var kickAssLinks = GetAllLinks(activeMovie, season, isDub);
                print("KICKASSOS:" + normalEpisode);
                for (int i = 0; i < kickAssLinks.Count; i++) {
                    print("KICKASSLINK:" + i + ". |" + kickAssLinks[i]);
                }
                if (normalEpisode < kickAssLinks.Count) {
                    GetKickassVideoFromURL(kickAssLinks[normalEpisode], normalEpisode, tempThred);
                }
            }

            static void GetKickassVideoFromURL(string url, int normalEpisode, TempThred tempThred)
            {/*
                print("GETLINK;;;::" + url);
                TempThred tempThred = new TempThred();
                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string CorrectURL(string u)
                {
                    if (u.StartsWith("//")) {
                        u = "https:" + u;
                    }
                    return u.Replace("\\/", "/");
                }
                string Base64Decode(string base64EncodedData)
                {
                    var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                    return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
                }
                string GetCode(string _d)
                {
                    string res = FindHTML(_d, "Base64.decode(\"", "\"");
                    return Base64Decode(res);
                }

                void GetSources(string _s)
                {
                    print("DECODED: " + _s);
                    string daly = "https://www.dailymotion.com/embed";
                    string dalyKey = FindHTML(_s, daly, "\"");
                    if (dalyKey != "") {
                        dalyKey = daly + dalyKey;
                        string f = DownloadString(dalyKey);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        print(f);

                        string qulitys = FindHTML(f, "qualities\":{", "]}");
                        string find = "\"url\":\"";
                        while (qulitys.Contains(find)) {
                            string burl = FindHTML(qulitys, find, "\"").Replace("\\/", "/");
                            qulitys = RemoveOne(qulitys, find);
                            if (qulitys.Replace(" ", "") != "") {
                                AddPotentialLink(normalEpisode, burl, "KickassDaily", 0);
                            }
                            print("URL::" + burl);
                        }
                    }

                    string mp4Upload = "<source src=\"";
                    string __s = "<source" + FindHTML(_s, "<source", "</video>");
                    while (__s.Contains(mp4Upload)) {
                        string mp4UploadKey = FindHTML(__s, mp4Upload, "\"");
                        print("UR: " + mp4UploadKey);
                        __s = RemoveOne(__s, mp4Upload);
                        string label = FindHTML(__s, "label=\"", "\"");
                        AddPotentialLink(normalEpisode, mp4UploadKey, "KickassMp4 " + label, 2);

                    }


                    // =================== GETS LINKS WITH AUDIO SEPARATED FROM LINK :( ===================
                    /* 
                    string kickass = "playlist: [{file:\"";
                    string kickKey = FindHTML(_s, kickass, "\"").Replace("https:", "").Replace("http:", "");
                    if (kickKey != "") {
                        string s = RemoveHtmlChars(DownloadString("https:" + kickKey));
                        string lookFor = "<BaseURL>";
                        while (s.Contains(lookFor)) {
                            string label = FindHTML(s, "FBQualityLabel=\"", "\"");

                            string uri = FindHTML(s, lookFor, "<");
                            print("UR: " + label + "|" + uri);
                            AddPotentialLink(normalEpisode, uri, "KickassPlay " + label, 1);

                            s = RemoveOne(s, lookFor);
                        }
                    }*/


                    //file:"

                    if (_s.Contains("sources: [{file:\"")) {
                        string s = _s.ToString();
                        const string lookFor = "file:\"";

                        while (s.Contains(lookFor)) {
                            string uri = FindHTML(s, lookFor, "\"");
                            s = RemoveOne(s, lookFor);
                            string label = FindHTML(s, "label:\"", "\"");
                            if (label.Replace(" ", "") != "") {
                                AddPotentialLink(normalEpisode, uri, "KickassSource " + label.Replace("720P", "720p"), 1);
                            }
                            print("UR: " + label + "|" + uri);
                        }
                    }
                }

                void UrlDecoder(string _d, string _url)
                {
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string _s = GetCode(_d);
                    if (_s != "") {
                        GetSources(_s);
                    }
                    GetSources(_d);

                    string img = FindHTML(_d, "src=\"pref.php", "\"");
                    string beforeAdd = "pref.php";
                    if (img == "") {
                        img = FindHTML(_d, "<iframe src=\"", "\"");
                        beforeAdd = "";
                    }
                    print("IMG:" + img);
                    if (img != "") {
                        img = beforeAdd + img;
                        string next = GetBase(_url) + "/" + img;
                        string __d = DownloadString(next);
                        print(__d);
                        UrlDecoder(__d, next);
                    }
                    else {
                        string wLoc = "window.location = \'";
                        string subURL = FindHTML(_d, "adComplete", wLoc);
                        print("SUBFILE:" + subURL);
                        string subEr = CorrectURL(FindHTML(_d, subURL + wLoc, "\'"));
                        print("ED:" + subEr);
                        if (subEr != "") {
                            print("URI:" + subEr);
                            if (subEr.StartsWith("https://vidstreaming.io")) {
                                string dEr = DownloadString(subEr);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                AddEpisodesFromMirrors(tempThred, dEr, normalEpisode);
                            }
                            else {
                                UrlDecoder(DownloadString(subEr, repeats: 2, waitTime: 100), subEr);
                            }
                        }
                    }
                }

                string GetBase(string _url)
                {
                    string from = FindHTML(_url, "/", "?");
                    int _i = from.LastIndexOf("/");
                    from = from.Substring(_i, from.Length - _i);
                    return FindHTML("|" + _url, "|", "/" + from.Replace("/", ""));
                }

                string d = DownloadString(url);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                //"link":"

                string link1 = FindHTML(d, "link1\":\"", "\"").Replace("\\/", "/");
                link1 = CorrectURL(link1);
                string look1 = "\"link\":\"";
                string main = DownloadString(link1);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                UrlDecoder(main, link1);
                string look = "\"src\":\"";
                while (main.Contains(look)) {
                    string source = FindHTML(main, look, "\"").Replace("\\/", "/");
                    UrlDecoder(DownloadString(source), source);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    main = RemoveOne(main, look);
                }

                while (d.Contains(look1)) {
                    string source = FindHTML(d, look1, "\"").Replace("\\/", "/");
                    print(source);
                    UrlDecoder(DownloadString(source), source);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    d = RemoveOne(d, look1);
                }
                /*  }
                  finally {
                      JoinThred(tempThred);
                  }
              });
              tempThred.Thread.Name = "Kickass Link Extractor";
              tempThred.Thread.Start();*/

            }

            public int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred)
            {
                return GetAllLinks(currentMovie, currentSeason, isDub).Count;
            }
        }

        class DubbedAnimeNetProvider : IAnimeProvider
        {
            #region structs
            public struct DubbedAnimeNetRelated
            {
                public string Alternative_version { get; set; }
                public string Parent_story { get; set; }
                public string Other { get; set; }
                public string Prequel { get; set; }
                public string Side_story { get; set; }
                public string Sequel { get; set; }
                public string Character { get; set; }
            }

            public struct DubbedAnimeNetSearchResult
            {
                public string id { get; set; }
                public string slug { get; set; }
                public string title { get; set; }
                public string image { get; set; }
                public string synopsis { get; set; }
                public string english { get; set; }
                public string japanese { get; set; }
                public string synonyms { get; set; }
                public string type { get; set; }
                public string total { get; set; }
                public string status { get; set; }
                public string date { get; set; }
                public string aired { get; set; }
                public object year { get; set; }
                public object season { get; set; }
                public string premiered { get; set; }
                public string duration { get; set; }
                public string rating { get; set; }
                public string genres { get; set; }
                public List<DubbedAnimeNetRelated> related { get; set; }
                public string score { get; set; }
                public string rank { get; set; }
                public string popularity { get; set; }
                public string mal_id { get; set; }
                public string url { get; set; }
            }

            public struct DubbedAnimeNetQuickSearch
            {
                public List<DubbedAnimeNetSearchResult> results { get; set; }
                public int pages { get; set; }
                public string query { get; set; }
                public int total { get; set; }
            }

            public struct DubbedAnimeNetName
            {
                public string @default { get; set; }
                public string english { get; set; }
            }

            public struct DubbedAnimeNetVideo
            {
                public string host { get; set; }
                public string id { get; set; }
                public string type { get; set; }
                public string date { get; set; }
            }

            public struct DubbedAnimeNetAPIEpisode
            {
                public string id { get; set; }
                public string anime_id { get; set; }
                public string slug { get; set; }
                public string number { get; set; }
                public DubbedAnimeNetName name { get; set; }
                public string title { get; set; }
                public string description { get; set; }
                public string date { get; set; }
                public List<DubbedAnimeNetVideo> videos { get; set; }
                public string image { get; set; }
                public string next_id { get; set; }
                public object previous_id { get; set; }
                public string url { get; set; }
                public string lang { get; set; }
            }
            public struct DubbedAnimeNetEpisodeExternalAPI
            {
                public string host { get; set; }
                public string id { get; set; }
                public string type { get; set; }
            }
            #endregion
            public string Name => "DubbedAnimeNet";

            public void FishMainLink(string year, TempThred tempThred, MALData malData)
            {
                print("GET MAIN LINK FROM " + Name);
                string search = malData.engName;//"neverland";
                string postReq = PostRequest("https://ww5.dubbedanime.net/ajax/paginate", "https://ww5.dubbedanime.net/browse-anime?search=" + search, $"query%5Bsearch%5D={search}&what=query&model=Anime&size=30&letter=all");
                print("DUBBEDANIMEPOST: " + postReq);

                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                try {
                    var _d = JsonConvert.DeserializeObject<DubbedAnimeNetQuickSearch>(postReq);

                    for (int i = 0; i < _d.results.Count; i++) {
                        for (int q = 0; q < activeMovie.title.MALData.seasonData.Count; q++) {
                            for (int z = 0; z < activeMovie.title.MALData.seasonData[q].seasons.Count; z++) {

                                MALSeason ms;
                                lock (AnimeProviderHelper._lock) {
                                    ms = activeMovie.title.MALData.seasonData[q].seasons[z];
                                }

                                string id = FindHTML(ms.malUrl, "/anime/", "/");
                                print("DUBBEDANIMEID:::???" + id + "--||--" + _d.results[i].mal_id);
                                if (id == _d.results[i].mal_id) {

                                    print("SLIGID:: " + _d.results[i].slug);

                                    string d = DownloadString("https://ww5.dubbedanime.net/anime/" + _d.results[i].slug);//anime/the-promised-neverland");

                                    var data = new DubbedAnimeNetData();
                                    int maxEp = 0;
                                    string lookFor = "<li class=\"jt-di dropdown-item\"";

                                    Dictionary<int, DubbedAnimeNetEpisode> dubbedKeys = new Dictionary<int, DubbedAnimeNetEpisode>();

                                    while (d.Contains(lookFor)) {
                                        d = RemoveOne(d, lookFor);
                                        bool isDubbed = FindHTML(d, "data-dubbed=\"", "\"") == "true";
                                        bool isSubbed = FindHTML(d, "data-subbed=\"", "\"") == "true";
                                        string href = FindHTML(d, "<a href=\'", "\'");
                                        int episode = int.Parse(FindHTML(d, ">Episode ", "<"));
                                        if (maxEp < episode) {
                                            maxEp = episode;
                                        }
                                        dubbedKeys.Add(episode, new DubbedAnimeNetEpisode() { dubExists = isDubbed, subExists = isSubbed, href = href });

                                        print(Name + "LOADED:::::::::: " + href + "|" + episode + "|" + isDubbed + "|" + isSubbed);
                                    }

                                    data.EpisodesUrls = new CloudStreamCore.DubbedAnimeNetEpisode[maxEp];
                                    for (int f = 0; f < maxEp; f++) {
                                        data.EpisodesUrls[f] = dubbedKeys[f + 1];
                                    }

                                    lock (AnimeProviderHelper._lock) {
                                        var _data = activeMovie.title.MALData.seasonData[q].seasons[z];
                                        _data.dubbedAnimeNetData = data;
                                        activeMovie.title.MALData.seasonData[q].seasons[z] = _data;
                                    }
                                }
                                //print(md.malUrl)
                            }
                        }
                        // print(_d.results[i].slug + "|" + _d.results[i].mal_id);
                    }
                }
                catch (Exception _ex) {
                    print(Name + " ERROROROOROROOR!! " + _ex);
                }

            }

            public int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred)
            {
                int len = 0;
                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].dubbedAnimeNetData;
                        if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                            //  dstring = ms.baseUrl;
                            foreach (var ep in ms.EpisodesUrls) {
                                if (ep.dubExists && isDub || ep.subExists && !isDub) {
                                    len++;
                                }
                            }
                        }
                    }
                }
                catch (Exception) {
                }
                return len;
            }

            static string GetSlug(int season, int normalEpisode)
            {
                int max = 0;

                for (int q = 0; q < activeMovie.title.MALData.seasonData[season].seasons.Count; q++) {
                    var urls = activeMovie.title.MALData.seasonData[season].seasons[q].dubbedAnimeNetData.EpisodesUrls;
                    if (urls == null) {
                        return "";
                    }
                    max += activeMovie.title.MALData.seasonData[season].seasons[q].dubbedAnimeNetData.EpisodesUrls.Length;
                    print("MAX::: " + max);

                    if (max > normalEpisode) {
                        var ms = activeMovie.title.MALData.seasonData[season].seasons[q];
                        if (ms.dubbedAnimeNetData.EpisodesUrls.Length > normalEpisode) {
                            return "https://ww5.dubbedanime.net" + ms.dubbedAnimeNetData.EpisodesUrls[normalEpisode].href;
                        }
                        //var ms = activeMovie.title.MALData.seasonData[season].seasons[q].animeFlixData;

                    }
                }
                return "";
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred)
            {
                try {
                    string slug = GetSlug(season, normalEpisode);

                    if (slug == "") return;

                    string d = DownloadString(slug);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string xtoken = FindHTML(d, "var xuath = \'", "\'");

                    string cepisode = FindHTML(d, "var episode = ", ";");
                    print("CEPISODE ==== " + cepisode);
                    var epi = JsonConvert.DeserializeObject<DubbedAnimeNetAPIEpisode>(cepisode);
                    for (int i = 0; i < epi.videos.Count; i++) {
                        var vid = epi.videos[i];
                        if ((vid.type == "dubbed" && !isDub) || (vid.type == "subbed" && isDub)) continue;

                        //type == dubbed/subbed
                        //host == mp4upload/trollvid
                        //id = i9w80jgcwbu7
                        // Getmp4UploadByFile()


                        void AddMp4(string id)
                        {
                            string mp4 = ("https://www.mp4upload.com/embed-" + id);
                            string __d = DownloadString(mp4, tempThred);
                            if (!GetThredActive(tempThred)) { return; };
                            string mxLink = Getmp4UploadByFile(__d);
                            AddPotentialLink(normalEpisode, mxLink, "Mp4Upload", 9);
                        }

                        if (vid.host == "trollvid") {
                            string dUrl = "https://mp4.sh/embed/" + vid.id + xtoken;
                            string p = HTMLGet(dUrl, slug);

                            string src = FindHTML(p, "<source src=\"", "\"");
                            AddPotentialLink(normalEpisode, src, "Trollvid", 10);

                            string fetch = FindHTML(p, "fetch(\'", "\'");
                            if (fetch != "") {
                                print("FETCH: " + fetch);
                                string _d = DownloadString(fetch);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                try {
                                    var res = JsonConvert.DeserializeObject<List<DubbedAnimeNetEpisodeExternalAPI>>(_d);
                                    for (int q = 0; q < res.Count; q++) {
                                        if (res[q].host == "mp4upload") {
                                            AddMp4(res[q].id);
                                        }
                                        else if (res[q].host == "vidstreaming") {
                                            string __d = "https://vidstreaming.io/streaming.php?id=" + res[q].id;
                                            AddEpisodesFromMirrors(tempThred, __d, normalEpisode);
                                        }
                                        // print(res[q].host + "|" + res[q].id + "|" + res[q].type);
                                        /*vidstreaming|MTE3NDg5|dubbed
            server hyrax||dubbed
            xstreamcdn||dubbed
            vidcdn|MTE3NDg5|dubbed
            mp4upload|nnh0ejaypnie|dubbed*/
                                    }
                                }
                                catch (Exception _ex) {
                                    print("EX:::: " + _ex);
                                }

                            }
                            print(p);
                        }
                        else if (vid.host == "mp4upload") {
                            AddMp4(vid.id);
                        }

                        print(vid.host + "|" + vid.id + "|" + vid.type);
                    }

                }
                catch (Exception _ex) {
                    print("ERROR IN LOADING DUBBEDANIMENET: " + _ex);
                }
            }
        }

        class DubbedAnimeProvider : IAnimeProvider
        {
            public string Name { get => "DubbedAnime"; }

            public void FishMainLink(string year, TempThred tempThred, MALData malData)
            {
                string _imdb = activeMovie.title.name; //"Attack On Titan";
                string imdb = _imdb.Replace(".", "").Replace("/", "");
                string d = DownloadString("https://bestdubbedanime.com/search/" + imdb);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                const string lookFor = "class=\"resulta\" href=\"";
                string nameLookFor = "<div class=\"titleresults\">";
                print(d);

                List<int> alreadyAdded = new List<int>();
                while (d.Contains(nameLookFor)) {
                    string name = FindHTML(d, nameLookFor, "<", decodeToNonHtml: true);
                    if (name.ToLower().Contains(_imdb.ToLower())) {

                        string url = FindHTML(d, lookFor, "\"").Replace("\\/", "/");
                        string slug = url.Replace("//bestdubbedanime.com/", "");

                        int season = 0;
                        if (name.ToLower().Contains("2nd season")) {
                            season = 2;
                        }
                        else if (name.ToLower().Contains("3rd season")) {
                            season = 3;
                        }
                        if (season == 0) {
                            for (int i = 1; i < 7; i++) {
                                if (name.EndsWith(" " + i)) {
                                    season = i;
                                }
                            }
                        }
                        if (season == 0) {
                            season = 1;
                        }
                        int part = 1;
                        for (int i = 2; i < 5; i++) {
                            if (name.ToLower().Contains("part " + i)) {
                                part = i;
                            }
                        }


                        int id = season + part * 1000;
                        if (!alreadyAdded.Contains(id)) {
                            alreadyAdded.Add(id);
                            try {
                                print("SEASON::" + season + "PART" + part);
                                lock (AnimeProviderHelper._lock) {
                                    var ms = activeMovie.title.MALData.seasonData[season].seasons[part - 1];
                                    ms.dubbedAnimeData.dubExists = true;
                                    ms.dubbedAnimeData.slug = slug;
                                    activeMovie.title.MALData.seasonData[season].seasons[part - 1] = ms;

                                    print("ÖÖ>>");
                                    print(activeMovie.title.MALData.seasonData[season].seasons[part - 1].dubbedAnimeData.dubExists);
                                }
                            }
                            catch (Exception) {
                                print("ERROR IN " + "SEASON::" + season + "PART" + part);
                                //throw;
                                // ERROR
                            }
                        }

                        print("-->" + name + "|" + url + "| Season " + season + "|" + slug + "|Park" + part);

                        //print("Season " + season + "||" + slug);
                    }
                    d = RemoveOne(d, nameLookFor);
                }
            }

            public List<string> GetAllLinks(Movie currentMovie, int currentSeason, bool isDub)
            {
                if (!isDub) return new List<string>();

                List<string> baseUrls = new List<string>();

                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].dubbedAnimeData;

                        if (ms.dubExists) {
                            if (!baseUrls.Contains(ms.slug)) {
                                baseUrls.Add(ms.slug);
                            }
                            //print("BASEURL " + ms.baseUrl);
                        }
                    }
                }
                catch (Exception) {
                    //  throw;
                }
                return baseUrls;
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred)
            {
                if (!isDub) return;

                /*   TempThred tempthread = new TempThred();
                   tempthread.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                   tempthread.Thread = new System.Threading.Thread(() => {
                       try {*/
                print("DUBBED::" + episode + "|" + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum());
                if (episode <= activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum()) {
                    List<string> fwords = GetAllLinks(activeMovie, season, isDub);
                    print("SLUG1." + fwords[0]);
                    int sel = -1;
                    int floor = 0;
                    int subtract = 0;
                    if (activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason != null) {
                        for (int i = 0; i < activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Count; i++) {
                            int seling = floor + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];

                            if (episode > floor && episode <= seling) {
                                sel = i;
                                subtract = floor;

                            }
                            //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                            floor += activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];
                        }
                    }

                    string fwordLink = fwords[sel];
                    print("SLUGOS: " + fwordLink);
                    DubbedAnimeEpisode dubbedEp = GetDubbedAnimeEpisode(fwordLink, episode - subtract);

                    string serverUrls = dubbedEp.serversHTML;
                    print("SERVERURLLRL:" + serverUrls);
                    const string sLookFor = "hl=\"";
                    while (serverUrls.Contains(sLookFor)) {
                        string baseUrl = FindHTML(dubbedEp.serversHTML, "hl=\"", "\"");
                        print("BASE::" + baseUrl);
                        string burl = "https://bestdubbedanime.com/xz/api/playeri.php?url=" + baseUrl + "&_=" + UnixTime;
                        print(burl);
                        string _d = DownloadString(burl);
                        print("SSC:" + _d);
                        int prio = -10; // SOME LINKS ARE EXPIRED, CAUSING VLC TO EXIT

                        string enlink = "\'";
                        if (_d.Contains("<source src=\"")) {
                            enlink = "\"";
                        }
                        string lookFor = "<source src=" + enlink;
                        while (_d.Contains(lookFor)) {
                            string vUrl = FindHTML(_d, lookFor, enlink);
                            if (vUrl != "") {
                                vUrl = "https:" + vUrl;
                            }
                            string label = FindHTML(_d, "label=" + enlink, enlink);
                            print("DUBBEDANIMECHECK:" + vUrl + "|" + label);
                            //if (GetFileSize(vUrl) > 0) {
                            AddPotentialLink(normalEpisode, vUrl, "DubbedAnime " + label.Replace("0p", "0") + "p", prio);
                            //}

                            _d = RemoveOne(_d, lookFor);
                            _d = RemoveOne(_d, "label=" + enlink);
                        }
                        serverUrls = RemoveOne(serverUrls, sLookFor);
                    }
                }
                /*   }
                   finally {
                       JoinThred(tempthread);
                   }
               });
               tempthread.Thread.Name = "DubAnime Thread";
               tempthread.Thread.Start();*/
            }

            public int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred)
            {
                if (isDub) {
                    //  activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason = new List<int>();
                    List<int> dubbedSum = new List<int>();
                    List<string> dubbedAnimeLinks = GetAllLinks(currentMovie, currentSeason, isDub);
                    if (tempThred != null) {
                        if (!GetThredActive((TempThred)tempThred)) { return 0; }; // COPY UPDATE PROGRESS
                    }
                    for (int i = 0; i < dubbedAnimeLinks.Count; i++) {
                        print("LINKOS:" + dubbedAnimeLinks[i]);
                        DubbedAnimeEpisode ep = GetDubbedAnimeEpisode(dubbedAnimeLinks[i], 1);
                        print("EPOS:" + ep.totalEp);
                        dubbedSum.Add(ep.totalEp);
                        // activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Add(ep.totalEp);
                    }
                    activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason = dubbedSum;

                    return dubbedSum.Sum();
                }
                return 0;
            }
        }



        public struct AnimeFlixSearchItem
        {
            public int id { get; set; }
            public int dynamic_id { get; set; }
            public string title { get; set; }
            public string english_title { get; set; }
            public string slug { get; set; }
            public string status { get; set; }
            public string description { get; set; }
            public string year { get; set; }
            public string season { get; set; }
            public string type { get; set; }
            public string cover_photo { get; set; }
            public List<string> alternate_titles { get; set; }
            public string duration { get; set; }
            public string broadcast_day { get; set; }
            public string broadcast_time { get; set; }
            public string rating { get; set; }
            public double? rating_scores { get; set; }
            public double gwa_rating { get; set; }
        }

        public struct AnimeFlixQuickSearch
        {
            public List<AnimeFlixSearchItem> data { get; set; }
        }


        public struct AnimeFlixAnimeEpisode
        {
            public int id { get; set; }
            public int dynamic_id { get; set; }
            public string title { get; set; }
            public string episode_num { get; set; }
            public string airing_date { get; set; }
            public int views { get; set; }
            public int sub { get; set; }
            public int dub { get; set; }
            public string thumbnail { get; set; }
        }

        public struct AnimeFlixAnimeLink
        {
            public string first { get; set; }
            public string last { get; set; }
            public object prev { get; set; }
            public string next { get; set; }
        }

        public struct AnimeFlixAnimeMetaData
        {
            public int current_page { get; set; }
            public int from { get; set; }
            public int last_page { get; set; }
            public string path { get; set; }
            public int per_page { get; set; }
            public int to { get; set; }
            public int total { get; set; }
        }

        public struct AnimeFlixAnimeData
        {
            public int id { get; set; }
            public int dynamic_id { get; set; }
            public string title { get; set; }
            public string english_title { get; set; }
            public string slug { get; set; }
            public string status { get; set; }
            public string description { get; set; }
            public string year { get; set; }
            public string season { get; set; }
            public string type { get; set; }
            public string cover_photo { get; set; }
            public List<string> alternate_titles { get; set; }
            public string duration { get; set; }
            public string broadcast_day { get; set; }
            public string broadcast_time { get; set; }
            public string rating { get; set; }
            public double rating_scores { get; set; }
            public double gwa_rating { get; set; }
        }

        public struct AnimeFlixAnimeSeason
        {
            public List<AnimeFlixAnimeEpisode> data { get; set; }
            public AnimeFlixAnimeLink links { get; set; }
            public AnimeFlixAnimeMetaData meta { get; set; }
            public AnimeFlixAnimeData anime { get; set; }
        }

        public struct AnimeFlixRawEpisode
        {
            public string id { get; set; }
            public string provider { get; set; }
            public string file { get; set; }
            public string lang { get; set; }
            public string type { get; set; }
            public bool hardsub { get; set; }
            public string thumbnail { get; set; }
            public string resolution { get; set; }
        }


        class AnimeFlixProvider : IAnimeProvider
        {
            public string Name { get => "AnimeFlix"; }

            public void FishMainLink(string year, TempThred tempThred, MALData malData)
            {
                try {


                    string result = DownloadString("https://animeflix.io/api/search?q=" + malData.firstName, waitTime: 400, repeats: 1);//activeMovie.title.name);
                    print("FLIX::::" + result);
                    if (result == "") return;
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    var res = JsonConvert.DeserializeObject<AnimeFlixQuickSearch>(result);
                    var data = res.data;
                    List<int> alreadyAdded = new List<int>();

                    for (int i = 0; i < data.Count; i++) {
                        var d = data[i];
                        List<string> names = new List<string>() { d.english_title, d.title };
                        if (d.alternate_titles != null) {
                            names.AddRange(d.alternate_titles);
                        }
                        GetSeasonAndPartFromName(d.title, out int season, out int part);
                        print("ID::::SEASON:" + season + "|" + part + "|" + names[0]);

                        int id = season + part * 1000;
                        if (!alreadyAdded.Contains(id)) {
                            for (int q = 0; q < names.Count; q++) {
                                if (names[q].ToLower().Contains(malData.firstName.ToLower()) || names[q].ToLower().Contains(activeMovie.title.name.ToLower())) {
                                    print("NAMES:::da" + d.title);
                                    alreadyAdded.Add(id);
                                    try {

                                        MALSeason ms;
                                        lock (AnimeProviderHelper._lock) {
                                            ms = activeMovie.title.MALData.seasonData[season].seasons[part - 1];
                                        }

                                        string url = "https://animeflix.io/api/episodes?anime_id=" + d.id + "&limit=50&sort=DESC";
                                        print("DURL:::==" + url);
                                        string dres = DownloadString(url, repeats: 1, waitTime: 50);
                                        print("DRES:::" + dres);
                                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                        var seasonData = JsonConvert.DeserializeObject<AnimeFlixAnimeSeason>(dres);
                                        for (int z = 0; z < seasonData.meta.last_page - 1; z++) {
                                            string _url = "https://animeflix.io/api/episodes?anime_id=" + d.id + "&limit=50" + "&page=" + (i + 2) + "&sort=DESC";
                                            print("DURL:::==" + url);
                                            string _dres = DownloadString(url, repeats: 1, waitTime: 50);
                                            var _seasonData = JsonConvert.DeserializeObject<AnimeFlixAnimeSeason>(dres);

                                            seasonData.data.AddRange(_seasonData.data);
                                        }


                                        bool hasDub = false, hasSub = false;

                                        AnimeFlixEpisode[] animeFlixEpisodes = new AnimeFlixEpisode[seasonData.data.Count];
                                        for (int s = 0; s < seasonData.data.Count; s++) {
                                            var _data = seasonData.data[s];
                                            bool dubEx = _data.dub == 1;
                                            bool subEx = _data.sub == 1;

                                            if (subEx) {
                                                hasSub = true;
                                            }
                                            if (dubEx) {
                                                hasDub = true;
                                            }
                                            print("ADDED EP::: " + _data.id + "|" + subEx + "|" + dubEx + "|");
                                            animeFlixEpisodes[int.Parse(_data.episode_num) - 1] = new AnimeFlixEpisode() { id = _data.id, dubExists = dubEx, subExists = subEx };
                                        }

                                        AnimeFlixData flixData = new AnimeFlixData() {
                                            dubExists = hasDub,
                                            subExists = hasSub,
                                            EpisodesUrls = animeFlixEpisodes,
                                        };

                                        ms.animeFlixData = flixData;

                                        lock (AnimeProviderHelper._lock) {
                                            activeMovie.title.MALData.seasonData[season].seasons[part - 1] = ms;
                                        }
                                    }
                                    catch (Exception) {

                                    }
                                    break;

                                }
                            }
                        }
                    }
                }
                catch (Exception _ex) {
                    print("Error====" + _ex);
                }
            }

            public int GetLinkCount(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred)
            {
                int len = 0;
                try {
                    for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                        var ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q].animeFlixData;
                        if ((ms.dubExists && isDub) || (ms.subExists && !isDub)) {
                            //  dstring = ms.baseUrl;
                            foreach (var ep in ms.EpisodesUrls) {
                                if (ep.dubExists && isDub || ep.subExists && !isDub) {
                                    len++;
                                }
                            }
                        }
                    }
                }
                catch (Exception) {
                }
                return len;
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isDub, TempThred tempThred)
            {
                /*
                TempThred tempThred = new TempThred();
                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                int max = 0;
                for (int q = 0; q < activeMovie.title.MALData.seasonData[season].seasons.Count; q++) {
                    var urls = activeMovie.title.MALData.seasonData[season].seasons[q].animeFlixData.EpisodesUrls;
                    if (urls == null) {
                        return;
                    }
                    max += activeMovie.title.MALData.seasonData[season].seasons[q].animeFlixData.EpisodesUrls.Length;
                    print("MAX::: " + max);

                    if (max > normalEpisode) {
                        var ms = activeMovie.title.MALData.seasonData[season].seasons[q];
                        if (ms.animeFlixData.EpisodesUrls.Length > normalEpisode) {
                            int id = ms.animeFlixData.EpisodesUrls[normalEpisode].id;

                            print("DLOAD:===" + id);
                            string main = DownloadString("https://animeflix.io/api/videos?episode_id=" + id);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            var epData = JsonConvert.DeserializeObject<List<AnimeFlixRawEpisode>>(main);

                            for (int i = 0; i < epData.Count; i++) {
                                if ((epData[i].lang == "dub" && isDub) || (epData[i].lang == "sub" && !isDub)) {
                                    AddPotentialLink(normalEpisode, epData[i].file, "Animeflix " + epData[i].provider + " " + epData[i].resolution, 10);
                                }
                            }
                            return;
                        }
                        //var ms = activeMovie.title.MALData.seasonData[season].seasons[q].animeFlixData;

                    }
                }

                /* }
                 finally {
                     JoinThred(tempThred);
                 }
             });
             tempThred.Thread.Name = "AnimeFlixThread";
             tempThred.Thread.Start();*/



            }

        }
        #endregion

        #region =================================================== MOVIE PROVIDERS ===================================================

        class WatchTVProvider : IMovieProvider
        {
            public void FishMainLinkTSync() { }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                /*  if (isMovie) return;

                  TempThred tempThred = new TempThred();
                  tempThred.typeId = 1; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                  tempThred.Thread = new System.Threading.Thread(() => {
                      try {*/
                string url = "https://www.tvseries.video/series/" + ToDown(activeMovie.title.name, replaceSpace: "-") + "/" + "season-" + season + "-episode-" + episode;

                string d = DownloadString(url);
                string vidId = FindHTML(d, " data-vid=\"", "\"");
                if (vidId != "") {
                    d = DownloadString("https://www.tvseries.video" + vidId);
                    AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                }
                /*  }
                  finally {
                      JoinThred(tempThred);
                  }
              });
              tempThred.Thread.Name = "FishWatch";
              tempThred.Thread.Start();*/
            }
        }

        class LiveMovies123Provider : IMovieProvider
        {
            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://movies123.live", tempThred);
                GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://c123movies.com", tempThred);
            }
            static void GetLiveMovies123Links(int normalEpisode, int episode, int season, bool isMovie, string provider = "https://c123movies.com", TempThred tempThred = default) // https://movies123.live & https://c123movies.com
            {
                /*
                TempThred tempThred = new TempThred();

                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string _title = ToDown(activeMovie.title.name, replaceSpace: "-");

                string _url = (isMovie ? (provider + "/movies/" + _title) : (provider + "/episodes/" + _title + "-season-" + season + "-episode-" + episode));

                string d = DownloadString(_url);
                if (!GetThredActive(tempThred)) { return; };
                string release = FindHTML(d, "Release:</strong> ", "<");
                bool succ = true;
                if (release != activeMovie.title.ogYear) {
                    succ = false;
                    if (isMovie) {
                        d = DownloadString(_url + "-1");
                        succ = true;
                    }
                }
                if (succ) {
                    string live = FindHTML(d, "getlink(\'", "\'");
                    if (live != "") {
                        string url = provider + "/ajax/get-link.php?id=" + live + "&type=" + (isMovie ? "movie" : "tv") + "&link=sw&" + (isMovie ? "season=undefined&episode=undefined" : ("season=" + season + "&episode=" + episode));
                        d = DownloadString(url); if (!GetThredActive(tempThred)) { return; };

                        string shortURL = FindHTML(d, "iframe src=\\\"", "\"").Replace("\\/", "/");
                        d = DownloadString(shortURL); if (!GetThredActive(tempThred)) { return; };

                        AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                    }
                }
                /*  }
                  finally {
                      JoinThred(tempThred);
                  }
              });
              tempThred.Thread.Name = "GetLiveMovies123Links";
              tempThred.Thread.Start();*/
            }

            public void FishMainLinkTSync()
            {
            }
        }

        class Movies123Provider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {
                TempThred tempThred = new TempThred();
                tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        if (activeMovie.title.movieType == MovieType.Anime) { return; }

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                        // string yesmovies = "https://yesmoviess.to/search/?keyword=" + rinput.Replace("+", "-");

                        // SUB HD MOVIES 123
                        string movies123 = "https://movies123.pro/search/" + rinput.Replace("+", "%20") + ((activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie) ? "/movies" : "/series");

                        string d = DownloadString(movies123, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        int counter = 0; // NOT TO GET STUCK, JUST IN CASE

                        List<Movies123SeasonData> seasonData = new List<Movies123SeasonData>();

                        while ((d.Contains("/movie/") || d.Contains("/tv-series/")) && counter < 100) {
                            counter++;

                            /*
                            data - filmName = "Iron Man"
                        data - year = "2008"
                        data - imdb = "IMDb: 7.9"
                        data - duration = "126 min"
                        data - country = "United States"
                        data - genre = "Action, Adventure, Sci-Fi"
                        data - descript = "Tony a boss of a Technology group, after his encounter in Afghanistan, became a symbol of justice as he built High-Tech armors and suits, to act as..."
                        data - star_prefix = ""
                        data - key = "0"
                        data - quality = "itemAbsolute_hd"
                        data - rating = "4.75"
                                */

                            // --------- GET TYPE ---------

                            int tvIndex = d.IndexOf("/tv-series/");
                            int movieIndex = d.IndexOf("/movie/");
                            bool isMovie = movieIndex < tvIndex;
                            if (tvIndex == -1) { isMovie = true; }
                            if (movieIndex == -1) { isMovie = false; }

                            Movies123 movie123 = new Movies123();

                            // --------- GET CROSSREFRENCE DATA ---------

                            movie123.year = ReadDataMovie(d, "data-year");
                            movie123.imdbRating = ReadDataMovie(d, "data-imdb").ToLower().Replace(" ", "").Replace("imdb:", "");
                            movie123.runtime = ReadDataMovie(d, "data-duration").Replace(" ", "");
                            movie123.genre = ReadDataMovie(d, "data-genre");
                            movie123.plot = ReadDataMovie(d, "data-descript");
                            movie123.type = isMovie ? MovieType.Movie : MovieType.TVSeries; //  "movie" : "tv-series";

                            string lookfor = isMovie ? "/movie/" : "/tv-series/";

                            // --------- GET FWORLDLINK, FORWARLINK ---------

                            int mStart = d.IndexOf(lookfor);
                            if (mStart == -1) {
                                debug("API ERROR!");
                                // print(mD);
                                debug(movie123.year + "|" + movie123.imdbRating + "|" + isMovie + "|" + lookfor);
                                continue;
                            }
                            d = d.Substring(mStart, d.Length - mStart);
                            d = d.Substring(7, d.Length - 7);
                            //string bMd = RemoveOne(mD, "<img src=\"/dist/image/default_poster.jpg\"");
                            movie123.posterUrl = ReadDataMovie(d, "<img src=\"/dist/image/default_poster.jpg\" data-src");




                            string rmd = lookfor + d;
                            //string realAPILink = mD.Substring(0, mD.IndexOf("-"));
                            string fwordLink = "https://movies123.pro" + rmd.Substring(0, rmd.IndexOf("\""));
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            if (!isMovie) {
                                fwordLink = rmd.Substring(0, rmd.IndexOf("\"")); // /tv-series/ies/the-orville-season-2/gMSTqyRs
                                fwordLink = fwordLink.Substring(11, fwordLink.Length - 11); //ies/the-orville-season-2/gMSTqyRs
                                string found = fwordLink.Substring(0, fwordLink.IndexOf("/"));
                                if (!found.Contains("-")) {
                                    fwordLink = fwordLink.Replace(found, ""); //the-orville-season-2/gMSTqyRs
                                }
                                fwordLink = "https://movies123.pro" + "/tv-series" + fwordLink;
                            }

                            // --------- GET NAME ECT ---------
                            //if (false) {
                            int titleStart = d.IndexOf("title=\"");
                            string movieName = d.Substring(titleStart + 7, d.Length - titleStart - 7);
                            movieName = movieName.Substring(0, movieName.IndexOf("\""));
                            movieName = movieName.Replace("&amp;", "and");
                            movie123.name = movieName;
                            //}

                            if ((isMovie && canMovie) || (!isMovie && canShow)) {
                                //FWORDLINK HERE
                                //   print(activeMovie.title.name + "||||" + movie123.name + " : " + activeMovie.title.rating + " : " + movie123.imdbRating + " : " + activeMovie.title.movieType + " : " + movie123.type + " : " + activeMovie.title.runtime + " : " + movie123.runtime);

                                // GET RATING IN INT (10-100)
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                                string s1 = activeMovie.title.rating;
                                string s2 = movie123.imdbRating;
                                if (s2.ToLower() == "n/a") {
                                    continue;
                                }

                                if (!s1.Contains(".")) { s1 += ".0"; }
                                if (!s2.Contains(".")) { s2 += ".0"; }

                                int i1 = int.Parse(s1.Replace(".", ""));
                                int i2 = int.Parse(s2.Replace(".", ""));

                                if ((i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1) && activeMovie.title.movieType == movie123.type && movie123.name.ToLower().Contains(activeMovie.title.name.ToLower())) { // --- THE SAME ---
                                                                                                                                                                                                        // counter = 10000;
                                                                                                                                                                                                        //print("FWORDLINK: " + fwordLink);
                                    if (activeMovie.title.movieType == MovieType.TVSeries) {
                                        //<a data-ep-id="
                                        string _d = DownloadString(fwordLink, tempThred);
                                        const string _lookFor = "<a data-ep-id=\"";
                                        //print(_d);
                                        List<string> sData = new List<string>();
                                        while (_d.Contains(_lookFor)) {
                                            string rLink = FindHTML(_d, _lookFor, "\"");
                                            //   print("RLINK: " + rLink);
                                            sData.Add(rLink + "-watch-free.html");
                                            _d = RemoveOne(_d, _lookFor);
                                        }
                                        seasonData.Add(new Movies123SeasonData() { seasonUrl = fwordLink, episodeUrls = sData });
                                    }
                                    else {
                                        activeMovie.title.movies123MetaData = new Movies123MetaData() { movieLink = fwordLink, seasonData = new List<Movies123SeasonData>() };
                                    }

                                }
                            }
                        }

                        seasonData.Reverse();
                        if (MovieType.TVSeries == activeMovie.title.movieType) {
                            Title t = activeMovie.title;
                            activeMovie.title = new Title() {
                                description = t.description,
                                MALData = t.MALData,
                                genres = t.genres,
                                id = t.id,
                                movieType = t.movieType,
                                name = t.name,
                                ogName = t.ogName,
                                posterUrl = t.posterUrl,
                                rating = t.rating,
                                recomended = t.recomended,
                                runtime = t.runtime,
                                seasons = t.seasons,
                                trailers = t.trailers,
                                year = t.year,
                                hdPosterUrl = t.hdPosterUrl,
                                shortEpView = t.shortEpView,
                                fmoviesMetaData = t.fmoviesMetaData,
                                movies123MetaData = new Movies123MetaData() { movieLink = "", seasonData = seasonData },
                                yesmoviessSeasonDatas = t.yesmoviessSeasonDatas,
                                watchSeriesHdMetaData = t.watchSeriesHdMetaData,
                            };
                        }

                        movie123FishingDone?.Invoke(null, activeMovie);
                        fishingDone?.Invoke(null, activeMovie);

                        // MonitorFunc(() => print(">>>" + activeMovie.title.movies123MetaData.seasonData.Count),0);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "Movies123MetaData";
                tempThred.Thread.Start();
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                /*
                TempThred tempThred = new TempThred();
                tempThred.typeId = 1; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                if (activeMovie.title.movies123MetaData.movieLink != null) {
                    if (activeMovie.title.movieType == MovieType.TVSeries) {
                        int normalSeason = season - 1;
                        List<Movies123SeasonData> seasonData = activeMovie.title.movies123MetaData.seasonData;
                        // ---- TO PREVENT ERRORS START ----
                        if (seasonData != null) {
                            if (seasonData.Count > normalSeason) {
                                if (seasonData[normalSeason].episodeUrls != null) {
                                    if (seasonData[normalSeason].episodeUrls.Count > normalEpisode) {
                                        // ---- END ----
                                        string fwordLink = seasonData[normalSeason].seasonUrl + "/" + seasonData[normalSeason].episodeUrls[normalEpisode];
                                        print(fwordLink);
                                        for (int f = 0; f < MIRROR_COUNT; f++) {
                                            GetLinkServer(f, fwordLink, normalEpisode);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else {
                        for (int f = 0; f < MIRROR_COUNT; f++) {
                            print(">::" + f);
                            GetLinkServer(f, activeMovie.title.movies123MetaData.movieLink); // JUST GET THE MOVIE
                        }
                    }
                }
                /*}
                 finally {
                     JoinThred(tempThred);
                 }
             });
             tempThred.Thread.Name = "QuickSearch";
             tempThred.Thread.Start();*/
            }

            /// <summary>
            /// GET LOWHD MIRROR SERVER USED BY MOVIES123 AND PLACE THEM IN ACTIVEMOVIE
            /// </summary>
            /// <param name="f"></param>
            /// <param name="realMoveLink"></param>
            /// <param name="tempThred"></param>
            /// <param name="episode"></param>
            public static void GetLinkServer(int f, string realMoveLink, int episode = 0)
            {
                TempThred minorTempThred = new TempThred();
                minorTempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                minorTempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        //  int unixTimestamp = ;

                        string jsn = GetWebRequest(realMoveLink + "?server=server_" + f + "&_=" + UnixTime);

                        if (!GetThredActive(minorTempThred)) { return; };  // ---- THREAD CANCELLED ----

                        while (jsn.Contains("http")) {
                            int _start = jsn.IndexOf("http");
                            jsn = jsn.Substring(_start, jsn.Length - _start);
                            int id = jsn.IndexOf("\"");
                            if (id != -1) {
                                string newM = jsn.Substring(0, id);
                                newM = newM.Replace("\\", "");
                                print("::>" + newM);
                                AddPotentialLink(episode, newM, "Mirror [MIRRORCOUNTER]", 0);
                            }
                            jsn = jsn.Substring(4, jsn.Length - 4);
                        }
                    }
                    finally {
                        JoinThred(minorTempThred);
                    }
                });
                minorTempThred.Thread.Name = "Mirror Thread";
                minorTempThred.Thread.Start();
            }
        }

        class FullMoviesProvider : IMovieProvider
        {
            public void FishMainLinkTSync() { }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                if (!isMovie) return;

                // freefullmovies
                /* TempThred tempThred = new TempThred();
                 tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                 tempThred.Thread = new System.Threading.Thread(() => {
                     try {*/
                string d = DownloadString("https://www.freefullmovies.zone/movies/watch." + ToDown(activeMovie.title.name, true, "-").Replace(" ", "-") + "-" + activeMovie.title.year.Substring(0, 4) + ".movie.html", tempThred);

                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                string find = "<source src=\"";
                string link = FindHTML(d, find, "\"");
                if (link != "") {
                    double dSize = GetFileSize(link);
                    if (dSize > 100) { // TO REMOVE TRAILERS
                        AddPotentialLink(episode, link, "HD FullMovies", 13);
                    }
                }
                /*}
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "FullMovies";
            tempThred.Thread.Start();*/

                // 1movietv
                /*
                TempThred _tempThred = new TempThred();
                _tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                _tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string __d = DownloadString("https://1movietv.com/playstream/" + activeMovie.title.id, tempThred);
                GetMovieTv(episode, __d, tempThred);
                /* }
                 finally {
                     JoinThred(_tempThred);
                 }
             });
             _tempThred.Thread.Name = "Movietv";
             _tempThred.Thread.Start();*/
            }
        }

        class TheMovies123Provider : IMovieProvider
        {
            public void FishMainLinkTSync() { }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                /*
                TempThred tempThred = new TempThred();

                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string extra = ToDown(activeMovie.title.name, true, "-") + (isMovie ? ("-" + activeMovie.title.ogYear) : ("-" + season + "x" + episode));
                string d = DownloadString("https://on.the123movies.eu/" + extra);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                string ts = FindHTML(d, "data-vs=\"", "\"");
                print("DATATS::" + ts);
                d = DownloadString(ts);
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                /* }
                 finally {
                     JoinThred(tempThred);
                 }
             });
             tempThred.Thread.Name = "GetThe123movies Thread";
             tempThred.Thread.Start();*/
            }
        }

        class GomoStreamProvider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {

            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                /*
                TempThred minorTempThred = new TempThred();
                minorTempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                minorTempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string find = activeMovie.title.name.ToLower() + (activeMovie.title.movieType == MovieType.TVSeries ? "-season-" + season : "");
                find = find.Replace("\'", "-");
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                find = rgx.Replace(find, "");

                find = find.Replace(" - ", "-").Replace(" ", "-");

                if (activeMovie.title.movieType == MovieType.TVSeries) { // ADD CORRECT FORMAT; https://gomostream.com/show/game-of-thrones/01-01
                    find = find.Replace("-season-", "/");

                    for (int i = 0; i < 10; i++) {
                        if (find.EndsWith("/" + i)) {
                            find = find.Replace("/" + i, "/0" + i);
                        }
                    }
                    if (episode.ToString() != "-1") {
                        find += "-" + episode;
                    }
                    for (int i = 0; i < 10; i++) {
                        if (find.EndsWith("-" + i)) {
                            find = find.Replace("-" + i, "-0" + i);
                        }
                    }
                }
                string gomoUrl = "https://" + GOMOURL + "/" + ((activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie) ? "movie" : "show") + "/" + find;
                print("GOMOURL==: " + gomoUrl);
                DownloadGomoSteam(gomoUrl, tempThred, normalEpisode);
                /*  }
                  finally {
                      JoinThred(minorTempThred);
                  }
              });
              minorTempThred.Thread.Name = "Mirror Thread";
              minorTempThred.Thread.Start();*/
            }
            /// <summary>
            /// GET GOMOSTEAM SITE MIRRORS
            /// </summary>
            /// <param name="url"></param>
            /// <param name="_tempThred"></param>
            /// <param name="episode"></param>
            static void DownloadGomoSteam(string url, TempThred tempThred, int episode)
            {
                bool done = true;

                try {
                    try {
                        string d = "";
                        if (d == "") {
                            try {
                                d = DownloadString(url, tempThred, false, 2); if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            }
                            catch (System.Exception) {
                                debug("Error gogo");
                            }
                        }

                        if (d == "") {
                            print("GetHTML SAVE");
                            d = GetHTML(url);
                            if (!GetThredActive(tempThred)) { return; };
                        }

                        if (d == "") {
                            d = HTMLGet(url, "https://" + GOMOURL);
                            if (d != "") {
                                print("HTMLGET SAVE");
                            }
                            if (!GetThredActive(tempThred)) { return; };
                        }

                        if (d != "") { // If not failed to connect
                            debug("Passed gogo download site");

                            // ----- JS EMULATION, CHECK USED BY WEBSITE TO STOP WEB SCRAPE BOTS, DID NOT STOP ME >:) -----

                            string tokenCode = FindHTML(d, "var tc = \'", "'");
                            string _token = FindHTML(d, "_token\": \"", "\"");
                            string funct = "function _tsd_tsd_ds(" + FindHTML(d, "function _tsd_tsd_ds(", "</script>").Replace("\"", "'") + " log(_tsd_tsd_ds('" + tokenCode + "'))";
                            // print(funct);
                            if (funct == "function _tsd_tsd_ds( log(_tsd_tsd_ds(''))") {
                                debug(d); // ERROR IN LOADING JS
                            }
                            string realXToken = "";
                            var engine = new Engine()
                            .SetValue("log", new Action<string>((a) => { realXToken = a; }));

                            engine.Execute(@funct);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                                                         //GetAPI(realXToken, tokenCode, _token, tempThred, episode);
                            print("PAssed js test" + realXToken);
                            System.Uri myUri = new System.Uri("https://" + GOMOURL + "/decoding_v3.php"); // Can't DownloadString because of RequestHeaders (Anti-bot)
                            HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

                            // --- Headers ---

                            webRequest.Method = "POST";
                            webRequest.Headers.Add("x-token", realXToken);
                            webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                            webRequest.Headers.Add("DNT", "1");
                            webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
                            webRequest.Headers.Add("TE", "Trailers");
                            webRequest.Headers.Add("Pragma", "Trailers");
                            webRequest.ContentType = "application/x-www-form-urlencoded";
                            done = false;
                            print("Passed token");

                            webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
                                HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
                                Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

                                string requestBody = true ? ("tokenCode=" + tokenCode + "&_token=" + _token) : "type=epis&xny=hnk&id=" + tokenCode; // --- RequestHeaders ---

                                byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

                                postStream.Write(byteArray, 0, byteArray.Length);
                                postStream.Close();
                                print("PASSED TOKENPOST");

                                if (!GetThredActive(tempThred)) { return; };


                                // BEGIN RESPONSE

                                _webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
                                    HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
                                    HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
                                    using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                                        if (!GetThredActive(tempThred)) { print(":("); return; };
                                        print("GOT RESPONSE:");
                                        string result = httpWebStreamReader.ReadToEnd();
                                        print("RESULT:::" + result);
                                        try {
                                            if (result != "") {

                                                // --------------- GOT RESULT!!!!! ---------------

                                                WebClient client = new WebClient();

                                                // --------------- MIRROR LINKS ---------------
                                                string veryURL = FindHTML(result, "https:\\/\\/verystream.com\\/e\\/", "\"");
                                                string gunURL = "https://gounlimited.to/" + FindHTML(result, "https:\\/\\/gounlimited.to\\/", ".html") + ".html";
                                                string onlyURL = "https://onlystream.tv" + FindHTML(result, "https:\\/\\/onlystream.tv", "\"").Replace("\\", "");
                                                //string gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
                                                string upstream = FindHTML(result, "https:\\/\\/upstream.to\\/embed-", "\"");
                                                string mightyupload = FindHTML(result, "http:\\/\\/mightyupload.com\\/", "\"").Replace("\\/", "/");
                                                //["https:\/\/upstream.to\/embed-05mzggpp3ohg.html","https:\/\/gomo.to\/vid\/eyJ0eXBlIjoidHYiLCJzIjoiMDEiLCJlIjoiMDEiLCJpbWQiOiJ0dDA5NDQ5NDciLCJfIjoiMzQyMDk0MzQzMzE4NTEzNzY0IiwidG9rZW4iOiI2NjQ0MzkifQ,,&noneemb","https:\/\/hqq.tv\/player\/embed_player.php?vid=SGVsWVI5aUNlVTZxTTdTV09RY0x6UT09&autoplay=no",""]

                                                if (upstream != "") {
                                                    string _d = DownloadString("https://upstream.to/embed-" + upstream);
                                                    if (!GetThredActive(tempThred)) { return; };
                                                    const string lookFor = "file:\"";
                                                    int prio = 16;
                                                    while (_d.Contains(lookFor)) {
                                                        prio--;
                                                        string ur = FindHTML(_d, lookFor, "\"");
                                                        _d = RemoveOne(_d, lookFor);
                                                        string label = FindHTML(_d, "label:\"", "\"");
                                                        AddPotentialLink(episode, ur, "HD Upstream " + label, prio);
                                                    }
                                                }

                                                if (mightyupload != "") {
                                                    print("MIGHT" + mightyupload);
                                                    string baseUri = "http://mightyupload.com/" + mightyupload;
                                                    //string _d = DownloadString("http://mightyupload.com/" + mightyupload);
                                                    string post = "op=download1&usr_login=&id=" + FindHTML("|" + mightyupload, "|", "/") + "&fname=" + FindHTML(mightyupload, "/", ".html") + "&referer=&method_free=Free+Download+%3E%3E";

                                                    string _d = PostRequest(baseUri, baseUri, post, tempThred);//op=download1&usr_login=&id=k9on84m2bvr9&fname=tt0371746_play.mp4&referer=&method_free=Free+Download+%3E%3E
                                                    print("RESMIGHT:" + _d);
                                                    if (!GetThredActive(tempThred)) { return; };
                                                    string ur = FindHTML(_d, "<source src=\"", "\"");
                                                    AddPotentialLink(episode, ur, "HD MightyUpload", 16);
                                                }
                                                /*
                                                if (gogoStream.EndsWith(",&noneemb")) {
                                                    result = RemoveOne(result, ",&noneemb");
                                                    gogoStream = FindHTML(result, "https:\\/\\/" + GOMOURL, "\"");
                                                }


                                                gogoStream = gogoStream.Replace(",,&noneemb", "").Replace("\\", "");

                                                */

                                                Episode ep = activeMovie.episodes[episode];
                                                if (ep.links == null) {
                                                    activeMovie.episodes[episode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                                                }

                                                if (veryURL != "") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString("https://verystream.com/e/" + veryURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        // print(d);
                                                        debug("-------------------- HD --------------------");
                                                        url = "https://verystream.com/gettoken/" + FindHTML(d, "videolink\">", "<");
                                                        debug(url);
                                                        if (url != "https://verystream.com/gettoken/") {
                                                            /*
                                                            if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                // print(activeMovie.episodes[episode].Progress);
                                                                activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 10, name = "HD Verystream" });
                                                                linkAdded?.Invoke(null, 1);
                                                            }*/
                                                            AddPotentialLink(episode, url, "HD Verystream", 20);
                                                        }

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Verystream Link error (Read api)");
                                                    debug("");
                                                }
                                                //   activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                const string __lookFor = "https:\\/\\/gomo.to\\/vid\\/";
                                                while (result.Contains(__lookFor)) {
                                                    string gogoStream = FindHTML(result, __lookFor, "\"");
                                                    result = RemoveOne(result, __lookFor);
                                                    if (gogoStream != "") {
                                                        debug(gogoStream);
                                                        try {
                                                            if (!GetThredActive(tempThred)) { return; };
                                                            string trueUrl = "https://" + GOMOURL + "/vid/" + gogoStream;
                                                            print(trueUrl);
                                                            d = client.DownloadString(trueUrl);
                                                            print("-->><<__" + d);
                                                            if (!GetThredActive(tempThred)) { return; };

                                                            //print(d);
                                                            // print("https://gomostream.com" + gogoStream);
                                                            //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
                                                            debug("-------------------- HD --------------------");
                                                            url = GetViduplayerUrl(d);
                                                            debug(url);
                                                            if (!url.EndsWith(".viduplayer.com/urlset/v.mp4") && !url.EndsWith(".viduplayer.com/vplayer/v.mp4") && !url.EndsWith(".viduplayer.com/adb/v.mp4")) {
                                                                /*if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                    //print(activeMovie.episodes[episode].Progress);
                                                                    activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 9, name = "HD Viduplayer" });
                                                                    linkAdded?.Invoke(null, 1);

                                                                }*/
                                                                debug("ADDED");
                                                                AddPotentialLink(episode, url, "HD Viduplayer", 19);

                                                            }
                                                            debug("--------------------------------------------");
                                                            debug("");
                                                        }
                                                        catch (System.Exception) {
                                                        }

                                                    }
                                                    else {
                                                        debug("HD Viduplayer Link error (Read api)");
                                                        debug("");
                                                    }
                                                }

                                                // activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                if (gunURL != "" && gunURL != "https://gounlimited.to/") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString(gunURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        string mid = FindHTML(d, "mp4|", "|");
                                                        string server = FindHTML(d, mid + "|", "|");
                                                        url = "https://" + server + ".gounlimited.to/" + mid + "/v.mp4";
                                                        if (mid != "" && server != "") {
                                                            /*
                                                            if (!LinkListContainsString(activeMovie.episodes[episode].links, url)) {
                                                                // print(activeMovie.episodes[episode].Progress);

                                                                activeMovie.episodes[episode].links.Add(new Link() { url = url, priority = 8, name = "HD Go Unlimited" });
                                                                linkAdded?.Invoke(null, 1);

                                                            }*/
                                                            AddPotentialLink(episode, url, "HD Go Unlimited", 18);

                                                        }
                                                        debug("-------------------- HD --------------------");
                                                        debug(url);

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Go Link error (Read api)");
                                                    debug("");
                                                }
                                                // activeMovie.episodes[episode] = SetEpisodeProgress(activeMovie.episodes[episode]);

                                                if (onlyURL != "" && onlyURL != "https://onlystream.tv") {
                                                    try {
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        d = client.DownloadString(onlyURL);
                                                        if (!GetThredActive(tempThred)) { return; };

                                                        string _url = FindHTML(d, "file:\"", "\"");

                                                        if (_url == "") {
                                                            _url = FindHTML(d, "src: \"", "\"");
                                                        }

                                                        bool valid = false;
                                                        if (CheckIfURLIsValid(_url)) { // NEW USES JW PLAYER I THNIK, EASIER LINK EXTRACTION
                                                            url = _url; valid = true;
                                                        }
                                                        else { // OLD SYSTEM I THINK
                                                            string server = "";//FindHTML(d, "urlset|", "|");
                                                            string mid = FindHTML(d, "logo|", "|");

                                                            if (mid == "" || mid.Length < 10) {
                                                                mid = FindHTML(d, "mp4|", "|");
                                                            }

                                                            string prefix = FindHTML(d, "ostreamcdn|", "|");

                                                            url = "";
                                                            if (server != "") {
                                                                url = "https://" + prefix + ".ostreamcdn.com/" + server + "/" + mid + "/v/mp4"; // /index-v1-a1.m3u8 also works if you want the m3u8 file instead
                                                            }
                                                            else {
                                                                url = "https://" + prefix + ".ostreamcdn.com/" + mid + "/v/mp4";
                                                            }

                                                            if (mid != "" && prefix != "" && mid.Length > 10) {
                                                                valid = true;
                                                            }
                                                        }

                                                        if (valid) {
                                                            AddPotentialLink(episode, url, "HD Onlystream", 17);
                                                        }
                                                        else {
                                                            debug(d);
                                                            debug("FAILED URL: " + url);
                                                        }

                                                        debug("-------------------- HD --------------------");
                                                        debug(url);

                                                        debug("--------------------------------------------");
                                                        debug("");
                                                    }
                                                    catch (System.Exception) {

                                                    }

                                                }
                                                else {
                                                    debug("HD Only Link error (Read api)");
                                                    debug("");
                                                }

                                                done = true;
                                            }
                                            else {
                                                done = true;
                                                debug("DA FAILED");
                                            }
                                        }
                                        catch (Exception) {
                                            done = true;
                                        }
                                    }
                                }), _webRequest);


                            }), webRequest);
                        }
                        else {
                            debug("Dident get gogo");
                        }
                    }
                    catch (System.Exception) {
                        debug("Error");
                    }
                }
                finally {
                    while (!done) {
                        Thread.Sleep(20);
                    }
                    try {
                        linksProbablyDone?.Invoke(null, activeMovie.episodes[episode]);

                    }
                    catch (Exception) {

                    }
                    JoinThred(tempThred);
                }
            }
        }

        class TMDBProvider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {

            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                if (isMovie) return;
                /*
                TempThred tempThred = new TempThred();
                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                string d = DownloadString("https://www.themoviedb.org/search/tv?query=" + activeMovie.title.name + "&language=en-US");
                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                if (d != "") {
                    string tmdbId = FindHTML(d, "<a id=\"tv_", "\"");
                    if (tmdbId != "") {
                        string _d = DownloadString("https://1movietv.com/playstream/" + tmdbId + "-" + season + "-" + episode, tempThred);
                        GetMovieTv(normalEpisode, _d, tempThred);
                        //https://1movietv.com/playstream/71340-2-8
                    }
                }
                /* }
                 finally {
                     JoinThred(tempThred);
                 }
             });
             tempThred.Thread.Name = "Movietv";
             tempThred.Thread.Start();*/
            }
        }

        class WatchSeriesProvider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {
                TempThred tempThred = new TempThred();
                tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        if (activeMovie.title.movieType == MovieType.Anime) { return; }

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                        string url = "http://watchserieshd.tv/search.html?keyword=" + rinput.Replace("+", "%20");

                        string d = DownloadString(url);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        const string lookFor = " <div class=\"vid_info\">";
                        List<FishWatch> fishWatches = new List<FishWatch>();

                        while (d.Contains(lookFor)) {
                            d = RemoveOne(d, lookFor);
                            string href = FindHTML(d, "<a href=\"", "\"");
                            if (href.Contains("/drama-info")) continue;
                            string title = FindHTML(d, "title=\"", "\"");
                            string _d = DownloadString("http://watchserieshd.tv" + href);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            try {
                                string imdbScore = FindHTML(_d, "IMDB: ", " ");
                                string released = FindHTML(_d, "Released: ", " ").Substring(0, 4);
                                int season = -1;
                                for (int i = 0; i < 100; i++) {
                                    if (title.Contains(" - Season " + i)) {
                                        season = i;
                                    }
                                }
                                string removedTitle = title.Replace(" - Season " + season, "").Replace(" ", "");

                                print(imdbScore + "|" + released + "|" + href + "|" + title + "|" + removedTitle);

                                fishWatches.Add(new FishWatch() { imdbScore = imdbScore, released = released, removedTitle = removedTitle, season = season, title = title, href = href });
                            }
                            catch (Exception) {

                            }


                            // MonitorFunc(() => print(">>>" + activeMovie.title.movies123MetaData.seasonData.Count),0);
                        }

                        List<FishWatch> nonSeasonOne = new List<FishWatch>();
                        List<FishWatch> seasonOne = new List<FishWatch>();
                        List<FishWatch> other = new List<FishWatch>();
                        for (int i = 0; i < fishWatches.Count; i++) {

                            if (fishWatches[i].season > 1) {
                                nonSeasonOne.Add(fishWatches[i]);
                            }
                            else if (fishWatches[i].season == 1) {
                                seasonOne.Add(fishWatches[i]);
                                other.Add(fishWatches[i]);
                            }
                            else {
                                other.Add(fishWatches[i]);
                            }

                        }
                        for (int q = 0; q < nonSeasonOne.Count; q++) {
                            for (int z = 0; z < seasonOne.Count; z++) {
                                if (nonSeasonOne[q].removedTitle == seasonOne[z].removedTitle) {
                                    FishWatch f = nonSeasonOne[q];
                                    f.released = seasonOne[z].released;
                                    other.Add(f);
                                }
                            }
                        }
                        activeMovie.title.watchSeriesHdMetaData = new List<WatchSeriesHdMetaData>();
                        other = other.OrderBy(t => t.season).ToList();
                        for (int i = 0; i < other.Count; i++) {
                            string s1 = activeMovie.title.rating;
                            string s2 = other[i].imdbScore;
                            if (s2.ToLower() == "n/a") {
                                continue;
                            }

                            if (!s1.Contains(".")) { s1 += ".0"; }
                            if (!s2.Contains(".")) { s2 += ".0"; }

                            int i1 = int.Parse(s1.Replace(".", ""));
                            int i2 = int.Parse(s2.Replace(".", ""));

                            print(i1 + "||" + i2 + "START:::" + ToDown(other[i].removedTitle.Replace("-", "").Replace(":", ""), replaceSpace: "") + "<<>>" + ToDown(activeMovie.title.name.Replace("-", "").Replace(":", ""), replaceSpace: "") + ":::");
                            if ((i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1) && ToDown(other[i].removedTitle.Replace("-", "").Replace(":", ""), replaceSpace: "") == ToDown(activeMovie.title.name.Replace("-", "").Replace(":", ""), replaceSpace: "")) {

                                if (other[i].released == activeMovie.title.ogYear || activeMovie.title.movieType != MovieType.Movie) {
                                    print("TRUE:::::" + other[i].imdbScore + "|" + other[i].released + "|" + other[i].href + "|" + other[i].title + "|" + other[i].removedTitle);
                                    if (other[i].href != "") {
                                        activeMovie.title.watchSeriesHdMetaData.Add(new WatchSeriesHdMetaData() { season = other[i].season, url = other[i].href });
                                    }
                                }
                            }
                        }
                        watchSeriesFishingDone?.Invoke(null, activeMovie);
                        fishingDone?.Invoke(null, activeMovie);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "WatchSeriesHdMetaData";
                tempThred.Thread.Start();
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                /*
                TempThred tempThred = new TempThred();
                tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {*/
                if (activeMovie.title.watchSeriesHdMetaData.Count == 1) {
                    season = activeMovie.title.watchSeriesHdMetaData[0].season;
                }
                for (int i = 0; i < activeMovie.title.watchSeriesHdMetaData.Count; i++) {
                    var meta = activeMovie.title.watchSeriesHdMetaData[i];
                    if (meta.season == season) {
                        string href = "http://watchserieshd.tv" + meta.url + "-episode-" + (normalEpisode + 1);
                        string d = DownloadString(href, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        string dError = "<h1 class=\"entry-title\">Page not found</h1>";
                        if (d.Contains(dError) && activeMovie.title.movieType == MovieType.Movie) {
                            href = "http://watchserieshd.tv" + meta.url + "-episode-0";
                            d = DownloadString(href, tempThred);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        }
                        if (d.Contains(dError)) {

                        }
                        else {
                            AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                        }
                        print("HREF:" + href);
                    }
                }
                /*  }
                  finally {
                      JoinThred(tempThred);
                  }
              });
              tempThred.Thread.Name = "GetLinksFromWatchSeries";
              tempThred.Thread.Start();*/
            }
        }

        class FMoviesProvider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {
                if (!FMOVIES_ENABLED) return;

                TempThred tempThred = new TempThred();
                tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        if (activeMovie.title.movieType == MovieType.Anime) { return; }

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                        string url = "https://fmovies.to/search?keyword=" + rinput.Replace("+", "%20");
                        string realName = activeMovie.title.name;
                        bool isMovie = (activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie);
                        string realYear = activeMovie.title.year;

                        List<FMoviesData> data = new List<FMoviesData>();

                        string d = HTMLGet(url, "https://fmovies.to");
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        string lookFor = "class=\"name\" href=\"/film/";
                        while (d.Contains(lookFor)) {
                            string _url = FindHTML(d, lookFor, "\"");
                            //print(_url);
                            string ajax = FindHTML(d, "data-tip=\"ajax/film/", "\"");

                            d = RemoveOne(d, lookFor);
                            string name = FindHTML(d, ">", "<");

                            bool same = false;
                            int season = 0;
                            same = name.Replace(" ", "").ToLower() == realName.Replace(" ", "").ToLower();
                            if (!same && !isMovie) {
                                for (int i = 1; i < 100; i++) {
                                    if (name.Replace(" ", "").ToLower() == realName.Replace(" ", "").ToLower() + i) {
                                        same = true;
                                        season = i;
                                        break;
                                    }
                                }
                            }

                            //  var result = Regex.Replace(name, @"[0-9\-]", string.Empty);

                            bool isSame = false;
                            if (same) {
                                if (isMovie) {
                                    string ajaxDownload = DownloadString("https://fmovies.to/ajax/film/" + ajax);
                                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                    if (ajaxDownload == "") {
                                        print("AJAX");
                                    }
                                    else {
                                        string ajaxYear = FindHTML(ajaxDownload, "<span>", "<");
                                        string ajaxIMDb = FindHTML(ajaxDownload, "<i>IMDb</i> ", "<"); // 9.0 = 9
                                        if (ajaxYear == realYear) {
                                            isSame = true;
                                        }
                                    }
                                }
                                else {
                                    isSame = true;
                                }
                            }
                            if (isSame) {
                                data.Add(new FMoviesData() { url = _url, season = season });
                                print(name + "|" + _url + "|" + season);
                            }

                            // print(ajaxDownload);
                        }
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        activeMovie.title.fmoviesMetaData = data;
                        fmoviesFishingDone?.Invoke(null, activeMovie);
                        fishingDone?.Invoke(null, activeMovie);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "FishFmovies";
                tempThred.Thread.Start();
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                if (!FMOVIES_ENABLED) return;

                /*   TempThred tempThred = new TempThred();

                   tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                   tempThred.Thread = new System.Threading.Thread(() => {
                       try {*/
                print("FMOVIESMETA:" + activeMovie.title.fmoviesMetaData);

                if (activeMovie.title.fmoviesMetaData == null) return;
                // bool isMovie = (activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie);
                string url = "";
                for (int i = 0; i < activeMovie.title.fmoviesMetaData.Count; i++) {
                    if (activeMovie.title.fmoviesMetaData[i].season == season || isMovie) {
                        url = activeMovie.title.fmoviesMetaData[i].url;
                        break;
                    }
                }
                print("FMOVIESURL:" + url);

                if (url == "") return;

                string d = HTMLGet("https://fmovies.to/film/" + url, "https://fmovies.to");
                string dataTs = FindHTML(d, "data-ts=\"", "\"");
                string dataId = FindHTML(d, "data-id=\"", "\"");
                string dataEpId = FindHTML(d, "data-epid=\"", "\"");
                string _url = "https://fmovies.to/ajax/film/servers/" + dataId + "?episode=" + dataEpId + "&ts=" + dataTs + "&_=" + Random(100, 999); //
                print(_url);
                //d = DownloadString(_url);
                d = HTMLGet(_url, "https://fmovies.to");

                print(d);

                string cloudGet = "";
                string cLookFor = "<a  data-id=\\\"";
                while (d.Contains(cLookFor)) {
                    string _cloudGet = FindHTML(d, cLookFor, "\\\"");
                    d = RemoveOne(d, cLookFor);
                    string _ep = FindHTML(d, "\">", "<");
                    int ep = 0; if (!isMovie) ep = int.Parse(_ep);
                    if (ep == episode || isMovie) {
                        cloudGet = _cloudGet;
                        d = "";
                    }
                }
                /*{"html":"<div id=\"servers\">\n                        
<div class=\"server row\" data-type=\"iframe\" data-id=\"28\">\n            
<label class=\"name col-md-4 col-sm-5\">\n                
<i class=\"fa fa-server\"><\/i>\n                 MyCloud\n            <\/label>\n            
<div class=\"col-md-20 col-sm-19\">\n                                <ul class=\"episodes range active\"\n                    
data-range-id=\"0\">\n                                        <li>\n                        
<a  data-id=\"b5a4388f1a2fadb87f94fde2abf3a3e85288d96026d6be3bf4920f6aa718e12c\" href=\"\/film\/iron-man-3.885o\/1n4rzyv\">HD<\/a>\n                    
<\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n                            
<div class=\"server row\" data-type=\"iframe\" data-id=\"36\">\n            <label class=\"name col-md-4 col-sm-5\">\n                
<i class=\"fa fa-server\"><\/i>\n                 F5 - HQ\n            <\/label>\n            <div class=\"col-md-20 col-sm-19\">\n                                
<ul class=\"episodes range active\"\n                    data-range-id=\"0\">\n                                        <li>\n                        
<a  data-id=\"05912e0bb9a837fc540e8ddc66beb8f2047667d192b2bee4aa9ba8e744f0eaea\" href=\"\/film\/iron-man-3.885o\/pr65wqx\">HD<\/a>\n                    
<\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n                            
<div class=\"server row\" data-type=\"iframe\" data-id=\"39\">\n            <label class=\"name col-md-4 col-sm-5\">\n                
<i class=\"fa fa-server\"><\/i>\n                 Hydrax\n            <\/label>\n            <div class=\"col-md-20 col-sm-19\">\n                                
<ul class=\"episodes range active\"\n                    data-range-id=\"0\">\n                                        <li>\n                        
<a class=\"active\" data-id=\"41d881669367be4d23b70715f40410adac4788764836c1b80801639f08621e96\" href=\"\/film\/iron-man-3.885o\/m280668\">HD<\/a>\n                    
<\/li>\n                                    <\/ul>\n                            <\/div>\n        <\/div>\n            <\/div>"}


https://prettyfast.to/e/66vvrk\/fe1541bb8d2aeaec6bb7e500d070b2ec?sub=https%253A%252F%252Fstaticf.akacdn.ru%252Ff%252Fsubtitle%252F7309.vtt%253Fv1*/
                // https://fmovies.to/ajax/episode/info?ts=1574168400&_=694&id=d49ac231d1ddf83114eadf1234a1f5d8136dc4a5b6db299d037c06804b37b1ab&server=28
                // https://fmovies.to/ajax/episode/info?ts=1574168400&_=199&id=1c7493cc7bf3cc16831ff9bf1599ceb6f4be2a65a57143c5a24c2dbea99104de&server=97
                d = "";
                int errorCount = 0;
                while (d == "" && errorCount < 10) {
                    errorCount++;
                    string rD = "https://fmovies.to/ajax/episode/info?ts=" + dataTs + "&_=" + Random(100, 999) + "&id=" + cloudGet + "&server=" + Random(1, 99);
                    print(rD);
                    d = HTMLGet(rD, "https://fmovies.to");
                }
                if (d != "") {
                    string lookFor = "\"target\":\"";
                    while (d.Contains(lookFor)) {
                        string __url = FindHTML(d, lookFor, "\"").Replace("\\/", "/");
                        string dl = HTMLGet(__url, "https://fmovies.to");
                        string _lookFor = "\"file\":\"";
                        while (dl.Contains(_lookFor)) {
                            string __link = FindHTML(dl, _lookFor, "\"");
                            if (__link != "") {

                                AddPotentialLink(normalEpisode, __link, "HD FMovies", -1);  //"https://bharadwajpro.github.io/m3u8-player/player/#"+ __link, "HD FMovies", 30); // https://bharadwajpro.github.io/m3u8-player/player/#
                            }
                            dl = RemoveOne(dl, _lookFor);
                        }
                        d = RemoveOne(d, lookFor);
                    }
                }
                /*  }
                  finally {
                      JoinThred(tempThred);
                  }
              });
              tempThred.Thread.Name = "GetFmoviesLinks";
              tempThred.Thread.Start();*/
            }
        }

        class YesMoviesProvider : IMovieProvider
        {
            public void FishMainLinkTSync()
            {
                TempThred tempThred = new TempThred();
                tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        if (activeMovie.title.movieType == MovieType.Anime) { return; }

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        string rinput = ToDown(activeMovie.title.name, replaceSpace: "+");
                        string yesmovies = "https://yesmoviess.to/search/?keyword=" + rinput.Replace("+", "-");


                        string d = DownloadString(yesmovies, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        int counter = 0;
                        const string lookfor = "data-url=\"";
                        while ((d.Contains(lookfor)) && counter < 100) {
                            counter++;
                            string url = FindHTML(d, lookfor, "\"");
                            string remove = "class=\"ml-mask jt\" title=\"";
                            string title = FindHTML(d, remove, "\"");
                            string movieUrl = "https://yesmoviess.to/movie/" + FindHTML(d, "<a href=\"https://yesmoviess.to/movie/", "\"");
                            d = RemoveOne(d, remove);

                            int seasonData = 1;
                            for (int i = 0; i < 100; i++) {
                                if (title.Contains(" - Season " + i)) {
                                    seasonData = i;
                                }
                            }
                            string realtitle = title.Replace(" - Season " + seasonData, "");
                            string _d = DownloadString(url, tempThred);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            string imdbData = FindHTML(_d, "IMDb: ", "<").Replace("\n", "").Replace(" ", "").Replace("	", "");
                            //  string year = FindHTML(_d, "<div class=\"jt-info\">", "<").Replace("\n", "").Replace(" ", "").Replace("	", "").Replace("	", "");

                            string s1 = activeMovie.title.rating;
                            string s2 = imdbData;
                            if (s2.ToLower() == "n/a") {
                                continue;
                            }

                            if (!s1.Contains(".")) { s1 += ".0"; }
                            if (!s2.Contains(".")) { s2 += ".0"; }

                            int i1 = int.Parse(s1.Replace(".", ""));
                            int i2 = int.Parse(s2.Replace(".", ""));
                            //activeMovie.title.year.Substring(0, 4) == year
                            if (ToDown(activeMovie.title.name, replaceSpace: "") == ToDown(realtitle, replaceSpace: "") && (i1 == i2 || i1 == i2 - 1 || i1 == i2 + 1)) {
                                print("TRUE: " + imdbData + "|" + realtitle);
                                if (activeMovie.title.yesmoviessSeasonDatas == null) {
                                    activeMovie.title.yesmoviessSeasonDatas = new List<YesmoviessSeasonData>();
                                }
                                activeMovie.title.yesmoviessSeasonDatas.Add(new YesmoviessSeasonData() { url = movieUrl, id = seasonData });
                            }
                            print("DATA:" + imdbData + "|" + movieUrl + "|" + realtitle + "|" + title + "|" + seasonData + "|" + url + "|" + i1 + "|" + i2);
                        }
                        yesmovieFishingDone?.Invoke(null, activeMovie);
                        fishingDone?.Invoke(null, activeMovie);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "YesMoviesMetaData";
                tempThred.Thread.Start();
            }

            public void LoadLinksTSync(int episode, int season, int normalEpisode, bool isMovie, TempThred tempThred)
            {
                if (activeMovie.title.yesmoviessSeasonDatas != null) {
                    for (int i = 0; i < activeMovie.title.yesmoviessSeasonDatas.Count; i++) {
                        //     print(activeMovie.title.yesmoviessSeasonDatas[i].id + "<-IDS:" + season);
                        if (activeMovie.title.yesmoviessSeasonDatas[i].id == (isMovie ? 1 : season)) {
                            string url = activeMovie.title.yesmoviessSeasonDatas[i].url;

                            /* TempThred tempThred = new TempThred();
                             tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                             tempThred.Thread = new System.Threading.Thread(() => {
                                 try {*/
                            int _episode = normalEpisode + 1;
                            string d = DownloadString(url.Replace("watching.html", "") + "watching.html");

                            string movieId = FindHTML(d, "var movie_id = \'", "\'");
                            if (movieId == "") return;

                            d = DownloadString("https://yesmoviess.to/ajax/v2_get_episodes/" + movieId);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string episodeId = FindHTML(d, "title=\"Episode " + _episode + "\" class=\"btn-eps\" episode-id=\"", "\"");
                            if (episodeId == "") return;
                            d = DownloadString("https://yesmoviess.to/ajax/load_embed/mov" + episodeId);

                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string embedededUrl = FindHTML(d, "\"embed_url\":\"", "\"").Replace("\\", "") + "=EndAll";
                            string __url = FindHTML(embedededUrl, "id=", "=EndAll");
                            if (__url == "") return;
                            embedededUrl = "https://video.opencdn.co/api/?id=" + __url;
                            print(embedededUrl + "<<<<<<<<<<<<<<<<");
                            d = DownloadString(embedededUrl);

                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            string link = FindHTML(d, "\"link\":\"", "\"").Replace("\\", "").Replace("//", "https://").Replace("https:https:", "https:");
                            print("LINK:" + link);
                            d = DownloadString(link);

                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                            string secondLink = FindHTML(d, "https://vidnode.net/download?id=", "\"");
                            print("FIRST: " + secondLink);
                            if (secondLink != "") {
                                d = DownloadString("https://vidnode.net/download?id=" + secondLink);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                GetVidNode(d, normalEpisode);
                            }
                            LookForFembedInString(tempThred, normalEpisode, d);
                            /*  }
                              finally {
                                  JoinThred(tempThred);
                              }
                          });
                          tempThred.Thread.Name = "YesMovies";
                          tempThred.Thread.Start();*/
                        }
                    }
                }
            }
        }

        #endregion

        static void GetSeasonAndPartFromName(string name, out int season, out int part)
        {
            season = 0;
            for (int i = 1; i < 100; i++) {
                if (name.ToLower().Contains("season " + i)) {
                    season = i;
                }
            }

            if (name.ToLower().Contains("2nd season")) {
                season = 2;
            }
            else if (name.ToLower().Contains("3rd season")) {
                season = 3;
            }
            if (season == 0) {
                for (int i = 1; i < 7; i++) {
                    if (name.EndsWith(" " + i)) {
                        season = i;
                    }
                }
            }
            if (season == 0) {
                season = 1;
            }
            part = 1;
            for (int i = 2; i < 5; i++) {
                if (name.ToLower().Contains("part " + i)) {
                    part = i;
                }
            }
        }



        /// <summary>
        /// Get a shareble url of the current movie
        /// </summary>
        /// <param name="extra"></param>
        /// <param name="redirectingName"></param>
        /// <returns></returns>
        public static string ShareMovieCode(string extra, string redirectingName = "Redirecting to CloudStream 2")
        {
            const string baseUrl = "CloudStreamForms";
            //Because I don't want to host my own servers I "Save" a js code on a free js hosting site. This code will automaticly give a responseurl that will redirect to the CloudStream app.
            string code = ("var x = document.createElement('body');\n var s = document.createElement(\"script\");\n s.innerHTML = \"window.location.href = '" + baseUrl + ":" + extra + "';\";\n var h = document.createElement(\"H1\");\n var div = document.createElement(\"div\");\n div.style.width = \"100%\";\n div.style.height = \"100%\";\n div.align = \"center\";\n div.style.padding = \"130px 0\";\n div.style.margin = \"auto\";\n div.innerHTML = \"" + redirectingName + "\";\n h.append(div);\n x.append(h);\n x.append(s);\n parent.document.body = x;").Replace("%", "%25");
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create("https://js.do/mod_perl/js.pl");
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            string postData = "action=save_code&js_code=" + code + "&js_title=&js_permalink=&js_id=&is_update=false";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            // Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            reader.Close();
            dataStream.Close();
            response.Close();
            string rLink = "https://js.do/code/" + FindHTML(responseFromServer, "js_permalink\":\"", "\"");
            return rLink;
            // Clean up the streams.
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static List<IMDbTopList> FetchRecomended(List<string> inp, bool shuffle = true, int max = 10)
        {
            List<IMDbTopList> topLists = new List<IMDbTopList>();

            Shuffle(inp);
            if (inp.Count > max) {
                inp.RemoveRange(max, inp.Count - max);
            }

            for (int q = 0; q < inp.Count; q++) {
                string url = "https://www.imdb.com/title/" + inp[q];

                //string d =;
                string _d = GetHTML(url);
                const string lookFor = "<div class=\"rec_item\"";
                while (_d.Contains(lookFor)) {
                    _d = RemoveOne(_d, lookFor);
                    string tt = FindHTML(_d, " data-tconst=\"", "\"");
                    string name = FindHTML(_d, "alt=\"", "\"", decodeToNonHtml: true);
                    string img = FindHTML(_d, "loadlate=\"", "\"");
                    string d = RemoveOne(_d, "<a href=\"/title/" + tt + "/vote?v=X;k", -200);
                    string __d = FindHTML(_d, "<div class=\"rec-title\">\n       <a href=\"/title/" + tt, "<div class=\"rec-rating\">");
                    List<string> genresNames = new List<string>() { "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };
                    List<int> contansGenres = new List<int>();
                    for (int i = 0; i < genresNames.Count; i++) {
                        if (__d.Contains(genresNames[i])) {
                            contansGenres.Add(i);
                        }
                    }
                    string value = FindHTML(d, "<span class=\"value\">", "<");
                    string descript = FindHTML(d, "<div class=\"rec-outline\">\n    <p>\n    ", "<");
                    if (!value.Contains(".")) {
                        value += ".0";
                    }

                    bool add = true;
                    for (int z = 0; z < topLists.Count; z++) {
                        if (topLists[z].id == tt) {

                            add = false;
                        };
                    }

                    if (add) {
                        topLists.Add(new IMDbTopList() { name = name, descript = descript, contansGenres = contansGenres, id = tt, img = img, place = -1, rating = value, runtime = "", genres = "" });
                    }
                    else {
                    }
                }
            }

            if (shuffle) {
                Shuffle<IMDbTopList>(topLists);
            }

            return topLists;
        }

        public static List<IMDbTopList> FetchTop100(List<string> order, int start = 1, int count = 250)
        {
            IMDbTopList[] topLists = new IMDbTopList[count];
            //List<string> genres = new List<string>() { "action", "adventure", "animation", "biography", "comedy", "crime", "drama", "family", "fantasy", "film-noir", "history", "horror", "music", "musical", "mystery", "romance", "sci-fi", "sport", "thriller", "war", "western" };
            //List<string> genresNames = new List<string>() { "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };
            string orders = "";
            for (int i = 0; i < order.Count; i++) {
                if (i != 0) {
                    orders += ",";
                }
                orders += order[i];
            }
            //https://www.imdb.com/search/title/?genres=adventure&sort=user_rating,desc&title_type=feature&num_votes=25000,&pf_rd_m=A2FGELUUNOQJNL&pf_rd_p=5aab685f-35eb-40f3-95f7-c53f09d542c3&pf_rd_r=VV0XPKMS8FXZ6D8MM0VP&pf_rd_s=right-6&pf_rd_t=15506&pf_rd_i=top&ref_=chttp_gnr_2
            //https://www.imdb.com/search/title/?title_type=feature&num_votes=25000,&genres=action&sort=user_rating,desc&start=51&ref_=adv_nxt
            string trueUrl = "https://www.imdb.com/search/title/?title_type=feature&num_votes=25000,&genres=" + orders + "&sort=user_rating,desc&start=" + start + "&ref_=adv_nxt&count=" + count;
            print("TRUEURL:" + trueUrl);
            string d = GetHTML(trueUrl, true);
            print("FALSEURL:" + trueUrl);

            const string lookFor = "class=\"loadlate\"";
            int place = start - 1;
            int counter = 0;
            while (d.Contains(lookFor)) {
                place++;
                d = RemoveOne(d, lookFor);

                string img = FindHTML(d, "loadlate=\"", "\"");
                string id = FindHTML(d, "data-tconst=\"", "\"");
                string runtime = FindHTML(d, "<span class=\"runtime\">", "<");
                string name = FindHTML(d, "ref_=adv_li_tt\"\n>", "<");
                string rating = FindHTML(d, "</span>\n        <strong>", "<");
                string _genres = FindHTML(d, "<span class=\"genre\">\n", "<").Replace("  ", "");
                string descript = FindHTML(d, "<p class=\"text-muted\">\n    ", "<").Replace("  ", "");
                topLists[counter] = (new IMDbTopList() { descript = descript, genres = _genres, id = id, img = img, name = name, place = place, rating = rating, runtime = runtime });
                counter++;
            }
            print("------------------------------------ DONE! ------------------------------------");
            return topLists.ToList();
        }

        public static void QuickSearch(string text, bool purgeCurrentSearchThread = true, bool onlySearch = true)
        {

            if (purgeCurrentSearchThread) {
                PurgeThreds(1);
            }

            TempThred tempThred = new TempThred();

            tempThred.typeId = 1;

            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                    text = rgx.Replace(text, "").ToLower();
                    if (text == "") {
                        return;
                    }
                    string qSearchLink = "https://v2.sg.media-imdb.com/suggestion/" + text.Substring(0, 1) + "/" + text.Replace(" ", "_") + ".json";
                    string result = DownloadString(qSearchLink, tempThred);
                    //print(qSearchLink+ "|" +result);
                    //  string lookFor = "{\"i\":{\"";

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    activeSearchResults = new List<Poster>();

                    //int counter = 0;
                    /*
                    while (result.Contains(lookFor) && counter < 100) {
                        counter++;
                        string name = ReadJson(result, "\"l");
                        name = RemoveHtmlChars(name);
                        string posterUrl = ReadJson(result, "imageUrl");
                        string extra = ReadJson(result, "\"q");
                        string year = FindHTML(result, "\"y\":", "}"); string oyear = year;
                        string years = FindHTML(year, "yr\":\"", "\""); if (years.Length > 4) { year = years; }
                        string id = ReadJson(result, "\"id");
                        string rank = FindHTML(result, "rank\":", ",");
                        if (extra == "feature") { extra = ""; }

                        if (year != "" && id.StartsWith("tt") && !extra.Contains("video game")) {
                            AddToActiveSearchResults(new Poster() { name = name, posterUrl = posterUrl, extra = extra, year = year, rank = rank, url = id, posterType = PosterType.Imdb });
                        }
                        result = RemoveOne(result, "y\":" + oyear);
                    }*/
                    try {
                        var f = JsonConvert.DeserializeObject<IMDbQuickSearch>(result);

                        if (f.d != null) {
                            for (int i = 0; i < f.d.Length; i++) {
                                var poster = f.d[i];
                                string year = poster.yr ?? poster.y.ToString();
                                if (poster.id.StartsWith("tt") && year != "0") {
                                    print("ID::" + poster.id + "|" + year);
                                    string extra = poster.q ?? "";
                                    if (extra == "feature") extra = "";

                                    AddToActiveSearchResults(new Poster() { extra = extra, name = poster.l ?? "", posterType = PosterType.Imdb, posterUrl = poster.i.imageUrl ?? "", year = year, url = poster.id, rank = poster.rank.ToString() ?? "" });
                                }

                            }
                        }
                    }
                    catch (Exception _ex) {
                        print("EERROOR:" + _ex);
                    }

                    if (onlySearch) {
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        searchLoaded?.Invoke(null, activeSearchResults);
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "QuickSearch";
            tempThred.Thread.Start();
        }

        public static string RemoveHtmlChars(string inp)
        {
            return System.Net.WebUtility.HtmlDecode(inp);
        }



        public static void GetMALData(bool cacheData = true)
        {
            bool fetchData = true;
            if (Settings.CacheMAL) {
                if (App.KeyExists("CacheMAL", activeMovie.title.id)) {
                    fetchData = false;
                    activeMovie.title.MALData = App.GetKey<MALData>("CacheMAL", activeMovie.title.id, new MALData());
                    if (activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason == null) {
                        fetchData = true;
                    }
                }
            }
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string currentSelectedYear = "";

                    if (fetchData) {
                        string year = activeMovie.title.year.Substring(0, 4); // this will not work in 8000 years time :)
                        string _d = DownloadString("https://myanimelist.net/search/prefix.json?type=anime&keyword=" + activeMovie.title.name, tempThred);
                        string url = "";
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        //string lookFor = "\"name\":\"";
                        //bool done = false;
                        var f = JsonConvert.DeserializeObject<MALQuickSearch>(_d);

                        try {
                            var items = f.categories[0].items;
                            for (int i = 0; i < items.Length; i++) {
                                var item = items[i];
                                string _year = item.payload.start_year.ToString();
                                if (!item.name.Contains(" Season") && !item.name.EndsWith("Specials") && _year == year && item.payload.score != "N/A") {
                                    url = item.url;
                                    currentSelectedYear = _year;
                                    break;
                                }
                            }
                        }
                        catch (Exception _ex) {
                            print("EROROOROROROOR::" + _ex);
                        }


                        /*
                        while (_d.Contains(lookFor) && !done) { // TO FIX MY HERO ACADIMEA CHOOSING THE SECOND SEASON BECAUSE IT WAS FIRST SEARCHRESULT
                            string name = FindHTML(_d, lookFor, "\"");
                            print("NAME FOUND: " + name);
                            if (!name.EndsWith("Specials")) {
                                string _url = FindHTML(_d, "url\":\"", "\"").Replace("\\/", "/");
                                string startYear = FindHTML(_d, "start_year\":", ",");
                                string aired = FindHTML(_d, "aired\":\"", "\"");
                                string _aired = FindHTML(aired, ", ", " ", readToEndOfFile: true);
                                string score = FindHTML(_d, "score\":\"", "\"");
                                print("SCORE:" + score);
                                if (!name.Contains(" Season") && year == _aired && score != "N\\/A") {
                                    print("URL FOUND: " + _url);
                                    print(_d);
                                    url = _url;
                                    done = true;
                                    currentSelectedYear = _aired;
                                }

                            }
                            _d = RemoveOne(_d, lookFor);
                            _d = RemoveOne(_d, "\"id\":");
                        }*/

                        /*

                        string d = DownloadString("https://myanimelist.net/search/all?q=" + activeMovie.title.name);

                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        d = RemoveOne(d, " <div class=\"picSurround di-tc thumb\">"); // DONT DO THIS USE https://myanimelist.net/search/prefix.json?type=anime&keyword=my%20hero%20acadimea
                        string url = "";//"https://myanimelist.net/anime/" + FindHTML(d, "<a href=\"https://myanimelist.net/anime/", "\"");
                        */

                        if (url == "") return;

                        WebClient webClient = new WebClient();
                        webClient.Encoding = Encoding.UTF8;

                        string d = webClient.DownloadString(url);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        string jap = FindHTML(d, "Japanese:</span> ", "<").Replace("  ", "").Replace("\n", ""); // JAP NAME IS FOR SEARCHING, BECAUSE ALL SEASONS USE THE SAME NAME
                        string eng = FindHTML(d, "English:</span> ", "<").Replace("  ", "").Replace("\n", "");
                        string firstName = FindHTML(d, " itemprop=\"name\">", "<").Replace("  ", "").Replace("\n", "");

                        string currentName = FindHTML(d, "<span itemprop=\"name\">", "<");
                        List<MALSeasonData> data = new List<MALSeasonData>() { new MALSeasonData() { malUrl = url, seasons = new List<MALSeason>() } };

                        string sqlLink = "-1";

                        // ----- GETS ALL THE SEASONS OF A SHOW WITH MY ANIME LIST AND ORDERS THEM IN THE CORRECT SEASON (BOTH Shingeki no Kyojin Season 3 Part 2 and Shingeki no Kyojin Season 3 is season 3) -----
                        while (sqlLink != "") {
                            string _malLink = (sqlLink == "-1" ? url.Replace("https://myanimelist.net", "") : sqlLink);
                            currentName = FindHTML(d, "<span itemprop=\"name\">", "<", decodeToNonHtml: true);
                            string sequel = FindHTML(d, "Sequel:", "</a></td>") + "<";
                            sqlLink = FindHTML(sequel, "<a href=\"", "\"");
                            string _jap = FindHTML(d, "Japanese:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "");
                            string _eng = FindHTML(d, "English:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "");
                            if (_eng == "") {
                                _eng = FindHTML(d, "og:title\" content=\"", "\"", decodeToNonHtml: true);
                            }
                            string _syno = FindHTML(d, "Synonyms:</span> ", "<", decodeToNonHtml: true).Replace("  ", "").Replace("\n", "") + ",";
                            List<string> _synos = new List<string>();
                            while (_syno.Contains(",")) {
                                string _current = _syno.Substring(0, _syno.IndexOf(",")).Replace("  ", "");
                                if (_current.StartsWith(" ")) {
                                    _current = _current.Substring(1, _current.Length - 1);
                                }
                                _synos.Add(_current);
                                _syno = RemoveOne(_syno, ",");
                            }
                            print("CURRENTNAME: " + currentName + "|" + _eng + "|" + _jap);

                            if (currentName.Contains("Part ") && !currentName.Contains("Part 1")) // WILL ONLY WORK UNTIL PART 10, BUT JUST HOPE THAT THAT DOSENT HAPPEND :)
                            {
                                data[data.Count - 1].seasons.Add(new MALSeason() { name = currentName, engName = _eng, japName = _jap, synonyms = _synos, malUrl = _malLink });
                            }
                            else {
                                data.Add(new MALSeasonData() {
                                    seasons = new List<MALSeason>() { new MALSeason() { name = currentName, engName = _eng, japName = _jap, synonyms = _synos, malUrl = _malLink } },
                                    malUrl = "https://myanimelist.net" + _malLink
                                });
                            }
                            if (sqlLink != "") {
                                try {
                                    d = webClient.DownloadString("https://myanimelist.net" + sqlLink);
                                }
                                catch (Exception) {
                                    d = "";
                                }
                            }
                        }
                        for (int i = 0; i < data.Count; i++) {
                            for (int q = 0; q < data[i].seasons.Count; q++) {
                                var e = data[i].seasons[q];
                                string _s = "";
                                for (int z = 0; z < e.synonyms.Count; z++) {
                                    _s += e.synonyms[z] + "|";
                                }
                                print("SEASON: " + (i + 1) + "  -  " + e.name + "|" + e.engName + "|" + e.japName + "| SYNO: " + _s);
                            }
                        }
                        activeMovie.title.MALData = new MALData() {
                            seasonData = data,
                            japName = jap,
                            engName = eng,
                            firstName = firstName,
                            done = false,
                            currentSelectedYear = currentSelectedYear,
                        };
                        if (fetchData && cacheData && Settings.CacheMAL) {
                            App.SetKey("CacheMAL", activeMovie.title.id, activeMovie.title.MALData);
                        }
                    }
                    else {
                        currentSelectedYear = activeMovie.title.MALData.currentSelectedYear;
                    }
                    print("FISHING DATA");

                    /*
                    for (int i = 0; i < animeProviders.Length; i++) {
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        animeProviders[i].FishMainLink(currentSelectedYear, tempThred, activeMovie.title.MALData);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    }*/


                    // FASTER, BUT.. VERY WEIRD BUG BECAUSE THEY ARE ALL WRITING TO SAME CLASS

                    int count = 0;
                    Parallel.For(0, animeProviders.Length, (int i) => {
                        print("STARTEDANIME: " + animeProviders[i].ToString() + "|" + i);
                        fishProgressLoaded?.Invoke(null, new FishLoaded() { name = animeProviders[i].Name, progress = ((double)count) / animeProviders.Length });
                        animeProviders[i].FishMainLink(currentSelectedYear, tempThred, activeMovie.title.MALData);
                        count++;
                        print("COUNT INCRESED < -------------------------------- " + count);
                        //if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    });
                    fishProgressLoaded?.Invoke(null, new FishLoaded() { name = "", progress = 1 });

                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS




                    /*
                    for (int i = 0; i < animeProviders.Length; i++) {
                        print("STARTEDANIME: " + animeProviders[i].ToString() + "|" + i); 
                        animeProviders[i].FishMainLink(currentSelectedYear, tempThred, activeMovie.title.MALData);
                        fishProgressLoaded?.Invoke(null, new FishLoaded() { name = animeProviders[i].Name, progress = (i + 1.0) / animeProviders.Length });
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    }*/

                    FishMALNotification();
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    print("DONE FISHING DATA");
                    MALData md = activeMovie.title.MALData;

                    activeMovie.title.MALData = new MALData() {
                        seasonData = md.seasonData,
                        japName = md.japName,
                        done = true,
                    };

                    //print(sequel + "|" + realSquel + "|" + sqlLink);

                }
                catch {
                    activeMovie.title.MALData.japName = "error";
                    // throw;
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "MAL Search";
            tempThred.Thread.Start();
        }

        public struct AnimeNotTitle
        {
            public string romaji { get; set; }
            public string english { get; set; }
            public string japanese { get; set; }
        }

        public struct AiringDate
        {
            public DateTime start { get; set; }
            public DateTime end { get; set; }
        }


        public struct AnimeNotEpisode
        {
            public string animeId { get; set; }
            public int number { get; set; }
            public AnimeNotTitle title { get; set; }
            public AiringDate airingDate { get; set; }
            public string id { get; set; }
        }


        static void FishMALNotification()
        {
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    var malSeason = activeMovie.title.MALData.seasonData;
                    var season = malSeason[malSeason.Count - 1].seasons;
                    string downloadString = "https://notify.moe/search/" + season[season.Count - 1].engName;
                    print("DOWNLOADINGMOE::" + downloadString);
                    string d = DownloadString(downloadString);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    const string lookFor = "<a href=\'/anime/";
                    while (d.Contains(lookFor)) {
                        string uri = FindHTML(d, lookFor, "\'");
                        string _d = DownloadString("https://notify.moe/api/anime/" + uri);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        MoeApi api = Newtonsoft.Json.JsonConvert.DeserializeObject<MoeApi>(_d);
                        bool doIt = false;
                        string serviceId = "-1";
                        if (api.mappings != null) {
                            for (int i = 0; i < api.mappings.Length; i++) {
                                if (api.mappings[i].service == "myanimelist/anime") {
                                    serviceId = api.mappings[i].serviceId;
                                }
                                // print(api.mappings[i].service);
                            }
                        }

                        // print("DA:::" + season[season.Count - 1].engName + "==||==" + api.title.English + "||" + serviceId + "|" + season[season.Count - 1].malUrl);
                        if (FindHTML(season[season.Count - 1].malUrl, "/anime/", "/") == serviceId && serviceId != "-1") {
                            doIt = true;
                        }
                        // if(Fi season[season.Count - 1].malUrl)

                        if (doIt) {
                            activeMovie.moeEpisodes = new List<MoeEpisode>();
                            // if (ToLowerAndReplace(api.title.English) == ToLowerAndReplace(season[season.Count - 1].engName)) {
                            if (api.episodes != null) {
                                for (int i = api.episodes.Length - 1; i > 0; i--) {
                                    //https://notify.moe/api/episode/
                                    //https://notify.moe/api/episode/r0Zy9WEZRV
                                    //https://notify.moe/api/episode/xGNheCEZgM
                                    // print(api.title.English + "|NO." + (i + 1) + " - " + api.episodes[i]);




                                    print("MOE API::" + i + "|" + uri);
                                    string __d = DownloadString("https://notify.moe/api/episode/" + api.episodes[i]);



                                    var _seasonData = JsonConvert.DeserializeObject<AnimeNotEpisode>(__d);
                                    string end = FindHTML(__d, "\"end\":\"", "\"");

                                    //https://twist.moe/api/anime/angel-beats/sources
                                    /*
                                    string name = _seasonData.title.english ?? "";
                                    if (name == "") {
                                        name = _seasonData.title.japanese ?? "";
                                    }
                                    if (name == "") {
                                        name = _seasonData.title.romaji ?? "";
                                    }
                                    if (name == "") {
                                        name = "Episode " + _seasonData.number;
                                    }*/
                                    string name = "Episode " + _seasonData.number;

                                    var time = DateTime.Parse(end);
                                    var _t = time.Subtract(DateTime.Now);
                                    print("TOTALLSLDLSA::" + _t.TotalSeconds);
                                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                                    if (activeMovie.moeEpisodes == null) return;
                                    print("DADAAA::::::");
                                    if (_t.TotalSeconds < 0) break;
                                    activeMovie.moeEpisodes.Add(new MoeEpisode() { timeOfRelease = time, timeOfMesure = DateTime.Now, number = _seasonData.number, episodeName = name });

                                    //print("TotalDays:" + _t.Days + "|" + _t.Hours + "|" + _t.Minutes);
                                }
                            }
                            moeDone?.Invoke(null, activeMovie.moeEpisodes);
                            return;
                            //   print(uri);
                            //}
                        }
                        d = RemoveOne(d, lookFor);
                    }

                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "FishMALNotification";
            tempThred.Thread.Start();


        }

        public static string ToLowerAndReplace(string inp, bool seasonReplace = true)
        {
            string _inp = inp.ToLower();
            if (seasonReplace) {
                _inp = _inp.Replace("2nd season", "season 2").Replace("3th season", "season 3").Replace("4th season", "season 4");
            }
            _inp = inp.Replace("-", " ").Replace("`", "\'");
            return _inp;
        }

        public static void GetImdbTitle(Poster imdb, bool purgeCurrentTitleThread = true, bool autoSearchTrailer = true, bool cacheData = true)
        {
            string __id = imdb.url.Replace("https://imdb.com/title/", "");
            bool fetchData = true;
            if (Settings.CacheImdb) {
                print("START CACHE");
                if (App.KeyExists("CacheImdb", __id)) {
                    print("KEY EXIST CACHE");

                    fetchData = false;
                    activeMovie = App.GetKey<Movie>("CacheImdb", __id, new Movie());
                    print("KEY EXIST MMM");
                    print(":::::::::" + activeMovie.title.name);

                    if (activeMovie.title.name == null || activeMovie.title.id == null) {
                        fetchData = true;
                    }
                }
            }

            if (purgeCurrentTitleThread) {
                PurgeThreds(2);
            }
            if (fetchData) {
                activeMovie = new Movie();
                activeMovie.title.id = __id;
            }
            // TurnNullMovieToActive(movie);
            TempThred tempThred = new TempThred();
            tempThred.typeId = 2; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string d = "";
                    List<string> keyWords = new List<string>();

                    if (fetchData) {
                        string url = "https://imdb.com/title/" + imdb.url.Replace("https://imdb.com/title/", "") + "/";
                        d = GetHTML(url); // DOWNLOADSTRING WILL GET THE LOCAL LAUNGEGE, AND NOT EN, THAT WILL MESS WITH RECOMENDATIONDS, GetHTML FIXES THAT
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        string _d = DownloadString(url + "keywords", tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        const string _lookFor = "data-item-keyword=\"";
                        while (_d.Contains(_lookFor)) {
                            keyWords.Add(FindHTML(_d, _lookFor, "\""));
                            _d = RemoveOne(_d, _lookFor);
                        }
                        for (int i = 0; i < keyWords.Count; i++) {
                            // print("Keyword: " + keyWords[i]);
                        }
                    }
                    if (d != "" || !fetchData) {
                        // ------ THE TITLE ------

                        try {
                            // ----- GET -----
                            if (fetchData) {
                                int seasons = 0; // d.Count<string>("");

                                for (int i = 1; i <= 100; i++) {
                                    if (d.Contains("episodes?season=" + i)) {
                                        seasons = i;
                                    }
                                }
                                string result = FindHTML(d, "<div class=\"title_wrapper\">", "</a>            </div>");
                                string descript = FindHTML(d, "<div class=\"summary_text\">", "<").Replace("\n", "").Replace("  ", " ").Replace("          ", ""); // string descript = FindHTML(d, "\"description\": \"", "\"");
                                if (descript == "") {
                                    descript = FindHTML(d, "\"description\": \"", "\"", decodeToNonHtml: true);
                                }
                                // print("Dscript: " + descript);
                                string __d = RemoveOne(d, "<div class=\"poster\">");
                                string hdPosterUrl = FindHTML(__d, "src=\"", "\"");
                                string ogName = FindHTML(d, "\"name\": \"", "\"", decodeToNonHtml: true);
                                string rating = FindHTML(d, "\"ratingValue\": \"", "\"");
                                string posterUrl = FindHTML(d, "\"image\": \"", "\"");
                                string genres = FindHTML(d, "\"genre\": [", "]");
                                string type = FindHTML(d, "@type\": \"", "\"");
                                string _trailer = FindHTML(d, "\"trailer\": ", "uploadDate");
                                string trailerUrl = "https://imdb.com" + FindHTML(_trailer, "\"embedUrl\": \"", "\"");
                                string trailerImg = FindHTML(_trailer, "\"thumbnailUrl\": \"", "\"");
                                string trailerName = FindHTML(_trailer, "\"name\": \"", "\"");
                                string keyWord = FindHTML(d, "\"keywords\": \"", "\"");
                                string duration = FindHTML(d, "<time datetime=\"PT", "\"").Replace("M", "min");
                                string year = FindHTML(d, "datePublished\": \"", "-");

                                //<span class="bp_sub_heading">66 episodes</span> //total episodes

                                List<string> allGenres = new List<string>();
                                int counter = 0;
                                while (genres.Contains("\"") && counter < 20) {
                                    counter++;
                                    string genre = FindHTML(genres, "\"", "\"");
                                    allGenres.Add(genre);
                                    genres = genres.Replace("\"" + genre + "\"", "");
                                }

                                MovieType movieType = (!keyWords.Contains("anime") ? (type == "Movie" ? MovieType.Movie : MovieType.TVSeries) : (type == "Movie" ? MovieType.AnimeMovie : MovieType.Anime)); // looks ugly but works

                                if (movieType == MovieType.TVSeries) { // JUST IN CASE
                                    if (d.Contains(">Japan</a>") && d.Contains(">Japanese</a>") && (d.Contains("Anime") || d.Contains(">Animation</a>,"))) {
                                        movieType = MovieType.Anime;
                                    }
                                }

                                // ----- SET -----
                                activeMovie.title = new Title() {
                                    name = REPLACE_IMDBNAME_WITH_POSTERNAME ? imdb.name : ogName,
                                    posterUrl = posterUrl,
                                    trailers = new List<Trailer>(),
                                    rating = rating,
                                    genres = allGenres,
                                    id = imdb.url.Replace("https://imdb.com/title/", ""),
                                    description = descript,
                                    runtime = duration,
                                    seasons = seasons,
                                    MALData = new MALData() { japName = "", seasonData = new List<MALSeasonData>(), currentSelectedYear = "" },
                                    movieType = movieType,
                                    year = year,
                                    ogName = ogName,
                                    hdPosterUrl = hdPosterUrl,
                                    fmoviesMetaData = new List<FMoviesData>(),
                                    watchSeriesHdMetaData = new List<WatchSeriesHdMetaData>(),

                                };

                                activeMovie.title.trailers.Add(new Trailer() { Url = trailerUrl, PosterUrl = trailerImg, Name = trailerName });

                            }
                            try {
                                if (autoSearchTrailer) { GetRealTrailerLinkFromImdb(true); }
                            }
                            catch (Exception) {

                            }

                            if (activeMovie.title.movieType == MovieType.Anime) {
                                GetMALData();
                            }
                            else { // FISHING : THIS IS TO SPEED UP LINK FETHING
                                for (int i = 0; i < movieProviders.Length; i++) {
                                    movieProviders[i].FishMainLinkTSync();
                                }
                                /*
                                FishFmovies();
                                FishMovies123Links();
                                FishYesMoviesLinks();
                                FishWatchSeries();*/
                            }

                        }
                        catch (Exception) { }

                        // ------ RECOMENDATIONS ------

                        if (fetchData) {
                            activeMovie.title.recomended = new List<Poster>();
                            const string lookFor = "<div class=\"rec_item\" data-info=\"\" data-spec=\"";
                            for (int i = 0; i < 12; i++) {
                                try {
                                    string result = FindHTML(d, lookFor, "/> <br/>");
                                    string id = FindHTML(result, "data-tconst=\"", "\"");
                                    string name = FindHTML(result, "title=\"", "\"", decodeToNonHtml: true);
                                    string posterUrl = FindHTML(result, "loadlate=\"", "\"");

                                    d = RemoveOne(d, result);
                                    Poster p = new Poster() { url = id, name = name, posterUrl = posterUrl, posterType = PosterType.Imdb };

                                    // if (!activeMovie.title.recomended.Contains(p)) {
                                    activeMovie.title.recomended.Add(p);
                                    // }

                                }
                                catch (Exception) {

                                }
                            }
                            if (cacheData && Settings.CacheImdb) {
                                App.SetKey("CacheImdb", __id, activeMovie);
                            }
                        }
                        titleLoaded?.Invoke(null, activeMovie);
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Imdb Recomended";
            tempThred.Thread.Start();
        }


        // DONT USE  https://www1.moviesjoy.net/search/ THEY USE GOOGLE RECAPTCH TO GET LINKS
        // DONT USE https://gostream.site/iron-man/ THEY HAVE DDOS PROTECTION

        public static void GetRealTrailerLinkFromImdbSingle(string url, int index, TempThred tempThred) // LOOK AT https://www.imdb.com/title/tt4508902/trailers/;; ///video/imdb/vi3474439449
        {
            url = url.Replace("video/imdb", "videoplayer");
            string d = GetHTML(url);
            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
            string key = FindHTML(d, "playbackDataKey\":[\"", "\"");
            d = GetHTML("https://www.imdb.com/ve/data/VIDEO_PLAYBACK_DATA?key=" + key);
            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

            string realURL = FindHTML(d, "\"video/mp4\",\"url\":\"", "\"");
            try {
                Trailer t = activeMovie.title.trailers[index];
                activeMovie.title.trailers[index] = new Trailer() { Name = t.Name, PosterUrl = t.PosterUrl, Url = realURL };

            }
            catch (Exception) {
                return;
            }
        }

        public static void GetRealTrailerLinkFromImdb(bool purgeCurrentTrailerThread = false) // LOOK AT https://www.imdb.com/title/tt4508902/trailers/;; ///video/imdb/vi3474439449
        {
            if (purgeCurrentTrailerThread) {
                PurgeThreds(5);
            }
            TempThred tempThred = new TempThred();
            tempThred.typeId = 5; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {

                    string d = DownloadString("https://www.imdb.com/title/" + activeMovie.title.id + "/trailers/");
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    print(d);
                    const string lookfor = "viconst=\"";
                    int index = 0;
                    while (d.Contains(lookfor)) {
                        string viUrl = FindHTML(d, lookfor, "\"");
                        string poster = FindHTML(d, "loadlate=\"", "\"");
                        string rep = FindHTML(poster, "._", "_.");
                        poster = poster.Replace("._" + rep + "_", "._V1_UY1000_UX1000_AL_");

                        d = RemoveOne(d, lookfor);
                        string name = FindHTML(d, "class=\"video-modal\" >", "<", decodeToNonHtml: true);
                        var cT = new Trailer() { Name = name, PosterUrl = poster, Url = "" };
                        if (activeMovie.title.trailers == null) return;
                        if (activeMovie.title.trailers.Count > index) {
                            activeMovie.title.trailers[index] = cT;
                        }
                        else {
                            activeMovie.title.trailers.Add(cT);
                        }

                        GetRealTrailerLinkFromImdbSingle("https://imdb.com/video/imdb/" + viUrl, index, tempThred);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        print("TRAILER::" + viUrl + "|" + name + "|" + poster);

                        index++;
                        trailerLoaded?.Invoke(null, activeMovie.title.trailers);

                        print(viUrl + "|" + name);
                    }

                    /*
                    url = url.Replace("video/imdb", "videoplayer");
                    string d = GetHTML(url);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                    string realTrailerUrl = FindHTML(d, "videoUrl\":\"", "\"");

                    for (int i = 0; i < 10; i++) {
                        realTrailerUrl = RemoveOne(realTrailerUrl, "\\u002F");
                    }
                    try {
                        realTrailerUrl = realTrailerUrl.Substring(5, realTrailerUrl.Length - 5);
                        realTrailerUrl = ("https://imdb-video.media-imdb.com/" + (url.Substring(url.IndexOf("/vi") + 1, url.Length - url.IndexOf("/vi") - 1)) + "/" + realTrailerUrl).Replace("/videoplayer", "");
                        activeTrailer = realTrailerUrl;
                        trailerLoaded?.Invoke(null, realTrailerUrl);
                    }
                    catch (Exception) {

                    }*/

                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Trailer";
            tempThred.Thread.Start();
        }

        public static void GetImdbEpisodes(int season = 1, bool purgeCurrentSeasonThread = true)
        {
            if (purgeCurrentSeasonThread) {
                PurgeThreds(6);
            }
            if (activeMovie.title.movieType == MovieType.Anime || activeMovie.title.movieType == MovieType.TVSeries) {
                TempThred tempThred = new TempThred();
                tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        string url = "https://www.imdb.com/title/" + activeMovie.title.id + "/episodes/_ajax?season=" + season;
                        print("URLLLLL::::" + url);
                        string d = DownloadString(url, tempThred);

                        // SEE https://www.imdb.com/title/tt0388629/episodes
                        if (d == "") {
                            print("FAILED TO GET EPISODES:::");
                            string _d = DownloadString("https://www.imdb.com/title/" + activeMovie.title.id + "/episodes");
                            string fromTo = FindHTML(_d, "<select id=\"byYear\"", "</select>");
                            List<string> years = new List<string>();
                            const string lookFor = "<option  value=\"";
                            while (fromTo.Contains(lookFor)) {
                                years.Add(FindHTML(fromTo, lookFor, "\""));
                                fromTo = RemoveOne(fromTo, lookFor);
                            }
                            for (int i = 0; i < years.Count; i++) {
                                //https://www.imdb.com/title/tt0388629/episodes/?year=2020
                                string partURL = "https://www.imdb.com/title/" + activeMovie.title.id + "/episodes/_ajax?year=" + years[i];
                                print("PARTURLL:::" + partURL);
                                d += DownloadString(partURL);
                            }
                            //<label for="byYear">Year:</label>
                        }
                        print(d);
                        if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                        int eps = 0;
                        //https://www.imdb.com/title/tt4508902/episodes/_ajax?season=2

                        for (int q = 1; q < 2000; q++) {
                            if (d.Contains("?ref_=ttep_ep" + q)) {
                                eps = q;
                            }
                            else {
                                break;
                            }
                        }
                        print("EPPSPPS:" + eps);
                        if (activeMovie.title.movieType == MovieType.Anime) {
                            while (!activeMovie.title.MALData.done) {
                                Thread.Sleep(100);
                                if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                            }
                            //string _d = DownloadString("");
                        }

                        activeMovie.episodes = new List<Episode>();

                        for (int q = 1; q <= eps; q++) {
                            string lookFor = "?ref_=ttep_ep" + q;
                            try {
                                d = d.Substring(d.IndexOf(lookFor), d.Length - d.IndexOf(lookFor));
                                string name = FindHTML(d, "title=\"", "\"", decodeToNonHtml: true);
                                string id = FindHTML(d, "div data-const=\"", "\"");
                                string rating = FindHTML(d, "<span class=\"ipl-rating-star__rating\">", "<");
                                string descript = FindHTML(d, "<div class=\"item_description\" itemprop=\"description\">", "<", decodeToNonHtml: true).Replace("\n", "").Replace("  ", "");
                                string date = FindHTML(d, "<div class=\"airdate\">", "<").Replace("\n", "").Replace("  ", "");
                                string posterUrl = FindHTML(d, "src=\"", "\"");

                                //print("ADDED EP::::" + name + "|" + q);

                                if (posterUrl == "https://m.media-amazon.com/images/G/01/IMDb/spinning-progress.gif" || posterUrl.Replace(" ", "") == "") {
                                    posterUrl = VIDEO_IMDB_IMAGE_NOT_FOUND; // DEAFULT LOADING
                                }

                                if (descript == "Know what this is about?") {
                                    descript = "";
                                }
                                activeMovie.episodes.Add(new Episode() { date = date, name = name, description = descript, rating = rating, posterUrl = posterUrl, id = id });

                            }
                            catch (Exception) {

                            }
                        }
                        //print(activeMovie.title.MALData.japName + "<<<<<<<<<<<<<<<<<<<<<<<<");
                        //     https://www9.gogoanime.io/category/mix-meisei-story
                        print("EPLOADED::::::");
                        episodeLoaded?.Invoke(null, activeMovie.episodes);
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "Season Info";
                tempThred.Thread.Start();
            }
            else {
                Episode ep = new Episode() { name = activeMovie.title.name };
                activeMovie.episodes = new List<Episode>();
                activeMovie.episodes.Add(ep);
                episodeLoaded?.Invoke(null, activeMovie.episodes);
            }
        }



        /// <summary>
        /// RETURN SUBTITLE STRING
        /// </summary>
        /// <param name="imdbTitleId"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string DownloadSubtitle(string imdbTitleId, string lang = "eng", bool showToast = true)
        {
            try {
                string rUrl = "https://www.opensubtitles.org/en/search/sublanguageid-" + lang + "/imdbid-" + imdbTitleId + "/sort-7/asc-0"; // best match first
                //print(rUrl);
                string d = DownloadString(rUrl);
                if (d.Contains("<div class=\"msg warn\"><b>No results</b> found, try")) {
                    return "";
                }
                string _found = FindHTML(d, "en/subtitles/", "\'");
                if (_found == "") return "";
                string _url = "https://www.opensubtitles.org/" + lang + "/subtitles/" + _found;

                d = DownloadString(_url);
                const string subAdd = "https://dl.opensubtitles.org/en/download/file/";
                string subtitleUrl = subAdd + FindHTML(d, "download/file/", "\"");
                if (subtitleUrl != subAdd) {
                    string s = HTMLGet(subtitleUrl, "https://www.opensubtitles.org");
                    if (BAN_SUBTITLE_ADS) {
                        List<string> bannedLines = new List<string>() { "Support us and become VIP member", "to remove all ads from www.OpenSubtitles.org", "to remove all ads from OpenSubtitles.org", "Advertise your product or brand here", "contact www.OpenSubtitles.org today" }; // No advertisement
                        foreach (var banned in bannedLines) {
                            s = s.Replace(banned, "");
                        }
                    }
                    s = s.Replace("\n\n", "");
                    if (!s.StartsWith("WEBVTT")) {
                        s = "WEBVTT\n\n" + s; // s.Insert(0, "WEBVTT\n");
                    }
                    if (showToast) {
                        App.ShowToast("Subtitles Downloaded");
                    }
                    return s;
                }
                else {
                    return "";
                }
            }
            catch (Exception) {
                return "";
            }
        }

        public static int GetMaxEpisodesInAnimeSeason(Movie currentMovie, int currentSeason, bool isDub, TempThred? tempThred = null)
        {
            int currentMax = 0;
            for (int i = 0; i < animeProviders.Length; i++) {
                int cmax = animeProviders[i].GetLinkCount(currentMovie, currentSeason, isDub, tempThred);
                if (cmax > currentMax) {
                    currentMax = cmax;
                }
            }
            return currentMax;
        }



        public static void DownloadSubtitlesAndAdd(string lang = "eng", bool isEpisode = false, int episodeCounter = 0)
        {
            if (!globalSubtitlesEnabled) { return; }

            TempThred tempThred = new TempThred();
            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    string id = activeMovie.title.id;
                    if (isEpisode) {
                        id = activeMovie.episodes[episodeCounter].id;
                    }

                    string _subtitleLoc = DownloadSubtitle(id, lang);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    bool contains = false;
                    if (activeMovie.subtitles == null) {
                        activeMovie.subtitles = new List<Subtitle>();
                    }

                    for (int i = 0; i < activeMovie.subtitles.Count; i++) {
                        if (activeMovie.subtitles[i].name == lang) {
                            contains = true;
                        }
                    }
                    if (!contains) {
                        print("CURRENT SUBFILE:" + _subtitleLoc);
                        activeMovie.subtitles.Add(new Subtitle() { name = lang, data = _subtitleLoc });
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "SubtitleThread";
            tempThred.Thread.Start();
        }

        static bool LookForFembedInString(TempThred tempThred, int normalEpisode, string d)
        {
            string source = "https://www.fembed.com";
            string _ref = "www.fembed.com";

            print("FMEMEDOSOOS:" + d);



            string fembed = FindHTML(d, "data-video=\"https://www.fembed.com/v/", "\"");
            if (fembed == "") {
                fembed = FindHTML(d, "data-video=\"https://gcloud.live/v/", "\"");
                if (fembed != "") {
                    source = "https://gcloud.live";
                    _ref = "www.gcloud.live";
                }
            }
            if (fembed != "") {
                GetFembed(fembed, tempThred, normalEpisode, source, _ref);
            }
            const string lookFor = "file: \'";
            int prio = 5;
            while (d.Contains(lookFor)) {
                string ur = FindHTML(d, lookFor, "\'");
                d = RemoveOne(d, lookFor);
                string label = FindHTML(d, "label: \'", "\'").Replace("hls P", "live").Replace(" P", "p");
                prio--;
                AddPotentialLink(normalEpisode, ur, "Fembed " + label, prio);
            }
            return fembed != "";
        }

        static int Random(int min, int max)
        {
            return rng.Next(min, max);
        }

        static void AddEpisodesFromMirrors(TempThred tempThred, string d, int normalEpisode) // DONT DO THEVIDEO provider, THEY USE GOOGLE CAPTCH TO VERIFY AUTOR; LOOK AT https://vev.io/api/serve/video/qy3pw89xwmr7 IT IS A POST REQUEST
        {
            string mp4 = "https://www.mp4upload.com/embed-" + FindHTML(d, "data-video=\"https://www.mp4upload.com/embed-", "\"");
            if (mp4 != "https://www.mp4upload.com/embed-") {
                try {
                    string _d = DownloadString(mp4, tempThred);
                    if (!GetThredActive(tempThred)) { return; };
                    string mxLink = Getmp4UploadByFile(_d);
                    AddPotentialLink(normalEpisode, mxLink, "Mp4Upload", 9);
                }
                catch (System.Exception) {
                }
            }
            string __d = d.ToString();
            const string lookFor = "https://redirector.googlevideo.com/";
            int prio = 11;
            while (__d.Contains(lookFor)) {
                prio++;
                __d = "|:" + RemoveOne(__d, lookFor);
                string all = FindHTML(__d, "|", "}");
                string url = FindHTML(all, ":", "\'");
                string label = FindHTML(all, "label: \'", "\'").Replace(" P", "p");
                AddPotentialLink(normalEpisode, "h" + url, "GoogleVideo " + label, prio);
            }
            bool fembedAdded = LookForFembedInString(tempThred, normalEpisode, d);

            string nameId = "Vidstreaming";
            string vid = FindHTML(d, "data-video=\"//vidstreaming.io/streaming.php?", "\"");
            string beforeId = "https://vidstreaming.io/download?id=";
            if (vid == "") {
                vid = FindHTML(d, "//vidstreaming.io/streaming.php?", "\"");
            }
            if (vid == "") {
                vid = FindHTML(d, "//vidnode.net/load.php?id=", "\"");
                if (vid != "") {
                    beforeId = "https://vidnode.net/download?id=";
                    nameId = "VidNode";
                }
            }
            if (vid == "") {
                vid = FindHTML(d, "//vidnode.net/streaming.php?id=", "\"");
                if (vid != "") {
                    beforeId = "https://vidnode.net/download?id=";
                    nameId = "VidNode";
                }
            }

            if (vid == "") {
                vid = FindHTML(d, "//vidcloud9.com/download?id=", "\"");

                if (vid != "") {
                    beforeId = "https://vidcloud9.com/download?id=";
                    nameId = "VidCloud";
                }
            }
            print(">>STREAM::" + vid);
            if (vid != "") {
                string dLink = beforeId + vid.Replace("id=", "");
                string _d = DownloadString(dLink, tempThred);

                //https://gcloud.live/v/ky5g0h3zqylzmq4#caption=https://xcdnfile.com/sub/iron-man-hd-720p/iron-man-hd-720p.vtt

                if (!GetThredActive(tempThred)) { return; };

                GetVidNode(_d, normalEpisode, nameId);
                // if (!fembedAdded) {
                string fMds = dLink.Replace("download", "streaming.php");
                print("FMEMEDST: " + fMds);
                string ___d = DownloadString(fMds, tempThred);
                if (!GetThredActive(tempThred)) { return; };
                LookForFembedInString(tempThred, normalEpisode, ___d);
                // }



                /* // OLD CODE, ONLY 403 ERROR DOSEN'T WORK ANYMORE
                vid = "http://vidstreaming.io/streaming.php?" + vid;
                string _d = DownloadString(vid); if (!GetThredActive(tempThred)) { return; };
                string mxLink = FindHTML(_d, "sources:[{file: \'", "\'");
                print("Browser: " + vid + " | RAW (NO ADS): " + mxLink);
                if (CheckIfURLIsValid(mxLink)) {
                    Episode ep = activeMovie.episodes[normalEpisode];
                    if (ep.links == null) {
                        activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating };
                    }
                    activeMovie.episodes[normalEpisode].links.Add(new Link() { priority = 0, url = mxLink, name = "Vidstreaming" }); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                    linkAdded?.Invoke(null, 2);

                }
                */
            }
            else {
                print("Error :(");
            }
        }

        public static void GetEpisodeLink(int episode = -1, int season = 1, bool purgeCurrentLinkThread = true, bool onlyEpsCount = false, bool isDub = true)
        {
            if (activeMovie.episodes == null) {
                return;
            }

            if (purgeCurrentLinkThread) {
                PurgeThreds(3);
            }

            TempThred tempThred = new TempThred();

            tempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {

                    string rinput = ToDown(activeMovie.title.name, replaceSpace: "+"); // THE URL SEARCH STRING

                    bool animeSeach = activeMovie.title.movieType == MovieType.Anime && ANIME_ENABLED; // || activeMovie.title.movieType == MovieType.AnimeMovie &&
                    bool movieSearch = activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie || activeMovie.title.movieType == MovieType.TVSeries;


                    // --------- CLEAR EPISODE ---------
                    int normalEpisode = episode == -1 ? 0 : episode - 1;                     //normalEp = ep-1;

                    activeMovie.subtitles = new List<Subtitle>(); // CLEAR SUBTITLES
                    DownloadSubtitlesAndAdd(isEpisode: (activeMovie.title.movieType == MovieType.TVSeries || activeMovie.title.movieType == MovieType.Anime), episodeCounter: normalEpisode); // CHANGE LANG TO USER SETTINGS


                    if (activeMovie.episodes.Count <= normalEpisode) { activeMovie.episodes.Add(new Episode()); }
                    Episode cEpisode = activeMovie.episodes[normalEpisode];
                    activeMovie.episodes[normalEpisode] = new Episode() {
                        links = new List<Link>(),
                        posterUrl = cEpisode.posterUrl,
                        rating = cEpisode.rating,
                        name = cEpisode.name,
                        date = cEpisode.date,
                        description = cEpisode.description,
                        id = cEpisode.id,
                    };

                    if (animeSeach) { // use https://www3.gogoanime.io/ or https://vidstreaming.io/

                        while (!activeMovie.title.MALData.done) {
                            Thread.Sleep(100);
                            if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                        }
                        /*
                        for (int i = 0; i < animeProviders.Length; i++) {
                            animeProviders[i].LoadLinksTSync(episode, season, normalEpisode, isDub);
                        }*/
                        TempThred temp = new TempThred();
                        temp.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                        temp.Thread = new System.Threading.Thread(() => { });
                        temp.Thread.Name = "Testing";
                        Parallel.For(0, animeProviders.Length, (int i) => {
                            animeProviders[i].LoadLinksTSync(episode, season, normalEpisode, isDub, temp);
                            print("LOADED DONE:::: " + animeProviders[i].Name);
                        });
                        JoinThred(temp);

                        /*
                        int _episode = int.Parse(episode.ToString()); // READ ONLY

                        if (isDub) {
                            TempThred tempthread = new TempThred();
                            tempthread.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                            tempthread.Thread = new System.Threading.Thread(() => {
                                try {
                                    print("DUBBED::" + episode + "|" + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum());
                                    if (episode <= activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Sum()) {
                                        List<string> fwords = GetAllDubbedAnimeLinks(activeMovie, season);
                                        print("SLUG1." + fwords[0]);
                                        int sel = -1;
                                        int floor = 0;
                                        int subtract = 0;
                                        if (activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason != null) {
                                            for (int i = 0; i < activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason.Count; i++) {
                                                int seling = floor + activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];

                                                if (episode > floor && episode <= seling) {
                                                    sel = i;
                                                    subtract = floor;

                                                }
                                                //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                                                floor += activeMovie.title.MALData.currentActiveDubbedMaxEpsPerSeason[i];
                                            }
                                        }

                                        string fwordLink = fwords[sel];
                                        print("SLUGOS: " + fwordLink);
                                        DubbedAnimeEpisode dubbedEp = GetDubbedAnimeEpisode(fwordLink, _episode - subtract);

                                        string serverUrls = dubbedEp.serversHTML;
                                        print("SERVERURLLRL:" + serverUrls);
                                        const string sLookFor = "hl=\"";
                                        while (serverUrls.Contains(sLookFor)) {
                                            string baseUrl = FindHTML(dubbedEp.serversHTML, "hl=\"", "\"");
                                            print("BASE::" + baseUrl);
                                            string burl = "https://bestdubbedanime.com/xz/api/playeri.php?url=" + baseUrl + "&_=" + UnixTime;
                                            print(burl);
                                            string _d = DownloadString(burl);
                                            print("SSC:" + _d);
                                            int prio = -10; // SOME LINKS ARE EXPIRED, CAUSING VLC TO EXIT

                                            string enlink = "\'";
                                            if (_d.Contains("<source src=\"")) {
                                                enlink = "\"";
                                            }
                                            string lookFor = "<source src=" + enlink;
                                            while (_d.Contains(lookFor)) {
                                                string vUrl = FindHTML(_d, lookFor, enlink);
                                                if (vUrl != "") {
                                                    vUrl = "https:" + vUrl;
                                                }
                                                string label = FindHTML(_d, "label=" + enlink, enlink);
                                                print("DUBBEDANIMECHECK:" + vUrl + "|" + label);
                                                //if (GetFileSize(vUrl) > 0) {
                                                AddPotentialLink(normalEpisode, vUrl, "DubbedAnime " + label.Replace("0p", "0") + "p", prio);
                                                //}

                                                _d = RemoveOne(_d, lookFor);
                                                _d = RemoveOne(_d, "label=" + enlink);
                                            }
                                            serverUrls = RemoveOne(serverUrls, sLookFor);
                                        }
                                    }
                                }
                                finally {
                                    JoinThred(tempthread);
                                }
                            });
                            tempthread.Thread.Name = "DubAnime Thread";
                            tempthread.Thread.Start();

                        }

                        var kickAssLinks = GetAllKickassLinksFromAnime(activeMovie, season, isDub);
                        print("KICKASSOS:" + normalEpisode);
                        for (int i = 0; i < kickAssLinks.Count; i++) {
                            print("KICKASSLINK:" + i + ". |" + kickAssLinks[i]);
                        }
                        if (normalEpisode < kickAssLinks.Count) {
                            GetKickassVideoFromURL(kickAssLinks[normalEpisode], normalEpisode);
                        }

                        try {
                            if (episode <= activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Sum()) {
                                string fwordLink = "";
                                List<string> fwords = GetAllGogoLinksFromAnime(activeMovie, season, isDub);
                                // for (int i = 0; i < fwords.Count; i++) {
                                // print("FW: " + fwords[i]);
                                //  }

                                // --------------- GET WHAT SEASON THE EPISODE IS IN ---------------

                                int sel = -1;
                                int floor = 0;
                                int subtract = 0;
                                // print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason);
                                if (activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason != null) {
                                    for (int i = 0; i < activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason.Count; i++) {
                                        int seling = floor + activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];

                                        if (episode > floor && episode <= seling) {
                                            sel = i;
                                            subtract = floor;

                                        }
                                        //print(activeMovie.title.MALData.currentActiveMaxEpsPerSeason[i] + "<<");
                                        floor += activeMovie.title.MALData.currentActiveGoGoMaxEpsPerSeason[i];
                                    }
                                }
                                //print("sel: " + sel);
                                if (sel != -1) {
                                    try {
                                        fwordLink = fwords[sel].Replace("-dub", "") + (isDub ? "-dub" : "");
                                    }
                                    catch (Exception) {

                                    }
                                }

                                if (fwordLink != "") { // IF FOUND
                                    string dstring = "https://www3.gogoanime.io/" + fwordLink + "-episode-" + (_episode - subtract);
                                    print("DSTRING: " + dstring);
                                    string d = DownloadString(dstring, tempThred);

                                    AddEpisodesFromMirrors(tempThred, d, normalEpisode);
                                }
                            }
                        }
                        catch (Exception) {
                            print("GOGOANIME ERROR");
                        }
                        */

                    }
                    if (movieSearch) { // use https://movies123.pro/

                        // --------- SETTINGS ---------

                        bool canMovie = GetSettings(MovieType.Movie);
                        bool canShow = GetSettings(MovieType.TVSeries);

                        // -------------------- HD MIRRORS --------------------

                        bool isMovie = activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie;

                        TempThred temp = new TempThred();
                        temp.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                        temp.Thread = new System.Threading.Thread(() => { });
                        temp.Thread.Name = "Testing";
                        Parallel.For(0, movieProviders.Length, (int i) => {
                            movieProviders[i].LoadLinksTSync(episode, season, normalEpisode, isMovie, temp);
                            print("LOADED DONE:::: " + movieProviders[i].GetType().Name);
                        });
                        JoinThred(temp);



                        /*
                        for (int i = 0; i < movieProviders.Length; i++) {
                            movieProviders[i].LoadLinksTSync(episode, season, normalEpisode, isMovie, temp);
                        }*/

                        /*
                        if (isMovie) {
                            AddFastMovieLink(normalEpisode);
                            AddFastMovieLink2(normalEpisode);
                        }
                        else if (activeMovie.title.movieType == MovieType.TVSeries) {
                            GetTMDB(episode, season, normalEpisode);
                            GetWatchTV(season, episode, normalEpisode);
                        }
                        GetFmoviesLinks(normalEpisode, episode, season);
                        GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://movies123.live");
                        GetLiveMovies123Links(normalEpisode, episode, season, isMovie, "https://c123movies.com");
                        GetThe123movies(normalEpisode, episode, season, isMovie);

                        if (activeMovie.title.yesmoviessSeasonDatas != null) {
                            for (int i = 0; i < activeMovie.title.yesmoviessSeasonDatas.Count; i++) {
                                //     print(activeMovie.title.yesmoviessSeasonDatas[i].id + "<-IDS:" + season);
                                if (activeMovie.title.yesmoviessSeasonDatas[i].id == (isMovie ? 1 : season)) {
                                    YesMovies(normalEpisode, activeMovie.title.yesmoviessSeasonDatas[i].url);
                                }
                            }
                        }
                        GetLinksFromWatchSeries(season, normalEpisode);
                        if (GOMOSTEAM_ENABLED) {
                            TempThred minorTempThred = new TempThred();
                            minorTempThred.typeId = 3; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                            minorTempThred.Thread = new System.Threading.Thread(() => {
                                try {
                                    string find = activeMovie.title.name.ToLower() + (activeMovie.title.movieType == MovieType.TVSeries ? "-season-" + season : "");
                                    find = find.Replace("\'", "-");
                                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                                    find = rgx.Replace(find, "");


                                    find = find.Replace(" - ", "-").Replace(" ", "-");

                                    if (activeMovie.title.movieType == MovieType.TVSeries) { // ADD CORRECT FORMAT; https://gomostream.com/show/game-of-thrones/01-01
                                        find = find.Replace("-season-", "/");

                                        for (int i = 0; i < 10; i++) {
                                            if (find.EndsWith("/" + i)) {
                                                find = find.Replace("/" + i, "/0" + i);
                                            }
                                        }

                                        if (episode.ToString() != "-1") {
                                            find += "-" + episode;
                                        }

                                        for (int i = 0; i < 10; i++) {
                                            if (find.EndsWith("-" + i)) {
                                                find = find.Replace("-" + i, "-0" + i);
                                            }
                                        }
                                    }

                                    string gomoUrl = "https://" + GOMOURL + "/" + ((activeMovie.title.movieType == MovieType.Movie || activeMovie.title.movieType == MovieType.AnimeMovie) ? "movie" : "show") + "/" + find;
                                    print(gomoUrl);
                                    DownloadGomoSteam(gomoUrl, tempThred, normalEpisode);
                                }
                                finally {
                                    JoinThred(minorTempThred);
                                }
                            });
                            minorTempThred.Thread.Name = "Mirror Thread";
                            minorTempThred.Thread.Start();
                        }
                        if (SUBHDMIRROS_ENABLED) {
                            if (activeMovie.title.movies123MetaData.movieLink != null) {
                                if (activeMovie.title.movieType == MovieType.TVSeries) {
                                    int normalSeason = season - 1;
                                    List<Movies123SeasonData> seasonData = activeMovie.title.movies123MetaData.seasonData;
                                    // ---- TO PREVENT ERRORS START ----
                                    if (seasonData != null) {
                                        if (seasonData.Count > normalSeason) {
                                            if (seasonData[normalSeason].episodeUrls != null) {
                                                if (seasonData[normalSeason].episodeUrls.Count > normalEpisode) {
                                                    // ---- END ----
                                                    string fwordLink = seasonData[normalSeason].seasonUrl + "/" + seasonData[normalSeason].episodeUrls[normalEpisode];
                                                    print(fwordLink);
                                                    for (int f = 0; f < MIRROR_COUNT; f++) {
                                                        GetLinkServer(f, fwordLink, normalEpisode);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                else {
                                    for (int f = 0; f < MIRROR_COUNT; f++) {
                                        print(">::" + f);
                                        GetLinkServer(f, activeMovie.title.movies123MetaData.movieLink); // JUST GET THE MOVIE
                                    }
                                }
                            }

                        }*/
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Get Links";
            tempThred.Thread.Start();
        }

        static void GetVidNode(string _d, int normalEpisode, string urlName = "Vidstreaming")
        {
            string linkContext = FindHTML(_d, "<h6>Link download</h6>", " </div>");
            const string lookFor = "href=\"";
            string rem = "<div class=<\"dowload\"><a";
            linkContext = RemoveOne(linkContext, rem);
            int prio = 0;
            while (linkContext.Contains(lookFor)) {
                string link = FindHTML(linkContext, lookFor, "\"");
                string _nameContext = FindHTML(linkContext, link, "</a></div>") + "</a></div>";
                string name = urlName + " (" + FindHTML(_nameContext, "            (", "</a></div>");
                link = link.Replace("&amp;", "&");

                print("LINK: " + link + "|" + name);
                name = name.Replace("(", "").Replace(")", "").Replace("mp4", "").Replace("orginalP", "Source").Replace("-", "").Replace("0P", "0p");

                if (CheckIfURLIsValid(link)) {
                    prio++;
                    /*
                    Episode ep = activeMovie.episodes[normalEpisode];
                    if (ep.links == null) {
                        activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                    }
                    activeMovie.episodes[normalEpisode].links.Add(new Link() { priority = prio, url = link, name = name }); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                    linkAdded?.Invoke(null, 1);*/
                    AddPotentialLink(normalEpisode, link, name, prio);
                }
                linkContext = RemoveOne(linkContext, lookFor);
            }
        }

        public static void GetFembed(string fembed, TempThred tempThred, int normalEpisode, string urlType = "https://www.fembed.com", string referer = "www.fembed.com")
        {
            if (fembed != "") {
                int prio = 10;
                string _d = PostRequest(urlType + "/api/source/" + fembed, urlType + "/v/" + fembed, "r=&d=" + referer, tempThred);
                if (_d != "") {
                    const string lookFor = "\"file\":\"";
                    string _labelFind = "\"label\":\"";
                    while (_d.Contains(_labelFind)) {
                        string link = FindHTML(_d, lookFor, "\",\"");

                        //  d = RemoveOne(d, link);
                        link = link.Replace("\\/", "/");

                        string label = FindHTML(_d, _labelFind, "\"");
                        print(label + "|" + link);
                        if (CheckIfURLIsValid(link)) {
                            prio++;
                            AddPotentialLink(normalEpisode, link, "XStream " + label, prio);
                        }
                        _d = RemoveOne(_d, _labelFind);
                    }
                }
            }
        }

        static void GetMovieTv(int episode, string d, TempThred tempThred) // https://1movietv.com/1movietv-streaming-api/ 
        {
            if (d != "") {
                string find = FindHTML(d, "src=\"https://myvidis.top/v/", "\"");
                int prio = 0;
                if (find != "") {
                    string _d = PostRequest("https://myvidis.top/api/source/" + find, "https://myvidis.top/v/" + find, "", tempThred);
                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
                    if (_d != "") {
                        const string lookFor = "\"file\":\"";
                        string _labelFind = "\"label\":\"";
                        while (_d.Contains(_labelFind)) {
                            string link = FindHTML(_d, lookFor, "\",\"");
                            //  d = RemoveOne(d, link);
                            link = link.Replace("\\/", "/");

                            string label = FindHTML(_d, _labelFind, "\"");
                            print(label + "|" + link);
                            if (CheckIfURLIsValid(link)) {
                                prio++;
                                AddPotentialLink(episode, link, "MovieTv " + label, prio);
                            }
                            _d = RemoveOne(_d, _labelFind);
                        }
                    }
                }
            }
        }

        public static double GetFileSize(string url)
        {
            try {
                var webRequest = HttpWebRequest.Create(new System.Uri(url));
                webRequest.Method = "HEAD";
                print("RESPONSEGET:");
                webRequest.Timeout = 10000;
                using (var webResponse = webRequest.GetResponse()) {
                    print("RESPONSE:");
                    var fileSize = webResponse.Headers.Get("Content-Length");
                    var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / Math.Pow((double)App.GetSizeOfJumpOnSystem(), 2.0), 2);
                    print("GETFILESIZE: " + fileSizeInMegaByte);

                    return fileSizeInMegaByte;
                }
            }
            catch (Exception) {
                print("ERRORGETFILESIZE: " + url);
                return -1;
            }
        }

        public static double GetFileSizeOnSystem(string path)
        {
            try {
                return Math.Round(Convert.ToDouble(GetFileBytesOnSystem(path)) / Math.Pow((double)App.GetSizeOfJumpOnSystem(), 2.0), 2);
            }
            catch (Exception) {
                return -1;
            }
        }

        public static long GetFileBytesOnSystem(string path)
        {
            try {
                return new System.IO.FileInfo(path).Length;
            }
            catch (Exception) {
                return -1;
            }
        }

        public static bool GetSettings(MovieType type = MovieType.Movie)
        {
            return true;
        }

        public static void AddToActiveSearchResults(Poster p)
        {
            if (!activeSearchResults.Contains(p)) {

                bool add = true;
                for (int i = 0; i < activeSearchResults.Count; i++) {
                    if (activeSearchResults[i].posterUrl == p.posterUrl) {
                        add = false;
                    }
                }
                if (add) {
                    //print(p.name + "|" + p.posterUrl);
                    activeSearchResults.Add(p);
                    addedSeachResult?.Invoke(null, p);
                }
            }
        }

        /* ------------------------------------------------ EXAMPLE THRED ------------------------------------------------

            TempThred tempThred = new TempThred();
            tempThred.typeId = 1; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() =>
            {
                try {


                    if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "QuickSearch";
            tempThred.Thread.Start();


            */

        public static string ConvertIMDbImagesToHD(string nonHDImg, int? pwidth = null, int? pheight = null, double multi = 1)
        {
            string img = FindHTML("|" + nonHDImg, "|", "._");
            pheight = (int)Math.Round((pheight ?? 0) * posterRezMulti * multi);
            pwidth = (int)Math.Round((pwidth ?? 0) * posterRezMulti * multi);
            pheight = App.ConvertDPtoPx((int)pheight);
            pwidth = App.ConvertDPtoPx((int)pwidth);
            if (pwidth == 0 && pheight == 0) return nonHDImg;
            img += "." + (pheight > 0 ? "_UY" + pheight : "") + (pwidth > 0 ? "UX" + pwidth : "") + "_.jpg";
            return img;
        }

        // -------------------- METHODS --------------------
        static string HTMLGet(string uri, string referer, bool br = false)
        {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                request.Method = "GET";
                request.ContentType = "text/html; charset=UTF-8";
                // webRequest.Headers.Add("Host", "trollvid.net");
                request.UserAgent = USERAGENT;
                request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");
                request.Referer = referer;

                // webRequest.Headers.Add("Cookie", "__cfduid=dc6e854c3f07d2a427bca847e1ad5fa741562456483; _ga=GA1.2.742704858.1562456488; _gid=GA1.2.1493684150.1562456488; _maven_=popped; _pop_=popped");
                request.Headers.Add("TE", "Trailers");

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    //  print(response.GetResponseHeader("set-cookie").ToString());


                    // using (Stream stream = response.GetResponseStream())
                    if (br) {
                        /*
                        using (BrotliStream bs = new BrotliStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress)) {
                            using (System.IO.MemoryStream msOutput = new System.IO.MemoryStream()) {
                                bs.CopyTo(msOutput);
                                msOutput.Seek(0, System.IO.SeekOrigin.Begin);
                                using (StreamReader reader = new StreamReader(msOutput)) {
                                    string result = reader.ReadToEnd();

                                    return result;

                                }
                            }
                        }
                        */
                        return "";
                    }
                    else {
                        using (Stream stream = response.GetResponseStream()) {
                            // print("res" + response.StatusCode);
                            foreach (string e in response.Headers) {
                                // print("Head: " + e);
                            }
                            // print("LINK:" + response.GetResponseHeader("Set-Cookie"));
                            using (StreamReader reader = new StreamReader(stream)) {
                                string result = reader.ReadToEnd();
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// WHEN DOWNLOADSTRING DOSNE'T WORK, BASILCY SAME THING, BUT CAN ALSO BE USED TO FORCE ENGLISH
        /// </summary>
        /// <param name="url"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public static string GetHTML(string url, bool en = true)
        {
            string html = string.Empty;

            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                // List<string> heads = new List<string>(); // HEADERS
                /*
                heads = HeadAdd("");
                for (int i = 0; i < heads.Count; i++) {
                    try {
                        request.Headers.Add(HeadToRes(heads[i], 0), HeadToRes(heads[i], 1));
                        print("PRO:" + HeadToRes(heads[i], 0) + ": " + HeadToRes(heads[i], 1));

                    }
                    catch (Exception) {

                    }
                }
                */
                WebHeaderCollection myWebHeaderCollection = request.Headers;
                if (en) {
                    myWebHeaderCollection.Add("Accept-Language", "en;q=0.8");
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.UserAgent = USERAGENT;
                request.Referer = url;
                //request.AddRange(1212416);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())

                using (StreamReader reader = new StreamReader(stream)) {
                    html = reader.ReadToEnd();
                }
                return html;
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// WHEN DOWNLOADSTRING DOSNE'T WORK, BASILCY SAME THING, BUT CAN ALSO BE USED TO FORCE ENGLISH
        /// </summary>
        /// <param name="url"></param>
        /// <param name="en"></param>
        /// <returns></returns>
        public static async Task<string> GetHTMLAsync(string url, bool en = true)
        {
            string html = string.Empty;
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                WebHeaderCollection myWebHeaderCollection = request.Headers;
                if (en) {
                    myWebHeaderCollection.Add("Accept-Language", "en;q=0.8");
                }
                request.AutomaticDecompression = DecompressionMethods.GZip;
                request.UserAgent = USERAGENT;
                request.Referer = url;
                //request.AddRange(1212416);

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())

                using (StreamReader reader = new StreamReader(stream)) {
                    html = reader.ReadToEnd();
                }
                return html;
            }
            catch (Exception) {
                return "";
            }
        }


        public static bool AddPotentialLink(int normalEpisode, string _url, string _name, int _priority)
        {
            if (activeMovie.episodes == null) return false;
            if (_url == "http://error.com") return false; // ERROR
            if (_url.Replace(" ", "") == "") return false;

            _name = _name.Replace("  ", " ");
            _url = _url.Replace(" ", "%20");
            try {
                if (!LinkListContainsString(activeMovie.episodes[normalEpisode].links, _url)) {
                    if (CheckIfURLIsValid(_url)) {
                        // if (GetFileSize(_url) > 0) {
                        print("ADD LINK:" + normalEpisode + "|" + _name + "|" + _priority + "|" + _url);
                        Episode ep = activeMovie.episodes[normalEpisode];
                        if (ep.links == null) {
                            activeMovie.episodes[normalEpisode] = new Episode() { links = new List<Link>(), date = ep.date, description = ep.description, name = ep.name, posterUrl = ep.posterUrl, rating = ep.rating, id = ep.id };
                            ep = activeMovie.episodes[normalEpisode];
                        }

                        bool done = false;
                        int count = 1;
                        string realName = _name;
                        while (!done && !realName.Contains("[MIRRORCOUNTER]")) {
                            count++;
                            done = true;
                            for (int i = 0; i < ep.links.Count; i++) {
                                if (ep.links[i].name == realName) {
                                    realName = _name + " (Mirror " + count + ")";
                                    done = false;
                                    break;
                                }
                            }
                        }
                        realName = realName.Replace("  ", " ");
                        var link = new Link() { priority = _priority, url = _url, name = realName };
                        activeMovie.episodes[normalEpisode].links.Add(link); // [MIRRORCOUNTER] IS LATER REPLACED WITH A NUMBER TO MAKE IT EASIER TO SEPERATE THEM, CAN'T DO IT HERE BECAUSE IT MUST BE ABLE TO RUN SEPARETE THREADS AT THE SAME TIME
                        linkAdded?.Invoke(null, link);
                        return true;
                        //}
                    }
                }

            }
            catch (Exception) {
                return false;
            }
            return false;
        }

        public static DubbedAnimeEpisode GetDubbedAnimeEpisode(string slug, int eps)
        {
            string url = "https://bestdubbedanime.com/xz/v3/jsonEpi.php?slug=" + slug + "/" + eps + "&_=" + UnixTime;
            string d = DownloadString(url);
            print("GOTEP" + d);
            var f = JsonConvert.DeserializeObject<DubbedAnimeSearchRootObject>(d, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            if (f.result.error) {
                print("RETURNOS:ERROR");
                return new DubbedAnimeEpisode();
            }
            else {
                try {
                    return f.result.anime[0];
                }
                catch (Exception) {
                    print("RETURNOS:ERROR");
                    return new DubbedAnimeEpisode();
                }
            }
            /* print(d);

             const string smallAdd = "\": \"";
             string FastFind(string name)
             {
                 if (d.Contains(name + smallAdd + "\": null,")) {
                     return "";
                 }
                 string _f = FindHTML(d, name + smallAdd, "\",");
                 if (_f == "") {
                     _f = FindHTML(d, name + "\":", ",");
                 }
                 return _f;
             }
             int FastId(string name)
             {
                 try {
                     return int.Parse(FastFind(name));
                 }
                 catch (Exception) {
                     return -1;
                 }
             }

             DubbedAnimeEpisode e = new DubbedAnimeEpisode();
             try {
                 e.serversHTML = FastFind(nameof(e.serversHTML));

                 e.ep = FastId(nameof(e.ep));
                 e.Epviews = FastId(nameof(e.Epviews));
                 e.rowid = FastId(nameof(e.ep));
                 e.totalEp = FastId(nameof(e.totalEp));
                 e.year = FastId(nameof(e.year));
                 e.showid = FastId(nameof(e.showid));
                 e.TotalViews = FastId(nameof(e.TotalViews));

                 e.desc = FastFind(nameof(e.desc));
                 e.title = FastFind(nameof(e.title));
                 e.slug = FastFind(nameof(e.slug));
                 e.status = FastFind(nameof(e.status));
             }
             catch (Exception) {
                 print("!!!!!!DubbedAnimeEpisodeERROR");
             }
             return e;*/
        }

        public static int UnixTime { get { return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; } }



        /// <summary>
        /// GET IF URL IS VALID, null and "" will return false
        /// </summary>
        /// <param name="uriName"></param>
        /// <returns></returns>
        public static bool CheckIfURLIsValid(string uriName)
        {
            if (uriName == null) return false;
            if (uriName == "") return false;

            Uri uriResult;
            return Uri.TryCreate(uriName, UriKind.Absolute, out uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// RETURNS THE TRUE MX URL OF A MP4 UPLOAD
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        static string Getmp4UploadByFile(string result)
        {
            result = result.Replace("||||", "|");
            result = result.Replace("|||", "|");
            result = result.Replace("||", "|");
            result = result.Replace("||", "|");
            result = result.Replace("||", "|");

            string server = "s1";
            for (int i = 0; i < 100; i++) {
                if (result.Contains("|s" + i + "|")) {
                    server = "s" + i;
                }
            }

            for (int i = 0; i < 100; i++) {
                if (result.Contains("|www" + i + "|")) {
                    server = "www" + i;
                }
            }

            int pos = result.IndexOf("vid|mp4|download");
            int offset = 18;

            if (pos == -1) {
                offset = 9;
                pos = result.IndexOf("vid|mp4");
            }
            if (pos == -1) {
                pos = result.IndexOf("mp4|video");
                offset = 11;
            }

            if (pos == -1) {
                return "";
                /*
                if (_episode.Contains("This video is no longer available due to a copyright claim")) {
                    break;
                }
                */
            }

            string r = "-1";
            string allEp = result.Substring(pos + offset - 1, result.Length - pos - offset + 1);
            if ((allEp.Substring(0, 30).Contains("|"))) {
                string rez = allEp.Substring(0, allEp.IndexOf("p")) + "p";
                r = rez;
                allEp = allEp.Substring(allEp.IndexOf("p") + 2, allEp.Length - allEp.IndexOf("p") - 2);
            }
            string urlLink = allEp.Substring(0, allEp.IndexOf("|"));

            allEp = allEp.Substring(urlLink.Length + 1, allEp.Length - urlLink.Length - 1);
            string typeID = allEp.Substring(0, allEp.IndexOf("|"));

            string _urlLink = FindReverseHTML(result, "|" + typeID + "|", "|");

            string mxLink = "https://" + server + ".mp4upload.com:" + typeID + "/d/" + _urlLink + "/video.mp4"; //  282 /d/qoxtvtduz3b4quuorgvegykwirnmt3wm3mrzjwqhae3zsw3fl7ajhcdj/video.mp4

            string addRez = "";
            if (r != "-1") {
                addRez += " | " + r;
            }

            if (typeID != "282") {
                //Error
            }
            else {

            }
            return mxLink;
        }

        static string ReadDataMovie(string all, string inp)
        {
            try {
                string newS = all.Substring(all.IndexOf(inp) + (inp.Length + 2), all.Length - all.IndexOf(inp) - (inp.Length + 2));
                string ns = newS.Substring(0, newS.IndexOf("\""));
                return ns;
            }
            catch (Exception) {
                return "";
            }
        }

        public static string FindReverseHTML(string all, string first, string end, int offset = 0)
        {
            int x = all.IndexOf(first);
            all = all.Substring(0, x);
            int y = all.LastIndexOf(end) + end.Length;
            //  print(x + "|" + y);
            return all.Substring(y, all.Length - y);
        }

        /// <summary>
        /// REMOVES ALL SPECIAL CHARACTERS
        /// </summary>
        /// <param name="text"></param>
        /// <param name="toLower"></param>
        /// <param name="replaceSpace"></param>
        /// <returns></returns>
        public static string ToDown(string text, bool toLower = true, string replaceSpace = " ")
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            try {
                text = rgx.Replace(text, "");

            }
            catch (Exception) {
                return text;
            }
            if (toLower) {
                text = text.ToLower();
            }
            text = text.Replace(" ", replaceSpace);
            return text;
        }

        static string ForceLetters(int inp, int letters = 2)
        {
            int added = letters - inp.ToString().Length;
            if (added > 0) {
                return MultiplyString("0", added) + inp.ToString();
            }
            else {
                return inp.ToString();
            }
        }

        public static string MultiplyString(string s, int times)
        {
            return String.Concat(Enumerable.Repeat(s, times));
        }

        /// <summary>
        /// NETFLIX like time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertTimeToString(double time)
        {
            int sec = (int)Math.Round(time);
            int rsec = (sec % 60);
            int min = (int)Math.Ceiling((sec - rsec) / 60f);
            int rmin = min % 60;
            int h = (int)Math.Ceiling((min - rmin) / 60f);
            int rh = h;// h % 24;
            return (h > 0 ? ForceLetters(h) + ":" : "") + ((rmin >= 0 || h >= 0) ? ForceLetters(rmin) + ":" : "") + ForceLetters(rsec);
        }

        private static string GetWebRequest(string url)
        {
            string WEBSERVICE_URL = url;
            try {
                var __webRequest = System.Net.WebRequest.Create(WEBSERVICE_URL);
                if (__webRequest != null) {
                    __webRequest.Method = "GET";
                    __webRequest.Timeout = 12000;
                    __webRequest.ContentType = "application/json";
                    __webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");

                    using (System.IO.Stream s = __webRequest.GetResponse().GetResponseStream()) {
                        using (System.IO.StreamReader sr = new System.IO.StreamReader(s)) {
                            var jsonResponse = sr.ReadToEnd();
                            return jsonResponse.ToString();
                            // Console.WriteLine(String.Format("Response: {0}", jsonResponse));
                        }
                    }
                }
            }
            catch (System.Exception) { }
            return "";
        }

        public static string PostRequest(string myUri, string referer = "", string _requestBody = "", TempThred? _tempThred = null)
        {
            try {
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(myUri);

                webRequest.Method = "POST";
                //  webRequest.Headers.Add("x-token", realXToken);
                webRequest.Headers.Add("X-Requested-With", "XMLHttpRequest");
                webRequest.Headers.Add("DNT", "1");
                webRequest.Headers.Add("Cache-Control", "max-age=0, no-cache");
                webRequest.Headers.Add("TE", "Trailers");
                webRequest.Headers.Add("Pragma", "Trailers");
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Referer = referer;
                webRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                // webRequest.Headers.Add("Host", "trollvid.net");
                webRequest.UserAgent = USERAGENT;
                webRequest.Headers.Add("Accept-Language", "en-US,en;q=0.5");
                bool done = false;
                string _res = "";
                webRequest.BeginGetRequestStream(new AsyncCallback((IAsyncResult callbackResult) => {
                    HttpWebRequest _webRequest = (HttpWebRequest)callbackResult.AsyncState;
                    Stream postStream = _webRequest.EndGetRequestStream(callbackResult);

                    string requestBody = _requestBody;// --- RequestHeaders ---

                    byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

                    postStream.Write(byteArray, 0, byteArray.Length);
                    postStream.Close();

                    if (_tempThred != null) {
                        TempThred tempThred = (TempThred)_tempThred;
                        if (!GetThredActive(tempThred)) { return; }
                    }


                    // BEGIN RESPONSE

                    _webRequest.BeginGetResponse(new AsyncCallback((IAsyncResult _callbackResult) => {
                        HttpWebRequest request = (HttpWebRequest)_callbackResult.AsyncState;
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(_callbackResult);
                        if (_tempThred != null) {
                            TempThred tempThred = (TempThred)_tempThred;
                            if (!GetThredActive(tempThred)) { return; }
                        }
                        using (StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream())) {
                            if (_tempThred != null) {
                                TempThred tempThred = (TempThred)_tempThred;
                                if (!GetThredActive(tempThred)) { return; }
                            }
                            _res = httpWebStreamReader.ReadToEnd();
                            done = true;
                        }
                    }), _webRequest);
                }), webRequest);


                for (int i = 0; i < 1000; i++) {
                    Thread.Sleep(10);
                    if (done) {
                        return _res;
                    }
                }
                return _res;
            }
            catch (Exception) {

                return "";
            }
        }

        /// <summary>
        /// Returns the true mx url of Viduplayer
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        static string GetViduplayerUrl(string source)
        {
            source = source.Replace("||||", "|");
            source = source.Replace("|||", "|");
            source = source.Replace("||", "|");
            source = source.Replace("||", "|");
            source = source.Replace("||", "|");

            string inter = FindHTML(source, "|mp4|", "|");
            /*
            if(inter.Length < 5) {
                inter = FindHTML(_episode, "|srt|", "|");

            }
            if (inter.Length < 5) {
                inter = FindHTML(_episode, "|vvad|", "|");

            }*/

            string server = "";
            string _ser = FindHTML(source, "<img src=\"https://", ".viduplayer.com");
            if (_ser != "") {
                server = _ser;
            }

            if (server == "") {
                string[] serverStart = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z" };
                for (int s = 0; s < serverStart.Length; s++) {
                    for (int i = 0; i < 100; i++) {
                        if (source.Contains("|" + serverStart[s] + i + "|")) {
                            server = serverStart[s] + i;
                        }
                    }
                }

                for (int i = 0; i < 100; i++) {
                    if (source.Contains("|www" + i + "|")) {
                        server = "www" + i;
                    }
                }
            }

            if (inter.Length < 5) {
                inter = FindReverseHTML(source, "|" + server + "|", "|");
            }
            if (inter == "adb") {
                inter = FindHTML(source, "|srt|", "|");
            }

            //https://v16.viduplayer.com/vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fble4vtvcgek2jha/v.mp4
            //100||vxokfmpswoalavf4eqnivlo2355co6iwwgaawrhe7je3fsxmxsx2ovpk34ua|1000
            if (inter.Length <= 5) {
                inter = FindHTML(source, "100|", "|1000");
            }
            if (server == "") {
                return "Error, server not found";
            }
            if (inter == "") {
                return "Error, index not found";
            }

            return "https://" + server + ".viduplayer.com/" + inter + "/v.mp4";
        }

        /// <summary>
        /// Do links contants inp
        /// </summary>
        /// <param name="links"></param>
        /// <param name="inp"></param>
        /// <returns></returns>
        public static bool LinkListContainsString(List<Link> links, string inp)
        {
            if (links == null) {
                return false;
            }
            else {
                try {
                    foreach (Link link in links) {
                        if (link.url == inp) { return true; }
                    }
                }
                catch {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Simple funct to download a sites fist page as string
        /// </summary>
        /// <param name="url"></param>
        /// <param name="UTF8Encoding"></param>
        /// <returns></returns>
        public static string DownloadString(string url, TempThred? tempThred = null, bool UTF8Encoding = true, int repeats = 2, int waitTime = 1000)
        {
            string s = "";
            for (int i = 0; i < repeats; i++) {
                if (s == "") {
                    s = DownloadStringOnce(url, tempThred, UTF8Encoding, waitTime);
                }
            }
            return s;
        }



        public static string DownloadStringOnce(string url, TempThred? tempThred = null, bool UTF8Encoding = true, int waitTime = 1000)
        {
            try {
                WebClient client = new WebClient();
                if (UTF8Encoding) {
                    client.Encoding = Encoding.UTF8; // TO GET SPECIAL CHARACTERS ECT
                }
                // ANDROID DOWNLOADSTRING

                bool done = false;
                string _s = "";
                bool error = false;
                client.DownloadStringCompleted += (o, e) => {
                    done = true;
                    if (!e.Cancelled) {
                        if (e.Error == null) {
                            _s = e.Result;
                        }
                        else {
                            _s = "";
                            error = true;
                            print(e.Error.Message + "|" + url);
                        }
                    }
                    else {
                        _s = "";
                    }
                };
                client.DownloadStringTaskAsync(url);
                for (int i = 0; i < waitTime; i++) {
                    Thread.Sleep(10);
                    try {
                        if (tempThred != null) {
                            if (!GetThredActive((TempThred)tempThred)) {
                                client.CancelAsync();
                                return "";
                            }
                        }
                    }
                    catch (Exception) { }

                    if (done) {
                        //print(_s);
                        print(">>" + i);
                        return _s;
                    }
                }
                if (!error) {
                    client.CancelAsync();
                }
                return _s;

                // return client.DownloadString(url);
            }
            catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// Makes first letter of all capital
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        static string ToTitle(string title)
        {
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(title.Replace("/", "").Replace("-", " "));
        }

        /// <summary>
        /// Used in while true loops to remove last used string
        /// </summary>
        /// <param name="d"></param>
        /// <param name="rem"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string RemoveOne(string d, string rem, int offset = 1)
        {
            int indexOfRem = d.IndexOf(rem);
            return d.Substring(indexOfRem + offset, d.Length - indexOfRem - offset);
        }

        /// <summary>
        /// Used to find string in string, for example 123>Hello<132123, hello can be found using FindHTML(d,">","<");
        /// </summary>
        /// <param name="all"></param>
        /// <param name="first"></param>
        /// <param name="end"></param>
        /// <param name="offset"></param>
        /// <param name="readToEndOfFile"></param>
        /// <returns></returns>
        public static string FindHTML(string all, string first, string end, int offset = 0, bool readToEndOfFile = false, bool decodeToNonHtml = false)
        {
            int firstIndex = all.IndexOf(first);
            if (firstIndex == -1) {
                return "";
            }
            int x = firstIndex + first.Length + offset;

            all = all.Substring(x, all.Length - x);
            int y = all.IndexOf(end);
            if (y == -1) {
                if (readToEndOfFile) {
                    y = all.Length;
                }
                else {
                    return "";
                }
            }
            //  print(x + "|" + y);

            string s = all.Substring(0, y);

            if (decodeToNonHtml) {
                return RemoveHtmlChars(s);
            }
            else {
                return s;
            }
        }


        static readonly List<string> sortingList = new List<string>() { "4k", "2160p", "upstream", "1080p", "hd", "auto", "autop", "720p", "hls", "source", "480p", "360p", "240p" };
        public static MirrorInfo[] SortToHdMirrors(List<string> mirrorsUrls, List<string> mirrorsNames)
        {
            List<MirrorInfo> mirrorInfos = new List<MirrorInfo>();
            for (int i = 0; i < sortingList.Count; i++) {
                for (int q = 0; q < mirrorsUrls.Count; q++) {
                    if ($" {mirrorsNames[q]} ".ToLower().Contains($" {sortingList[i]} ")) {
                        var add = new MirrorInfo() { name = mirrorsNames[q], url = mirrorsUrls[q] };
                        if (!mirrorInfos.Contains(add)) {
                            mirrorInfos.Add(add);
                        }
                    }
                }
            }
            for (int q = 0; q < mirrorsUrls.Count; q++) {
                var add = new MirrorInfo() { name = mirrorsNames[q], url = mirrorsUrls[q] };
                if (!mirrorInfos.Contains(add)) {
                    mirrorInfos.Add(add);
                }
            }
            return mirrorInfos.ToArray();//<MirrorInfo>();
        }

        public static void print(object o)
        {
#if DEBUG
            if (o != null) {
                System.Diagnostics.Debug.WriteLine(o.ToString());
            }
            else {
                System.Diagnostics.Debug.WriteLine("Null");
            }
#endif
        }
        public static void debug(object o)
        {
#if DEBUG
            if (o != null && DEBUG_WRITELINE) {
                System.Diagnostics.Debug.WriteLine(o.ToString());
            }
            else {
                System.Diagnostics.Debug.WriteLine("Null");
            }
#endif
        }

        // LICENSE
        //
        //   This software is dual-licensed to the public domain and under the following
        //   license: you are granted a perpetual, irrevocable license to copy, modify,
        //   publish, and distribute this file as you see fit.
        /// <summary>
        /// Does a fuzzy search for a pattern within a string.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern)
        {
            var patternIdx = 0;
            var strIdx = 0;
            var patternLength = pattern.Length;
            var strLength = stringToSearch.Length;

            while (patternIdx != patternLength && strIdx != strLength) {
                if (char.ToLower(pattern[patternIdx]) == char.ToLower(stringToSearch[strIdx]))
                    ++patternIdx;
                ++strIdx;
            }

            return patternLength != 0 && strLength != 0 && patternIdx == patternLength;
        }

        /// <summary>
        /// Does a fuzzy search for a pattern within a string, and gives the search a score on how well it matched.
        /// </summary>
        /// <param name="stringToSearch">The string to search for the pattern in.</param>
        /// <param name="pattern">The pattern to search for in the string.</param>
        /// <param name="outScore">The score which this search received, if a match was found.</param>
        /// <returns>true if each character in pattern is found sequentially within stringToSearch; otherwise, false.</returns>
        public static bool FuzzyMatch(string stringToSearch, string pattern, out int outScore)
        {
            // Score consts
            const int adjacencyBonus = 5;               // bonus for adjacent matches
            const int separatorBonus = 10;              // bonus if match occurs after a separator
            const int camelBonus = 10;                  // bonus if match is uppercase and prev is lower

            const int leadingLetterPenalty = -3;        // penalty applied for every letter in stringToSearch before the first match
            const int maxLeadingLetterPenalty = -9;     // maximum penalty for leading letters
            const int unmatchedLetterPenalty = -1;      // penalty for every letter that doesn't matter


            // Loop variables
            var score = 0;
            var patternIdx = 0;
            var patternLength = pattern.Length;
            var strIdx = 0;
            var strLength = stringToSearch.Length;
            var prevMatched = false;
            var prevLower = false;
            var prevSeparator = true;                   // true if first letter match gets separator bonus

            // Use "best" matched letter if multiple string letters match the pattern
            char? bestLetter = null;
            char? bestLower = null;
            int? bestLetterIdx = null;
            var bestLetterScore = 0;

            var matchedIndices = new List<int>();

            // Loop over strings
            while (strIdx != strLength) {
                var patternChar = patternIdx != patternLength ? pattern[patternIdx] as char? : null;
                var strChar = stringToSearch[strIdx];

                var patternLower = patternChar != null ? char.ToLower((char)patternChar) as char? : null;
                var strLower = char.ToLower(strChar);
                var strUpper = char.ToUpper(strChar);

                var nextMatch = patternChar != null && patternLower == strLower;
                var rematch = bestLetter != null && bestLower == strLower;

                var advanced = nextMatch && bestLetter != null;
                var patternRepeat = bestLetter != null && patternChar != null && bestLower == patternLower;
                if (advanced || patternRepeat) {
                    score += bestLetterScore;
                    matchedIndices.Add((int)bestLetterIdx);
                    bestLetter = null;
                    bestLower = null;
                    bestLetterIdx = null;
                    bestLetterScore = 0;
                }

                if (nextMatch || rematch) {
                    var newScore = 0;

                    // Apply penalty for each letter before the first pattern match
                    // Note: Math.Max because penalties are negative values. So max is smallest penalty.
                    if (patternIdx == 0) {
                        var penalty = System.Math.Max(strIdx * leadingLetterPenalty, maxLeadingLetterPenalty);
                        score += penalty;
                    }

                    // Apply bonus for consecutive bonuses
                    if (prevMatched)
                        newScore += adjacencyBonus;

                    // Apply bonus for matches after a separator
                    if (prevSeparator)
                        newScore += separatorBonus;

                    // Apply bonus across camel case boundaries. Includes "clever" isLetter check.
                    if (prevLower && strChar == strUpper && strLower != strUpper)
                        newScore += camelBonus;

                    // Update pattern index IF the next pattern letter was matched
                    if (nextMatch)
                        ++patternIdx;

                    // Update best letter in stringToSearch which may be for a "next" letter or a "rematch"
                    if (newScore >= bestLetterScore) {
                        // Apply penalty for now skipped letter
                        if (bestLetter != null)
                            score += unmatchedLetterPenalty;

                        bestLetter = strChar;
                        bestLower = char.ToLower((char)bestLetter);
                        bestLetterIdx = strIdx;
                        bestLetterScore = newScore;
                    }

                    prevMatched = true;
                }
                else {
                    score += unmatchedLetterPenalty;
                    prevMatched = false;
                }

                // Includes "clever" isLetter check.
                prevLower = strChar == strLower && strLower != strUpper;
                prevSeparator = strChar == '_' || strChar == ' ';

                ++strIdx;
            }

            // Apply score for last match
            if (bestLetter != null) {
                score += bestLetterScore;
                matchedIndices.Add((int)bestLetterIdx);
            }

            outScore = score;
            return patternIdx == patternLength;
        }
    }
}

// =============================================================== NOT IMPLEMENTED PROVIDERS (COMING SOON) ===============================================================



// =============================================================== watch.animeonline360 ===============================================================

/*
            string url = "https://watch.animeonline360.com/?s=overlord";

            string d = DownloadString(url);
            string lookFor = " class=\"title\"> <a href=\"";
            while (d.Contains(lookFor)) {
                string uri = FindHTML(d, lookFor, "\"");
                d = RemoveOne(d, lookFor);
                string name = FindHTML(d, "/\">", "<",decodeToNonHtml:true);
                print(name + "|" + uri);
            }
            */
/*
string url = "https://watch.animeonline360.com/tvshows/overlord-dubbed/";
string d = DownloadString(url);
List<string> episodes = new List<string>();
string lookFor = "div class=\'numerando\'>";
while (d.Contains(lookFor)) {
    string uri = FindHTML(d, "<a href=\'", "\'");
    d = RemoveOne(d, lookFor);
    episodes.Add(uri);
}*/

/*
string url = "https://watch.animeonline360.com/episodes/air-episode-1-english-dubbed/";
string d = DownloadString(url);
string id = FindHTML(d, "data-post=\'", "\'");
string _d = PostRequest("https://watch.animeonline360.com/wp-admin/admin-ajax.php", url, "action=doo_player_ajax&post=" + id + "&nume=1&type=tv");
string source = FindHTML(_d, "src=\'", "\'");
 d = DownloadString(source);
string videoUrl = FindHTML(d, "source src=\"", "\"");
print(videoUrl);*/



// =============================================================== twist.moe ===============================================================

/*
        // =============== GET COOKIE SITE ===============
        string d = GetHTML("https://twist.moe/a/angel-beats/1");
        string code = FindHTML(d, "<script>", "</script>").Replace("e(r);", "alert(r);");
        string token = "";
        string token2 = "";
        string tokenCook = "";
        var engine = new Engine().SetValue("alert", new Action<string>((a) => { token = a; }));//.SetValue("log", new Action<string>((a) => { token2 = a; })); ;
        engine.Execute(code);
        token = token.Replace("location.reload();", "alert(e); alertCook(doccookie);");
        string find = "+ \';path=";
        token = token.Replace(find + "" + FindHTML(token, find, "\'") + "\'", "").Replace("document.cookie=", "var doccookie=");

        var engine2 = new Engine()
                 .SetValue("alert", new Action<string>((a) => { token2 = a; })).SetValue("alertCook", new Action<string>((a) => { tokenCook = a; }));
        engine2.Execute(token);

        //string _d = HTMLGet("https://twist.moe/api/anime/angel-beats/sources", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { token2 }) ;

        // =============== GET REAL SITE ===============

        string _d = HTMLGet("https://twist.moe/a/angel-beats/1", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } });
        string openToken = "";
        string lookFor = "<link href=\"/_nuxt/";
        while (_d.Contains(lookFor)) {
            if (openToken == "") {
                string ___d = FindHTML(_d, lookFor, "\"");
                if (___d.EndsWith(".js")) {
                    string dKey = DownloadString("https://twist.moe/_nuxt/" + ___d);
                    openToken = FindHTML(dKey, "x-access-token\":\"", "\"");
                    //x-access-token":"
                }
            }
            _d = RemoveOne(_d, lookFor);
        }
        // =============== GET SOURCES ===============
        string seriesD = HTMLGet("https://twist.moe/api/anime/angel-beats/sources", "https://twist.moe/a/angel-beats/1", cookies: new List<Cookie>() { new Cookie() { Name = FindHTML("|" + tokenCook, "|", "="), Value = FindHTML(tokenCook + "|", "=", "|"), Expires = DateTime.Now.AddSeconds(1000) } }, keys: new List<string>() { "x-access-token" }, values: new List<string>() { openToken }) ;
        print(seriesD);
        string fetch = FetchMoeUrlFromSalted("U2FsdGVkX19keEYdmWm5SfhOhvOubne48rkyrP/vvFp/9yurDNMI20rUCXnlkf8DFLW9bSJfKLFkKsN6P7Hhy+zYjYdqjOB/EyNH9+gdhdAjUJVKBbdui/Kk3Avx84/e");
        print(fetch);*/

// =============================================================== twist.moe LINK FETCH ===============================================================


/*        static string FetchMoeUrlFromSalted(string _salted)
    {
        byte[] CreateMD5Byte(byte[] input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create()) {
                byte[] hashBytes = md5.ComputeHash(input);
                return hashBytes;
            }
        }
        string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            string plaintext = null;
            using (Aes aesAlg = Aes.Create()) {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText)) {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8)) {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }
        byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays) {
                System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }
        byte[] bytes_to_key(byte[] data, byte[] _salt, int output = 48)
        {
            data = Combine(data, _salt);
            byte[] _key = CreateMD5Byte(data);
            List<byte> final_key = _key.ToList();
            while (final_key.Count < output) {
                _key = CreateMD5Byte(Combine(_key, data));
                final_key.AddRange(_key);
            }
            return SubArray(final_key.ToArray(), 0, output);
        }

        const string KEY = "LXgIVP&PorO68Rq7dTx8N^lP!Fa5sGJ^*XK";
        var f = System.Convert.FromBase64String(_salted);
        var salt = SubArray(f, 8, 8);
        var bytes = System.Text.Encoding.ASCII.GetBytes(KEY);
        byte[] key_iv = bytes_to_key(bytes, salt, 32 + 16);
        byte[] key = SubArray(key_iv, 0, 32);

        byte[] iv = SubArray(key_iv, 32, 16);
        return FindHTML(DecryptStringFromBytes_Aes(SubArray(f, 16, f.Length - 16), key, iv) + "|", "/", "|").Replace(" ", "%20");
    }
*/
