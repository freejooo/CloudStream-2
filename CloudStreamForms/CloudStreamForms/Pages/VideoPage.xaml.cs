using CloudStreamForms.Core;
using FFImageLoading;
using LibVLCSharp.Shared;
using SubtitlesParser.Classes;
using SubtitlesParser.Classes.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VideoPage : ContentPage
    {
        const string PLAY_IMAGE = "netflixPlay.png";//"baseline_play_arrow_white_48dp.png";
        const string PAUSE_IMAGE = "pausePlay.png";//"baseline_pause_white_48dp.png";

        const string LOCKED_IMAGE = "LockLocked1.png";// "wlockLocked.png";
        const string UN_LOCKED_IMAGE = "LockUnlocked1.png";//"wlockUnLocked.png";

        /// <summary>
        /// Given current imdbId respond with the next episode id after loading links
        /// </summary>
        /// <param name="currentEpIMDBId"></param>
        /// <returns></returns>
        public delegate Task<string> LoadLinkForNextEpisode(string currentEpIMDBId, bool loadLinks);
        public delegate bool CanLoadLinks(string currentEpIMDBId);
        public static LoadLinkForNextEpisode loadLinkForNext;
        public static CanLoadLinks canLoad;

        /// <summary>
        /// If header is correct
        /// </summary>
        public static string loadLinkValidForHeader = "";
        public static int maxEpisodeForLoading = 0;

        bool isPaused = false;

        MediaPlayer Player { get { return vvideo.MediaPlayer; } set { vvideo.MediaPlayer = value; } }

        static LibVLC _libVLC;
        static MediaPlayer _mediaPlayer;


        /// <summary>
        /// Multiplyes a string
        /// </summary>
        /// <param name="s">String of what you want to multiply</param>
        /// <param name="times">How many times you want to multiply it</param>
        /// <returns></returns>
        public static string MultiplyString(string s, int times)
        {
            return String.Concat(Enumerable.Repeat(s, times));
        }


        /// <summary>
        /// Turn a int to classic score like pacman or spaceinvader, 123 -> 00000123
        /// </summary>
        /// <param name="inp">Input, can be string or int</param>
        /// <param name="maxLetters">How many letters at max</param>
        /// <param name="multiString">What character that will be used before score</param>
        /// <returns></returns>
        public static string ConvertScoreToArcadeScore(object inp, int maxLetters = 8, string multiString = "0")
        {
            string inpS = inp.ToString();
            inpS = MultiplyString(multiString, maxLetters - inpS.Length) + inpS;
            return inpS;
        }

        /// <summary>
        /// 0-1
        /// </summary>
        /// <param name="time"></param>
        public void ChangeTime(double time)
        {
            StartTxt.Text = CloudStreamCore.ConvertTimeToString((GetPlayerLenght() / 1000) * time);
            EndTxt.Text = CloudStreamCore.ConvertTimeToString(((GetPlayerLenght()) / 1000) - (GetPlayerLenght() / 1000) * time);
        }


        /// <summary>
        /// Holds info about the current video
        /// </summary>
        /// 
        [System.Serializable]
        public struct PlayVideo
        {
            public List<string> MirrorUrls;
            public List<string> MirrorNames;
            public bool isDownloadFile;
            public string downloadFileUrl;

            //public List<string> Subtitles;
            //public List<string> SubtitlesNames; // Requested subtitled to download
            public string name;
            public string descript;
            public int episode; //-1 = null, movie  
            public int season; //-1 = null, movie  
            public bool isSingleMirror;
            public long startPos; // -2 from progress, -1 = from start
            public string episodeId;
            public string headerId;
        }

        /// <summary>
        /// IF MOVIE, 1 else number of episodes in season
        /// </summary>
        public static int maxEpisodes = 0;
        public static int currentMirrorId = 0;
        //public static int currentSubtitlesId = -2; // -2 = null, -1 = none, 0 = def
        public static PlayVideo currentVideo;

        const string NONE_SUBTITLES = "None";
        const string ADD_BEFORE_EPISODE = "\"";
        const string ADD_AFTER_EPISODE = "\"";

        public static bool IsSeries { get { return !(currentVideo.season == -1 || currentVideo.episode == -1); } }
        public static string BeforeAddToName { get { return IsSingleMirror && !currentVideo.isDownloadFile ? AllMirrorsNames[0] : (IsSeries ? ("S" + currentVideo.season + ":E" + currentVideo.episode + " ") : ""); } }
        public static string CurrentDisplayName { get { return BeforeAddToName + (IsSeries ? ADD_BEFORE_EPISODE : "") + currentVideo.name + (IsSeries ? ADD_AFTER_EPISODE : "") + (IsSingleMirror ? "" : (" · " + CurrentMirrorName)); } }
        public static string CurrentMirrorName { get { return currentVideo.MirrorNames[currentMirrorId]; } }
        public static string CurrentMirrorUrl { get { return currentVideo.MirrorUrls[currentMirrorId]; } }
        //public static string CurrentSubtitles { get { if (currentSubtitlesId == -1) { return ""; } else { return currentVideo.Subtitles[currentSubtitlesId]; } } }

        //public static string CurrentSubtitlesNames { get { if (currentSubtitlesId == -1) { return NONE_SUBTITLES; } else { return currentVideo.SubtitlesNames[currentSubtitlesId]; } } }
        //public static List<string> AllSubtitlesNames { get { var f = new List<string>() { NONE_SUBTITLES }; f.AddRange(currentVideo.SubtitlesNames); return f; } }
        //public static List<string> AllSubtitlesUrls { get { var f = new List<string>() { "" }; f.AddRange(currentVideo.Subtitles); return f; } }

        // public static List<SubtitleItem[]> ParsedSubtitles = new List<SubtitleItem[]>();
        //   public static SubtitleItem[] Subtrack { get { if (currentSubtitlesId == -1) { return null; } else { return ParsedSubtitles[currentSubtitlesId]; } } }
        public static List<string> AllMirrorsNames { get { return currentVideo.MirrorNames; } }
        public static List<string> AllMirrorsUrls { get { return currentVideo.MirrorUrls; } }

        string lastUrl = "";
        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
        {
            try {
                Task task = Task.Factory.StartNew(() => codeBlock());
                task.Wait(timeSpan);
                return task.IsCompleted;
            }
            catch (AggregateException ae) {
                return false;
                // throw ae.InnerExceptions[0];
            }
            finally {
                print("FINISHED EXICUTE");
            }
        }

        public void HandleAudioFocus(object sender, bool e)
        {
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (GetPlayerLenght() == -1) return;
            if (!GetPlayerIsPlaying() && e) { // HANDLE GAIN AUDIO FOCUS AUTO

            }
            if (GetPlayerIsPlaying() && !e) { // HANDLE LOST AUDIO FOCUS
                if (Player.CanPause) {
                    Player.SetPause(true);
                }
            }
        }


        Media disMedia;
        public void SelectMirror(int mirror)
        {
            if (!isShown) return;

            if (currentVideo.isDownloadFile) {
                EpisodeLabel.Text = CurrentDisplayName;
                //  System.IO.File.Open(currentVideo.downloadFileUrl, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);


                disMedia = new Media(_libVLC, currentVideo.downloadFileUrl, FromType.FromPath);//new Uri(currentVideo.downloadFileUrl, UriKind.Absolute)); // currentVideo.downloadFileUrl, FromType.FromPath,);
                                                                                               // disMedia.AddOption(new MediaConfiguration() { })

                bool succ = vvideo.MediaPlayer.Play(disMedia);
                return;
            }
            if (lastUrl == AllMirrorsUrls[mirror]) return;


            currentMirrorId = mirror;

            print("MIRROR SELECTED : " + CurrentMirrorUrl);
            if (CurrentMirrorUrl == "" || CurrentMirrorUrl == null) {
                print("ERRPR IN SELECT MIRROR");
                ErrorWhenLoading();
            }
            else {
                Device.BeginInvokeOnMainThread(() => {
                    EpisodeLabel.Text = CurrentDisplayName;
                    App.ToggleRealFullScreen(true);
                });

                bool Completed = ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(1000), () => {
                    try {
                        print("PLAY MEDIA " + CurrentMirrorUrl);
                        disMedia = new Media(_libVLC, CurrentMirrorUrl, FromType.FromLocation);
                        lastUrl = CurrentMirrorUrl;
                        bool succ = vvideo.MediaPlayer.Play(disMedia);
                        print("SUCC:::: " + succ);

                        if (!succ) {
                            ErrorWhenLoading();
                        }
                    }
                    catch (Exception _ex) {
                        print("EXEPTIOM: " + _ex);
                    }
                    //
                    // Write your time bounded code here
                    // 
                });
                print("COMPLEATED::: " + Completed);
            }
        }

        public List<VideoSubtitle> currentSubtitles = new List<VideoSubtitle>();
        public int subtitleIndex = -1;
        object subtitleMutex = new object();
        Dictionary<string, bool> searchingForLang = new Dictionary<string, bool>();
        int subtitleDelay = 0;

        public struct VideoSubtitle
        {
            public SubtitleItem[] subtitles;
            public string name;
        }

        public async Task SubtitleOptions()
        {
            List<string> options = new List<string>();
            if (currentSubtitles.Count > 0) {
                if (subtitleIndex != -1) {
                    options.Add($"Change Delay ({subtitleDelay} ms)");
                }
                options.Add("Select Subtitles");
            }
            else {
                options.Add($"Download Subtitles ({Settings.NativeSubLongName})");
            }
            options.Add("Download Subtitles");

            string action = await ActionPopup.DisplayActionSheet("Subtitles", options.ToArray());

            if (action == "Download Subtitles") {
                string subAction = await ActionPopup.DisplayActionSheet("Download Subtitles", subtitleNames);
                if (subAction != "Cancel") {
                    int index = subtitleNames.IndexOf(subAction);
                    PopulateSubtitle(subtitleShortNames[index], subAction);
                }
            }
            else if (action == "Select Subtitles") {
                List<string> subtitlesList = currentSubtitles.Select(t => t.name).ToList();
                subtitlesList.Insert(0, "None");

                string subAction = await ActionPopup.DisplayActionSheet("Select Subtitles", subtitleIndex + 1, subtitlesList.ToArray());

                if (subAction != "Cancel") {
                    int setTo = subtitlesList.IndexOf(subAction) - 1;
                    if (setTo != subtitleIndex) {
                        subtitleIndex = setTo;
                    }
                }
            }
            else if (action.StartsWith("Change Delay")) {
                int del = await ActionPopup.DisplayIntEntry("ms", "Subtitle Delay", 50, false, subtitleDelay.ToString(), "Set Delay");
                if (del != -1) {
                    subtitleDelay = del;
                }
            }
            else if (action == $"Download Subtitles ({Settings.NativeSubLongName})") {
                PopulateSubtitle();
            }

        }

        public bool HasSupportForSubtitles() => currentVideo.episodeId.IsClean() && currentVideo.episodeId.StartsWith("tt");

        public void PopulateSubtitle(string lang = "", string name = "")
        {
            if (!HasSupportForSubtitles()) return;

            if (lang == "") {
                lang = Settings.NativeSubShortName;
                name = Settings.NativeSubLongName;
            }

            bool ContainsLang()
            {
                lock (subtitleMutex) {
                    return currentSubtitles.Where(t => t.name == name).Count() != 0;
                }
            }

            if (ContainsLang()) {
                App.ShowToast("Subtitles already downloaded"); // THIS SHOULD NEVER HAPPEND
                return;
            }

            if (searchingForLang.ContainsKey(lang)) {
                App.ShowToast("Searching for subtitles");
                return;
            }
            searchingForLang[lang] = true;

            var thread = mainCore.CreateThread(6);
            mainCore.StartThread("PopulateSubtitles", () => {
                try {
                    string data = mainCore.DownloadSubtitle(currentVideo.episodeId, lang, false, true);
                    if (data.IsClean()) {
                        if (!ContainsLang()) {
                            lock (subtitleMutex) {
                                VideoSubtitle s = new VideoSubtitle();
                                s.name = name;
                                s.subtitles = MainChrome.ParseSubtitles(data).ToArray();
                                currentSubtitles.Add(s);
                            }
                            App.ShowToast(name + " subtitles added");
                        }
                    }
                    else {
                        App.ShowToast("Error Downloading Subtitles");
                    }
                }
                finally {
                    if (searchingForLang.ContainsKey(lang)) {
                        searchingForLang.Remove(lang);
                    }
                }
            });
        }

        int currentSubtitleIndexInCurrent = 0;

        string subtitleText1; // TOP
        string subtitleText2; // BOTTOM

        double currentTime = 0;

        string lastSub = "";

        void UpdateSubtitles()
        {
            string comp = subtitleText1 + "|||" + subtitleText2;
            if (lastSub == comp) {
                return;
            }
            lastSub = comp;

            Device.BeginInvokeOnMainThread(() => {
                string subTxt = subtitleText1 + "\n" + subtitleText2;
                print("TITLL:::: " + subtitleText1 + "|" + subtitleText2);

                SubtitleTxt1.Text = subtitleText1;//"HELLO WORLD\ndadada YEeett";//
                if (hasOutline) {
                    SubtitleTxt1Back1.Text = subtitleText1;
                    SubtitleTxt1Back2.Text = subtitleText1;
                    SubtitleTxt1Back3.Text = subtitleText1;
                    SubtitleTxt1Back4.Text = subtitleText1;
                    SubtitleTxt1Back5.Text = subtitleText1;
                    SubtitleTxt1Back6.Text = subtitleText1;
                    SubtitleTxt1Back7.Text = subtitleText1;
                    SubtitleTxt1Back8.Text = subtitleText1;
                }


                SubtitleTxt2.Text = subtitleText2;
                SubtitleTxt2Back1.Text = subtitleText2;
                SubtitleTxt2Back2.Text = subtitleText2;
                SubtitleTxt2Back3.Text = subtitleText2;
                SubtitleTxt2Back4.Text = subtitleText2;
                SubtitleTxt2Back5.Text = subtitleText2;
                SubtitleTxt2Back6.Text = subtitleText2;
                SubtitleTxt2Back7.Text = subtitleText2;
                SubtitleTxt2Back8.Text = subtitleText2;
            });
        }

        int last = 0;
        bool lastType = false;
        public void SubtitleLoop()
        {
            if (subtitleIndex == -1 || currentSubtitles[subtitleIndex].subtitles.Length <= 3) {
                // await Task.Delay(50);
                subtitleText2 = "";
                subtitleText1 = "";
                UpdateSubtitles();
                return;
            }

            bool done = false;
            var Subtrack = currentSubtitles[subtitleIndex].subtitles;
            while (!done) {
                try {
                    var track = Subtrack[currentSubtitleIndexInCurrent];
                    // SET CORRECT TIME
                    bool IsTooHigh()
                    {
                        //  print("::::::>>>>>" + currentSubtitleIndexInCurrent + "|" + currentTime + "<>" + track.fromMilisec);
                        return currentTime < Subtrack[currentSubtitleIndexInCurrent].StartTime + subtitleDelay && currentSubtitleIndexInCurrent > 0;
                    }

                    bool IsTooLow()
                    {
                        return currentTime > Subtrack[currentSubtitleIndexInCurrent + 1].StartTime + subtitleDelay && currentSubtitleIndexInCurrent < Subtrack.Length - 1;
                    }

                    if (IsTooHigh()) {
                        while (IsTooHigh()) {
                            currentSubtitleIndexInCurrent--;
                        }
                        print("SKIPP::1");
                        continue;
                    }

                    if (IsTooLow()) {
                        while (IsTooLow()) {
                            currentSubtitleIndexInCurrent++;
                        }
                        print("SKIPP::2");
                        continue;
                    }


                    if (currentTime < track.EndTime + subtitleDelay && currentTime > track.StartTime + subtitleDelay) {
                        if (last != currentSubtitleIndexInCurrent) {
                            var lines = track.Lines;
                            if (lines.Count > 0) {
                                if (lines.Count == 1) {
                                    subtitleText1 = "";//lines[1].line;
                                    subtitleText2 = lines[0];
                                }
                                else {
                                    subtitleText1 = lines[0];
                                    subtitleText2 = lines[1];
                                }
                            }
                            UpdateSubtitles();
                            print("SETSUB:" + track.StartTime + "|" + currentTime + "|" + subtitleDelay);
                            lastType = false;
                        }
                    }
                    else {
                        if (!lastType) {
                            subtitleText1 = "";
                            subtitleText2 = "";
                            UpdateSubtitles();
                            lastType = true;
                        }
                    }
                    done = true;

                    last = currentSubtitleIndexInCurrent;
                    print("DELAY!::: " + currentSubtitleIndexInCurrent);
                    //  await Task.Delay(30);
                }
                catch (Exception _ex) {
                    done = true;
                    print("EX::X:X::X:X: " + _ex);
                }
            }
        }

        const double lazyLoadOverProcentage = 0.8; // 80% compleated before it loads links
        bool canLazyLoadNextEpisode = false;
        bool hasLazyLoadedNextEpisode = false;

        public long GetPlayerLenght()
        {
            if (Player == null || !isShown) {
                return -1;
            }
            else {
                return lastPlayerLenght;// Player.Length;
            }
        }

        bool IsNotValid()
        {
            return Player == null || !isShown;
        }

        public bool GetPlayerIsPlaying()
        {
            if (IsNotValid()) return false;
            return Player.IsPlaying; // Player.Time;
        }

        public bool GetPlayerIsSeekable()
        {
            if (IsNotValid()) return false;
            return isSeekeble; // Player.Time;
        }

        public bool GetPlayerIsPauseble()
        {
            if (IsNotValid()) return false;
            return isPausable; // Player.Time;
        }

        public long GetPlayerTime()
        {
            if (Player == null || !isShown) {
                return -1;
            }
            else {
                return lastPlayerTime; // Player.Time;
            }
        }

        long lastPlayerTime = 0;
        long lastPlayerLenght = 0;
        public void PlayerTimeChanged(long time)
        {
            if (!isShown) return;

            if (Player == null) {
                return;
            }

            lastPlayerTime = time;
            double pro = ((double)(time / 1000) / (double)(lastPlayerLenght / 1000));
            if (canLazyLoadNextEpisode && !hasLazyLoadedNextEpisode) {
                if (pro > lazyLoadOverProcentage) {
                    hasLazyLoadedNextEpisode = true;
                    try {
                        loadLinkForNext?.Invoke(currentVideo.episodeId, false);
                    }
                    catch (Exception) {
                    }
                }
            }

            try {
                SubtitleLoop();
            }
            catch (Exception _ex) {
                print("SUBLOOP FAILED:D" + _ex);
            }

            Device.BeginInvokeOnMainThread(() => {
                try {
                    if (Player == null) {
                        return;
                    }
                    if (!isShown) return;
                    var len = GetPlayerLenght();

                    double val = ((double)(time / 1000) / (double)(len / 1000));
                    ChangeTime(val);
                    currentTime = time;
                    VideoSlider.Value = val;
                    if (len != -1 && len > 10000 && len <= time + 1000) {
                        //TODO: NEXT EPISODE
                        Navigation.PopModalAsync();
                    }
                }
                catch (Exception _ex) {
                    print("EROROR WHEN TIME" + _ex);
                }
            });
        }

        VisualElement[] lockElements;
        VisualElement[] settingsElements;

        public static bool IsSingleMirror = false;

        void UpdateAudioDelay(long delay)
        {
            print("SETAUDIO:::: " + delay);
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            Player.SetAudioDelay(delay * 1000); // MS TO NS
        }

        static bool hasOutline = false;

        Label[] font1;
        Label[] font2;

        /// <summary>
        /// Subtitles are in full
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="name"></param>
        /// <param name="subtitles"></param>
        public VideoPage(PlayVideo video, int _maxEpisodes = 1)
        {
            print("DADAD LOADED VIDEO A");
            isShown = true;
            changeFullscreenWhenPop = true;

            currentVideo = video;
            maxEpisodes = _maxEpisodes;
            IsSingleMirror = video.isSingleMirror;

            InitializeComponent();


            // ======================= SUBTITLE SETUP =======================

            Vector2[] offsets = new Vector2[]  {
                new Vector2(0,1),
                new Vector2(1,1),
                new Vector2(1,0),
                new Vector2(1,-1),
                new Vector2(0,-1),
                new Vector2(-1,-1),
                new Vector2(-1,0),
                new Vector2(-1,1),
            };

            font1 = new Label[] {
                 SubtitleTxt1Back1,
                 SubtitleTxt1Back2,
                 SubtitleTxt1Back3,
                 SubtitleTxt1Back4,
                 SubtitleTxt1Back5,
                 SubtitleTxt1Back6,
                 SubtitleTxt1Back7,
                 SubtitleTxt1Back8,
            };


            font2 = new Label[] {
                SubtitleTxt2Back1,
                SubtitleTxt2Back2,
                SubtitleTxt2Back3,
                SubtitleTxt2Back4,
                SubtitleTxt2Back5,
                SubtitleTxt2Back6,
                SubtitleTxt2Back7,
                SubtitleTxt2Back8,
            };

            float multi = 1f;


            //double base1X = SubtitleTxt1.TranslationX;
            double base1Y = -5;
            double base2Y = 20;
            SubtitleTxt1.TranslationY = base1Y;
            SubtitleTxt2.TranslationY = base2Y;

            double hreq = SubtitleTxt1.HeightRequest;


            double base1X = SubtitleTxt1.TranslationX;
            double base2X = SubtitleTxt2.TranslationX;

            bool hasDropshadow = Settings.SubtitlesHasDropShadow;
            hasOutline = Settings.SubtitlesHasOutline;

            string fontFam = App.GetFont(Settings.GlobalSubtitleFont);

            string classId = hasDropshadow ? "OUTLINE" : "";
            for (int i = 0; i < font1.Length; i++) {
                if (hasOutline) {
                    font1[i].FontFamily = fontFam;
                    font1[i].TranslationX = base1X + offsets[i].X * multi;
                    font1[i].TranslationY = base1Y + offsets[i].Y * multi;
                    font1[i].ClassId = classId;
                    font1[i].HeightRequest = hreq;
                }
                else {
                    font1[i].IsVisible = false;
                    font1[i].IsEnabled = false;
                }
            }

            for (int i = 0; i < font2.Length; i++) {
                if (hasOutline) {
                    font2[i].FontFamily = fontFam;
                    font2[i].TranslationX = base2X + offsets[i].X * multi;
                    font2[i].TranslationY = base2Y + offsets[i].Y * multi;
                    font2[i].ClassId = classId;
                    font1[i].HeightRequest = hreq;

                }
                else {
                    font2[i].IsVisible = false;
                    font2[i].IsEnabled = false;
                }
            }

            SubtitleTxt1.FontFamily = fontFam;
            SubtitleTxt2.FontFamily = fontFam;

            SubtitleTxt1.ClassId = classId;
            SubtitleTxt2.ClassId = classId;

            // ======================= END =======================

            LibVLCSharp.Shared.Core.Initialize();
            lockElements = new VisualElement[] { NextMirror, NextMirrorBtt, BacktoMain, GoBackBtt, EpisodeLabel, PausePlayClickBtt, PausePlayBtt, SkipForward, SkipForwardBtt, SkipForwardImg, SkipForwardSmall, SkipBack, SkipBackBtt, SkipBackImg, SkipBackSmall };
            settingsElements = new VisualElement[] { EpisodesTap, MirrorsTap, DelayTap, SubTap, NextEpisodeTap, };
            VisualElement[] pressIcons = new VisualElement[] { LockTap, EpisodesTap, MirrorsTap, DelayTap, SubTap, NextEpisodeTap };

            void SetIsLocked()
            {
                LockImg.Source = App.GetImageSource(isLocked ? LOCKED_IMAGE : UN_LOCKED_IMAGE);
                LockTxt.TextColor = Color.FromHex(isLocked ? "#516bde" : "#FFFFFF"); // 516bde 617EFF
                // LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#617EFF" : "#FFFFFF"));
                LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#99acff" : "#FFFFFF"));
                //LockTap.BackgroundColor = isLocked ? new Color(0.6, 0.67, 1, 0.1) : Color.Transparent;
                LockImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(isLocked ? MainPage.DARK_BLUE_COLOR : "#FFFFFF")) };
                VideoSlider.InputTransparent = isLocked;

                foreach (var visual in lockElements) {
                    visual.IsEnabled = !isLocked;
                    visual.IsVisible = !isLocked;
                }

                foreach (var visual in settingsElements) {
                    visual.Opacity = isLocked ? 0 : 1;
                    visual.IsEnabled = !isLocked;
                }

                if (!isLocked) {
                    ShowNextMirror();
                }
            }


            SkipForward.TranslationX = TRANSLATE_START_X;
            SkipForwardImg.Source = App.GetImageSource("netflixSkipForward.png");
            SkipForwardBtt.TranslationX = TRANSLATE_START_X;
            Commands.SetTap(SkipForwardBtt, new Command(() => {
                if (!isShown) return;
                CurrentTap++;
                StartFade();
                SeekMedia(SKIPTIME);
                lastClick = DateTime.Now;
                SkipFor();
            }));

            SkipBack.TranslationX = -TRANSLATE_START_X;
            SkipBackImg.Source = App.GetImageSource("netflixSkipBack.png");
            SkipBackBtt.TranslationX = -TRANSLATE_START_X;
            Commands.SetTap(SkipBackBtt, new Command(() => {
                if (!isShown) return;
                CurrentTap++;
                StartFade();
                SeekMedia(-SKIPTIME);
                lastClick = DateTime.Now;
                SkipBac();
            }));

            Commands.SetTap(LockTap, new Command(() => {
                isLocked = !isLocked;
                CurrentTap++;
                FadeEverything(false);
                StartFade();
                SetIsLocked();
            }));

            MirrorsTap.IsVisible = currentVideo.isDownloadFile ? false : AllMirrorsUrls.Count > 1;


            if (Settings.SUBTITLES_INVIDEO_ENABELD) {
                try {
                    if (globalSubtitlesEnabled && HasSupportForSubtitles()) {
                        PopulateSubtitle();
                    }
                }
                catch (Exception _ex) {
                    print("A_A__A__A:: " + _ex);
                }
            }

            SubTap.IsVisible = HasSupportForSubtitles() && Settings.SUBTITLES_INVIDEO_ENABELD;
            EpisodesTap.IsVisible = false; // TODO: ADD EPISODES SWITCH
            NextEpisodeTap.IsVisible = false; // TODO: ADD NEXT EPISODE

            if (currentVideo.isDownloadFile) {
                var keys = Download.downloads.Keys;
                foreach (var key in keys) {
                    var info = Download.downloads[key].info;
                    if (info.source == currentVideo.headerId) {
                        if (info.episode == currentVideo.episode + 1 && info.season == currentVideo.season) {
                            NextEpisodeTap.IsVisible = true;

                            Commands.SetTap(NextEpisodeTap, new Command(async () => {
                                await Navigation.PopModalAsync();
                                Download.PlayDownloadedFile(info, false);
                            }));
                            break;
                        }
                    }
                }
            }
            else {
                if (currentVideo.headerId == loadLinkValidForHeader) {
                    if (canLoad?.Invoke(currentVideo.episodeId) ?? false) {
                        NextEpisodeTap.IsVisible = true;
                        canLazyLoadNextEpisode = true;

                        Commands.SetTap(NextEpisodeTap, new Command(async () => {
                            await loadLinkForNext?.Invoke(currentVideo.episodeId, true);
                        }));
                    }
                }
            }


            Commands.SetTap(MirrorsTap, new Command(async () => {
                string action = await ActionPopup.DisplayActionSheet("Mirrors", AllMirrorsNames.ToArray());//await DisplayActionSheet("Mirrors", "Cancel", null, AllMirrorsNames.ToArray());
                App.ToggleRealFullScreen(true);
                CurrentTap++;
                StartFade();
                print("ACTION = " + action);
                if (action != "Cancel") {
                    for (int i = 0; i < AllMirrorsNames.Count; i++) {
                        if (AllMirrorsNames[i] == action) {
                            print("SELECT MIRR" + action);
                            SelectMirror(i);
                        }
                    }
                }
            }));

            Commands.SetTap(SubTap, new Command(async () => {
                await SubtitleOptions();
            }));

            Commands.SetTap(DelayTap, new Command(async () => {
                int action = await ActionPopup.DisplayIntEntry("ms", "Audio Delay", 50, false, App.GetDelayAudio().ToString(), "Set Delay");//await DisplayActionSheet("Mirrors", "Cancel", null, AllMirrorsNames.ToArray());
                print("MAINACTIONFROM : " + action);
                App.ToggleRealFullScreen(true);
                CurrentTap++;
                StartFade();
                if (action != -1) {
                    App.SetAudioDelay(action);
                    UpdateAudioDelay(action);
                }
            }));



            // ======================= SETUP ======================= 
            if (_libVLC == null) {
                _libVLC = new LibVLC();
            }
            if (_mediaPlayer == null) {
                _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = false, };
            }

            vvideo.MediaPlayer = _mediaPlayer; // = new VideoView() { MediaPlayer = _mediaPlayer };

            // ========== IMGS ==========
            // SubtitlesImg.Source = App.GetImageSource("netflixSubtitlesCut.png"); //App.GetImageSource("baseline_subtitles_white_48dp.png");
            MirrosImg.Source = App.GetImageSource("baseline_playlist_play_white_48dp.png");
            AudioImg.Source = App.GetImageSource("AudioVolLow3.png"); // App.GetImageSource("baseline_volume_up_white_48dp.png");
            EpisodesImg.Source = App.GetImageSource("netflixEpisodesCut.png");
            NextImg.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
            BacktoMain.Source = App.GetImageSource("baseline_keyboard_arrow_left_white_48dp.png");
            NextMirror.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
            SetIsLocked();
            // LockImg.Source = App.GetImageSource("wlockUnLocked.png");
            SubtitleImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");


            //  GradientBottom.Source = App.GetImageSource("gradient.png");
            // DownloadImg.Source = App.GetImageSource("netflixEpisodesCut.png");//App.GetImageSource("round_more_vert_white_48dp.png");



            Player.AudioDevice += (o, e) => {
                SetIsPaused(true);
            };


            Commands.SetTap(EpisodesTap, new Command(() => {
                //do something
                print("Hello");
            }));
            Commands.SetTap(NextMirrorBtt, new Command(() => {
                SelectNextMirror();
            }));


            //Commands.SetTapParameter(view, someObject);
            // ================================================================================ UI ================================================================================
            PausePlayBtt.Source = PAUSE_IMAGE;//App.GetImageSource(PAUSE_IMAGE);
            //PausePlayClickBtt.set
            Commands.SetTap(PausePlayClickBtt, new Command(() => {
                //do something
                PausePlayBtt_Clicked(null, EventArgs.Empty);
                print("Hello");
            }));
            Commands.SetTap(GoBackBtt, new Command(() => {
                print("DAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAdddddddddddAAAAAAAA");
                Navigation.PopModalAsync();
                //Navigation.PopModalAsync();
            }));

            void SetIsPaused(bool paused)
            {
                PausePlayBtt.Source = paused ? PLAY_IMAGE : PAUSE_IMAGE;//App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
                PausePlayBtt.Opacity = 1;
                LoadingCir.IsVisible = false;
                BufferLabel.IsVisible = false;
                isPaused = paused;
                if (!isPaused) {
                    UpdateAudioDelay(App.GetDelayAudio());
                }
            }

            Player.Paused += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    SetIsPaused(true);
                    App.ReleaseAudioFocus();
                    //   LoadingCir.IsEnabled = false;
                });
            };
            Player.Playing += (o, e) => {
                UpdateAudioDelay(App.GetDelayAudio());
                Device.BeginInvokeOnMainThread(() => {
                    if (App.GainAudioFocus()) {
                        SetIsPaused(false);
                    }
                    else {
                        if (Player.CanPause) {
                            Player.SetPause(true);
                        }
                    }
                });
                //   LoadingCir.IsEnabled = false;
            };
            Player.TimeChanged += (o, e) => {
                PlayerTimeChanged(e.Time);
            };
            Player.LengthChanged += (o, e) => {
                lastPlayerLenght = e.Length;
            };
            Player.PausableChanged += (o, e) => {
                isPausable = e.Pausable == 1;
            };
            Player.SeekableChanged += (o, e) => {
                isSeekeble = e.Seekable == 1;
            };


            Player.Buffering += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    if (e.Cache == 100) {
                        try {
                            if (Player != null) {
                                SetIsPaused(!GetPlayerIsPlaying());
                                UpdateAudioDelay(App.GetDelayAudio());
                            }
                        }
                        catch (Exception) {

                        }
                    }
                    else {
                        //BufferBar.ProgressTo(e.Cache / 100.0, 50, Easing.Linear);
                        BufferLabel.Text = "" + (int)e.Cache + "%";
                        BufferLabel.IsVisible = true;
                        PausePlayBtt.Opacity = 0;
                        LoadingCir.IsVisible = true;
                    }
                });

            };

            Player.EncounteredError += (o, e) => {
                // TODO: SKIP TO NEXT
                print("ERROR LOADING MDDD: ");
                ErrorWhenLoading();
            };
            SelectMirror(0);
            ShowNextMirror();


            int columCount = 0;
            for (int i = 0; i < pressIcons.Length; i++) {
                if (pressIcons[i].IsVisible) {
                    Grid.SetColumn(pressIcons[i], columCount);
                    columCount++;
                }
            }



            //  Player.AddSlave(MediaSlaveType.Subtitle,"") // ADD SUBTITLEs
        }

        bool isPausable = false;
        bool isSeekeble = false;

        void ShowNextMirror()
        {
            NextMirror.IsVisible = !IsSingleMirror;
            NextMirror.IsEnabled = !IsSingleMirror;
            NextMirrorBtt.IsVisible = !IsSingleMirror;
            NextMirrorBtt.IsEnabled = !IsSingleMirror;
        }

        void ErrorWhenLoading()
        {
            print("ERROR UCCURED");
            App.ShowToast("Error loading media");
            if (currentVideo.isDownloadFile) {
                Navigation.PopModalAsync();
            }
            else {
                SelectNextMirror();
            }
        }

        void SelectNextMirror()
        {
            int _currentMirrorId = currentMirrorId + 1;
            if (_currentMirrorId >= AllMirrorsUrls.Count) {
                _currentMirrorId = 0;
            }
            SelectMirror(_currentMirrorId);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LibVLCSharp.Shared.Core.Initialize();

            print("ON APPEARING VIDEOPAGE");
            isShown = true;
            App.OnAudioFocusChanged += HandleAudioFocus;

            try { // SETTINGS NOW ALLOWED 
                BrightnessProcentage = App.GetBrightness() * 100;
                print("BRIGHTNESS:::" + BrightnessProcentage + "]]]");
            }
            catch (Exception) {
                canChangeBrightness = false;
            }

            Volume = 100;
            Hide();
            App.LandscapeOrientation();
            App.ToggleRealFullScreen(true);
            /*
            TapRec. += (o, e) => {
                print("CHANGED:::<<<<<<<<<<<<:");
                if (visible) {
                    VideoSettings.FadeTo(0);
                }
                else {
                    VideoSettings.FadeTo(1);
                }
                visible = !visible;*/
            /*
            StringBuilder sb = new StringBuilder("");

            //print($" { e.NumberOfTaps} times with {e.NumberOfTouches} fingers.");
            print($" ViewPosition: {e.ViewPosition.X}/{ e.ViewPosition.Y}/{e.ViewPosition.Width}/{ e.ViewPosition.Height}, Touches: ");
            if (e.Touches != null && e.Touches.Length > 0)
                print(String.Join(", ", e.Touches.Select(t => t.X + "/" + t.Y)));

            print("DADAAAAAAAAAAAAAAAAAAAAAAAAADDDGGGGGGGGGGG" + (e.ViewPosition.X < e.ViewPosition.Width / 2.0));
            print(e.Touches.Length);*/
            /*};
            TapRec.DoubleTapped += (o, e) => {
                var d = e;
                throw new Exception();
            };*/
            /*
            print("CHADHSHAHDSANN:" + e.GestureId);
            if (e.GestureId == (int)GestureStatus.Started) {
                print("CHANGED:::<<<<<<<<<<<<:");
                if (visible) {
                    VideoSettings.FadeTo(0);
                }
                else {
                    VideoSettings.FadeTo(1);
                }
                visible = !visible;
            }*/
        }


        public static bool isShown = false;
        public static bool changeFullscreenWhenPop = true;
        protected override void OnDisappearing()
        {
            isShown = false;

            print("ONDIS:::::::");
            try {
                App.OnAudioFocusChanged -= HandleAudioFocus;

                if (Player != null) {

                    if (Player.State != VLCState.Error && Player.State != VLCState.Opening) {
                        if (GetPlayerLenght() != -1) {
                            string lastId = currentVideo.episodeId;
                            if (lastId != null) {
                                long pos = GetPlayerTime();//Last position in media when player exited
                                if (pos > -1) {
                                    App.SetViewPos(lastId, pos);
                                    print("ViewHistoryTimePos SET TO: " + lastId + "|" + pos);
                                }
                                long dur = GetPlayerLenght();//	long	Total duration of the media
                                if (dur > -1) {
                                    App.SetViewDur(lastId, dur);
                                    print("ViewHistoryTimeDur SET TO: " + lastId + "|" + dur);
                                }
                            }

                        }

                    }
                    /*
                    try {
                        Device.BeginInvokeOnMainThread(() => {
                            Player.Stop();
                        });
                    }
                    catch (Exception _ex) {
                        error("EDDDD::: " + _ex);
                    }*/
                }
                // App.ShowStatusBar();
                if (changeFullscreenWhenPop) {
                    App.NormalOrientation();
                    App.ToggleRealFullScreen(false);
                }
                //App.ToggleFullscreen(!Settings.HasStatusBar);
                App.ForceUpdateVideo?.Invoke(null, EventArgs.Empty);

                try {
                    //  Player.TryDispose();
                }
                catch (Exception _ex) {
                    print("MAIN EX: in video :::: " + _ex);
                }
                print("STOPDIS::");
                // Player.Dispose();
            }
            catch (Exception _ex) {
                print("ERROR IN DISAPEERING" + _ex);
            }

            try {
                if (currentVideo.isDownloadFile) {
                    if (disMedia != null) {
                        disMedia.Dispose();
                    }
                    Dispose();
                }
                else {
                    //Thread t = new Thread(() => {
                    _mediaPlayer.Stop();
                    //});
                    //t.Start();
                    Dispose();

                }

                /*
                _libVLC.Dispose();
                _mediaPlayer.Dispose();*/
                //Player.Dispose();
            }
            catch (Exception) {

            }


            base.OnDisappearing();
        }

        public void Dispose()
        {
          //  Thread t = new Thread(() => {
                var mediaPlayer = _mediaPlayer;
                _mediaPlayer = null;
                mediaPlayer?.Dispose();
                _libVLC?.Dispose();
                _libVLC = null;
           // });
          //  t.Name = "DISPOSETHREAD";
            //t.Start();
        }
        async void Hide()
        {
            await Task.Delay(100);
            App.HideStatusBar();
        }

        public void PausePlayBtt_Clicked(object sender, EventArgs e)
        {
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (GetPlayerLenght() == -1) return;
            if (!Player.CanPause && !isPaused) return;

            if (isPaused) { // UNPAUSE
                StartFade();
            }

            //Player.SetPause(true);
            if (!dragingVideo) {
                if (GetPlayerIsPlaying()) {
                    Player.SetPause(true);
                }
                else {
                    if (App.GainAudioFocus()) {
                        Player.SetPause(false);
                    }
                }
                //Player.Pause();
            }

        }


        // =========================================================================== LOGIC ====================================================================================================




        static bool dragingVideo = false;
        private void VideoSlider_DragStarted(object sender, EventArgs e)
        {
            CurrentTap++;
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (GetPlayerLenght() == -1) return;
            if (!GetPlayerIsSeekable()) return;
            if (!Player.CanPause) return;

            Player.SetPause(true);
            dragingVideo = true;
            SlideChangedLabel.IsVisible = true;
            SlideChangedLabel.Text = "";
        }

        private void VideoSlider_DragCompleted(object sender, EventArgs e)
        {
            CurrentTap++;
            try {
                if (Player == null) return;
                if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
                if (!GetPlayerIsSeekable()) return;
                if (GetPlayerLenght() == -1) return;

                long len = (long)(VideoSlider.Value * GetPlayerLenght());
                Player.Time = len;
                //SeekSubtitles(len);

                if (App.GainAudioFocus()) {
                    Player.SetPause(false);
                }
                dragingVideo = false;
                SlideChangedLabel.IsVisible = false;
                SlideChangedLabel.Text = "";
                SkiptimeLabel.Text = "";
                StartFade();
            }
            catch (Exception _ex) {
                print("ERROR DTAG: " + _ex);
            }

        }

        private void VideoSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (!isShown) return;
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (!GetPlayerIsSeekable()) return;
            if (GetPlayerLenght() == -1) return;

            ChangeTime(e.NewValue);
            if (dragingVideo) {
                long timeChange = (long)(VideoSlider.Value * GetPlayerLenght()) - GetPlayerTime();
                CurrentTap++;
                SlideChangedLabel.TranslationX = ((e.NewValue - 0.5)) * (VideoSlider.Width - 30);

                //    var time = TimeSpan.FromMilliseconds(timeChange);

                string before = ((Math.Abs(timeChange) < 1000) ? "" : timeChange > 0 ? "+" : "-") + ConvertTimeToString(Math.Abs(timeChange / 1000)); //+ (int)time.Seconds + "s";

                SkiptimeLabel.Text = $"[{ConvertTimeToString(VideoSlider.Value * GetPlayerLenght() / 1000)}]";
                SlideChangedLabel.Text = before;//CloudStreamCore.ConvertTimeToString((timeChange / 1000.0));
            }
        }

        const int TRANSLATE_SKIP_X = 70;
        const int TRANSLATE_START_X = 200;

        async void SkipForAni()
        {
            SkipForwardImg.AbortAnimation("RotateTo");
            SkipForwardImg.AbortAnimation("ScaleTo");
            SkipForwardImg.Rotation = 0;
            SkipForwardImg.ScaleTo(0.9, 100, Easing.SinInOut);
            await SkipForwardImg.RotateTo(90, 100, Easing.SinInOut);
            SkipForwardImg.ScaleTo(1, 100, Easing.SinInOut);
            await SkipForwardImg.RotateTo(0, 100, Easing.SinInOut);
        }

        async void SkipBacAni()
        {
            SkipBackImg.AbortAnimation("RotateTo");
            SkipBackImg.AbortAnimation("ScaleTo");
            SkipBackImg.Rotation = 0;
            SkipBackImg.ScaleTo(0.9, 100, Easing.SinInOut);
            await SkipBackImg.RotateTo(-90, 100, Easing.SinInOut);
            SkipBackImg.ScaleTo(1, 100, Easing.SinInOut);
            await SkipBackImg.RotateTo(0, 100, Easing.SinInOut);
        }

        async void SkipFor()
        {
            SkipForward.AbortAnimation("TranslateTo");
            SkipForAni();

            SkipForward.Opacity = 1;
            SkipForwardSmall.Opacity = 0;
            SkipForward.TranslationX = TRANSLATE_START_X;

            await SkipForward.TranslateTo(TRANSLATE_START_X + TRANSLATE_SKIP_X, SkipForward.TranslationY, 200, Easing.SinOut);

            SkipForward.TranslationX = TRANSLATE_START_X;
            SkipForward.Opacity = 0;
            SkipForwardSmall.Opacity = 1;
        }


        async void SkipBac()
        {
            SkipBack.AbortAnimation("TranslateTo");
            SkipBacAni();

            SkipBack.Opacity = 1;
            SkipBackSmall.Opacity = 0;
            SkipBack.TranslationX = -TRANSLATE_START_X;

            await SkipBack.TranslateTo(-TRANSLATE_START_X - TRANSLATE_SKIP_X, SkipBack.TranslationY, 200, Easing.SinOut);

            SkipBack.TranslationX = -TRANSLATE_START_X;
            SkipBack.Opacity = 0;
            SkipBackSmall.Opacity = 1;
        }


        DateTime lastClick = DateTime.MinValue;

        public const int SKIPTIME = 10000;
        const float minimumDistance = 1;
        bool isMovingCursor = false;
        bool isMovingHorozontal = false;
        bool isMovingFromLeftSide = false;
        long isMovingStartTime = 0;
        long isMovingSkipTime = 0;


        double _Volume = 100;
        double Volume {
            set {
                _Volume = value; Player.Volume = (int)value;
            }
            get { return _Volume; }
        }

        int maxVol = 100;
        bool canChangeBrightness = true;
        double _BrightnessProcentage = 100;
        double BrightnessProcentage {
            set {
                _BrightnessProcentage = value; /*OverlayBlack.Opacity = 1 - (value / 100.0);*/ App.SetBrightness(value / 100);
            }
            get { return _BrightnessProcentage; }
        }
        TouchTracking.TouchTrackingPoint cursorPosition;
        TouchTracking.TouchTrackingPoint startCursorPosition;


        const uint fadeTime = 100;
        const int timeUntilFade = 3500;
        const bool CAN_FADE_WHEN_PAUSED = false;
        const bool WILL_AUTO_FADE_WHEN_PAUSED = false;
        async void FadeEverything(bool disable, bool overridePaused = false)
        {
            if (isPaused && !overridePaused && CAN_FADE_WHEN_PAUSED) { // CANT FADE WHEN PAUSED
                return;
            }
            await Task.Delay(100);

            print("FADETO: " + disable);
            VideoSliderAndSettings.AbortAnimation("TranslateTo");
            VideoSliderAndSettings.TranslateTo(VideoSliderAndSettings.TranslationX, disable ? 80 : 0, fadeTime, Easing.Linear);
            EpisodeLabel.AbortAnimation("TranslateTo");
            EpisodeLabel.TranslateTo(EpisodeLabel.TranslationX, disable ? -60 : 20, fadeTime, Easing.Linear);


            List<Label> subHolders = new List<Label>();
            /*subHolders.AddRange(font1);
            subHolders.AddRange(font2);
            subHolders.Add(SubtitleTxt1);
            subHolders.Add(SubtitleTxt2);

            for (int i = 0; i < subHolders.; i++) {

            }*/
            SubHolder.AbortAnimation("TranslateTo");
            SubHolder.TranslateTo(EpisodeLabel.TranslationX, disable ? 0 : -90, fadeTime, Easing.Linear);

            AllButtons.AbortAnimation("FadeTo");
            AllButtons.IsEnabled = !disable;
            await AllButtons.FadeTo(disable ? 0 : 1, fadeTime, Easing.Linear);
        }

        async void StartFade(bool overridePause = false)
        {
            string _currentTap = CurrentTap.ToString();
            await Task.Delay(timeUntilFade);
            if (_currentTap == CurrentTap.ToString()) {
                if (!isPaused || WILL_AUTO_FADE_WHEN_PAUSED) {
                    FadeEverything(true, overridePause);
                }
            }
        }

        int CurrentTap = 0;

        bool isLocked = false;

        double TotalOpasity { get { return AllButtons.Opacity; } }

        DateTime lastRelease = DateTime.MinValue;
        DateTime startPressTime = DateTime.MinValue;

        private void TouchEffect_TouchAction(object sender, TouchTracking.TouchActionEventArgs args)
        {
            if (!isShown) return;
            print("TOUCH ACCTION0");
            bool lockActionOnly = false;

            void CheckLock()
            {
                if (Player == null) {
                    lockActionOnly = true; return;
                }
                else if (GetPlayerTime() == -1) {
                    lockActionOnly = true; return;
                }
                else if (GetPlayerLenght() == -1) {
                    lockActionOnly = true; return;
                }
                else if (Player.State == VLCState.Error) {
                    lockActionOnly = true; return;
                }
                else if (Player.State == VLCState.Opening) {
                    lockActionOnly = true; return;
                }
                else if (!GetPlayerIsSeekable()) {
                    lockActionOnly = true; return;
                }
            }

            try {
                CheckLock();
            }
            catch (Exception _ex) {
                print("ERRORIN TOUCH: " + _ex);
                lockActionOnly = true;
            }

            if (args.Type == TouchTracking.TouchActionType.Pressed || args.Type == TouchTracking.TouchActionType.Moved || args.Type == TouchTracking.TouchActionType.Entered) {
                CurrentTap++;
            }
            else if (args.Type == TouchTracking.TouchActionType.Cancelled || args.Type == TouchTracking.TouchActionType.Exited || args.Type == TouchTracking.TouchActionType.Released) {
                StartFade();
            }

            if (!isShown) return;


            // ========================================== LOCKED LOGIC ==========================================
            if (isLocked || lockActionOnly) {

                if (args.Type == TouchTracking.TouchActionType.Pressed) {
                    startPressTime = System.DateTime.Now;
                }

                if (args.Type == TouchTracking.TouchActionType.Released && System.DateTime.Now.Subtract(startPressTime).TotalSeconds < 0.4) {

                    if (TotalOpasity == 1) {
                        FadeEverything(true);
                    }
                    else {
                        FadeEverything(false);
                    }
                }
                return;
            };

            CheckLock();
            if (lockActionOnly) return;

            long playerTime = GetPlayerTime();
            long playerLenght = GetPlayerLenght();
            if (!isShown) return;

            // ========================================== NORMAL LOGIC ==========================================
            if (args.Type == TouchTracking.TouchActionType.Pressed) {
                if (DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) { // Doubble click
                    lastRelease = DateTime.Now;

                    bool forward = (TapRec.Width / 2.0 < args.Location.X);
                    SeekMedia(SKIPTIME * (forward ? 1 : -1));
                    CurrentTap++;
                    StartFade();
                    if (forward) {
                        SkipFor();
                    }
                    else {
                        SkipBac();
                    }
                }
                lastClick = DateTime.Now;
                FadeEverything(false);

                startCursorPosition = args.Location;
                isMovingFromLeftSide = (TapRec.Width / 2.0 > args.Location.X);
                isMovingStartTime = playerTime;
                isMovingSkipTime = 0;
                isMovingCursor = false;
                cursorPosition = args.Location;

                maxVol = Volume >= 100 ? 200 : 100;
            }
            else if (args.Type == TouchTracking.TouchActionType.Moved) {
                print(startCursorPosition.X - args.Location.X);
                if ((minimumDistance < Math.Abs(startCursorPosition.X - args.Location.X) || minimumDistance < Math.Abs(startCursorPosition.X - args.Location.X)) && !isMovingCursor) {
                    // STARTED FIRST TIME
                    isMovingHorozontal = Math.Abs(startCursorPosition.X - args.Location.X) > Math.Abs(startCursorPosition.Y - args.Location.Y);
                    isMovingCursor = true;
                }
                else if (isMovingCursor) { // DRAGINS SKIPING TIME
                    if (isMovingHorozontal) {
                        double diffX = (args.Location.X - startCursorPosition.X) * 2.0 / TapRec.Width;
                        isMovingSkipTime = (long)((playerLenght * (diffX * diffX) / 10) * (diffX < 0 ? -1 : 1)); // EXPONENTIAL SKIP LIKE VLC

                        if (isMovingSkipTime + isMovingStartTime > playerLenght) { // SKIP TO END
                            isMovingSkipTime = playerLenght - isMovingStartTime;
                        }
                        else if (isMovingSkipTime + isMovingStartTime < 0) { // SKIP TO FRONT
                            isMovingSkipTime = -isMovingStartTime;
                        }
                        SkiptimeLabel.Text = $"{CloudStreamCore.ConvertTimeToString((isMovingStartTime + isMovingSkipTime) / 1000)} [{ (Math.Abs(isMovingSkipTime) < 1000 ? "" : (isMovingSkipTime > 0 ? "+" : "-"))}{CloudStreamCore.ConvertTimeToString(Math.Abs(isMovingSkipTime / 1000))}]";
                    }
                    else {
                        if (isMovingFromLeftSide) {
                            if (canChangeBrightness) {
                                BrightnessProcentage -= (args.Location.Y - cursorPosition.Y) / 2.0;
                                BrightnessProcentage = Math.Max(Math.Min(BrightnessProcentage, 100), 0); // CLAM
                                SkiptimeLabel.Text = $"Brightness {(int)BrightnessProcentage}%";
                            }
                        }
                        else {
                            Volume -= (args.Location.Y - cursorPosition.Y) / 2.0;
                            Volume = Math.Max(Math.Min(Volume, maxVol), 0); // CLAM
                            SkiptimeLabel.Text = $"Volume {(int)Volume}%";
                        }
                    }

                    cursorPosition = args.Location;

                }
            }
            else if (args.Type == TouchTracking.TouchActionType.Released) {
                if (isMovingCursor && isMovingHorozontal && Math.Abs(isMovingSkipTime) > 1000) { // SKIP TIME
                    FadeEverything(true);
                    if (Player != null) {
                        SeekMedia(isMovingSkipTime - playerTime + isMovingStartTime);
                    }
                }
                else {
                    SkiptimeLabel.Text = "";
                    isMovingCursor = false;
                    if ((DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) && TotalOpasity == 1 && DateTime.Now.Subtract(lastRelease).TotalSeconds > 0.25) { // FADE WHEN REALEASED
                        FadeEverything(true);
                    }
                }
            }
            print("LEFT TIGHT " + (TapRec.Width / 2.0 < args.Location.X) + TapRec.Width + "|" + TapRec.X);
            print("TOUCHED::D:A::A" + args.Location.X + "|" + args.Type.ToString());
        }

        void SeekMedia(long ms)
        {
            if (!isShown) return;
            try {
                if (Player == null) return;
                if (Player.State == VLCState.Error) return;
                if (Player.State == VLCState.Opening) return;
                if (GetPlayerLenght() == -1) return;
                if (!GetPlayerIsSeekable()) return;
            }
            catch (Exception _ex) {
                print("ERRORIN TOUCH: " + _ex);
                return;
            }

            print("SEEK MEDIA to " + ms);
            var newTime = GetPlayerTime() + ms;
            Player.Time = newTime;

            // SeekSubtitles(newTime);
            PlayerTimeChanged(newTime);



        }
    }
}