using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.CloudStreamCore;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VideoPage : ContentPage
    {



        const string PLAY_IMAGE = "netflixPlay.png";//"baseline_play_arrow_white_48dp.png";
        const string PAUSE_IMAGE = "pausePlay.png";//"baseline_pause_white_48dp.png";

        const string LOCKED_IMAGE = "wlockLocked.png";
        const string UN_LOCKED_IMAGE = "wlockUnLocked.png";

        bool isPaused = false;

        MediaPlayer Player { get { return vvideo.MediaPlayer; } set { vvideo.MediaPlayer = value; } }

        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;


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
            StartTxt.Text = CloudStreamCore.ConvertTimeToString((Player.Length / 1000) * time);
            EndTxt.Text = CloudStreamCore.ConvertTimeToString(((Player.Length) / 1000) - (Player.Length / 1000) * time);
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
            public List<string> Subtitles;
            public List<string> SubtitlesNames;
            public string name;
            public string descript;
            public int episode; //-1 = null, movie  
            public int season; //-1 = null, movie  
            public bool isSingleMirror;
        }

        /// <summary>
        /// IF MOVIE, 1 else number of episodes in season
        /// </summary>
        public static int maxEpisodes = 0;
        public static int currentMirrorId = 0;
        public static int currentSubtitlesId = 0;
        public static PlayVideo currentVideo;

        const string NONE_SUBTITLES = "None";
        const string ADD_BEFORE_EPISODE = "\"";
        const string ADD_AFTER_EPISODE = "\"";

        public static bool IsSeries { get { return !(currentVideo.season == -1 || currentVideo.episode == -1); } }
        public static string BeforeAddToName { get { return IsSingleMirror ? AllMirrorsNames[0] : (IsSeries ? ("S" + currentVideo.season + ":E" + currentVideo.episode + " ") : ""); } }
        public static string CurrentDisplayName { get { return BeforeAddToName + (IsSeries ? ADD_BEFORE_EPISODE : "") + currentVideo.name + (IsSeries ? ADD_AFTER_EPISODE : "") + (IsSingleMirror ? "" : (" · " + CurrentMirrorName)); } }
        public static string CurrentMirrorName { get { return currentVideo.MirrorNames[currentMirrorId]; } }
        public static string CurrentMirrorUrl { get { return currentVideo.MirrorUrls[currentMirrorId]; } }
        public static string CurrentSubtitles { get { if (currentSubtitlesId == -1) { return ""; } else { return currentVideo.Subtitles[currentMirrorId]; } } }
        public static string CurrentSubtitlesNames { get { if (currentSubtitlesId == -1) { return NONE_SUBTITLES; } else { return currentVideo.SubtitlesNames[currentMirrorId]; } } }
        public static List<string> AllSubtitlesNames { get { var f = new List<string>() { NONE_SUBTITLES }; f.AddRange(currentVideo.SubtitlesNames); return f; } }
        public static List<string> AllSubtitlesUrls { get { var f = new List<string>() { "" }; f.AddRange(currentVideo.Subtitles); return f; } }
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
        public void SelectMirror(int mirror)
        {
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
                        var media = new Media(_libVLC, CurrentMirrorUrl, FromType.FromLocation);
                        lastUrl = CurrentMirrorUrl;
                        bool succ = vvideo.MediaPlayer.Play(media);
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

        /// <summary>
        /// -1 = none
        /// </summary>
        /// <param name="subtitles"></param>
        public void SelectSubtitles(int subtitles = -1)
        {
            currentSubtitlesId = subtitles;
        }


        public void PlayerTimeChanged(long time)
        {
            Device.BeginInvokeOnMainThread(() => {
                try {
                    double val = ((double)(time / 1000) / (double)(Player.Length / 1000));
                    ChangeTime(val);
                    VideoSlider.Value = val;

                    if (Player.Length != -1 && Player.Length > 10000 && Player.Length <= Player.Time + 1000) {
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

        /// <summary>
        /// Subtitles are in full
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="name"></param>
        /// <param name="subtitles"></param>
        public VideoPage(PlayVideo video, int _maxEpisodes = 1)
        {
            currentVideo = video;
            maxEpisodes = _maxEpisodes;
            IsSingleMirror = video.isSingleMirror;

            InitializeComponent();
            Core.Initialize();
            lockElements = new VisualElement[] { BacktoMain, GoBackBtt, EpisodeLabel, PausePlayClickBtt, PausePlayBtt, SkipForward, SkipForwardBtt, SkipForwardImg, SkipForwardSmall, SkipBack, SkipBackBtt, SkipBackImg, SkipBackSmall };
            settingsElements = new VisualElement[] { EpisodesTap, MirrorsTap, SubTap, NextEpisodeTap, };

            void SetIsLocked()
            {
                LockImg.Source = App.GetImageSource(isLocked ? LOCKED_IMAGE : UN_LOCKED_IMAGE);
                LockTxt.TextColor = Color.FromHex(isLocked ? "#617EFF" : "#FFFFFF");
                // LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#617EFF" : "#FFFFFF"));
                LockTap.SetValue(XamEffects.TouchEffect.ColorProperty, Color.FromHex(isLocked ? "#99acff" : "#FFFFFF"));
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

            }


            SkipForward.TranslationX = TRANSLATE_START_X;
            SkipForwardImg.Source = App.GetImageSource("netflixSkipForward.png");
            SkipForwardBtt.TranslationX = TRANSLATE_START_X;
            Commands.SetTap(SkipForwardBtt, new Command(() => {
                SeekMedia(SKIPTIME);
                lastClick = DateTime.Now;
                SkipFor();
            }));

            SkipBack.TranslationX = -TRANSLATE_START_X;
            SkipBackImg.Source = App.GetImageSource("netflixSkipBack.png");
            SkipBackBtt.TranslationX = -TRANSLATE_START_X;
            Commands.SetTap(SkipBackBtt, new Command(() => {
                SeekMedia(-SKIPTIME);
                lastClick = DateTime.Now;
                SkipBac();
            }));
            Commands.SetTap(LockTap, new Command(() => {
                isLocked = !isLocked;
                CurrentTap++;
                FadeEverything(false);
                SetIsLocked();
            }));

            MirrorsTap.IsVisible = AllMirrorsUrls.Count > 1;
            SubTap.IsVisible = false; // TODO: ADD SUBTITLES
            EpisodesTap.IsVisible = false; // TODO: ADD EPISODES SWITCH
            NextEpisodeTap.IsVisible = false; // TODO: ADD NEXT EPISODE


            Commands.SetTap(MirrorsTap, new Command(async () => {
                string action = await DisplayActionSheet("Mirrors", "Cancel", null, AllMirrorsNames.ToArray());
                App.ToggleRealFullScreen(true);
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


            // ======================= SETUP =======================

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };

            vvideo.MediaPlayer = _mediaPlayer; // = new VideoView() { MediaPlayer = _mediaPlayer };



            // ========== IMGS ==========
            // SubtitlesImg.Source = App.GetImageSource("netflixSubtitlesCut.png"); //App.GetImageSource("baseline_subtitles_white_48dp.png");
            MirrosImg.Source = App.GetImageSource("baseline_playlist_play_white_48dp.png");
            EpisodesImg.Source = App.GetImageSource("netflixEpisodesCut.png");
            NextImg.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
            BacktoMain.Source = App.GetImageSource("baseline_keyboard_arrow_left_white_48dp.png");

            //  GradientBottom.Source = App.GetImageSource("gradient.png");
            // DownloadImg.Source = App.GetImageSource("netflixEpisodesCut.png");//App.GetImageSource("round_more_vert_white_48dp.png");

            SetIsLocked();
            // LockImg.Source = App.GetImageSource("wlockUnLocked.png");
            SubtitleImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");


            Commands.SetTap(EpisodesTap, new Command(() => {
                //do something
                print("Hello");
            }));
            //Commands.SetTapParameter(view, someObject);
            // ================================================================================ UI ================================================================================
            PausePlayBtt.Source = App.GetImageSource(PAUSE_IMAGE);
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
                PausePlayBtt.Source = App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
                PausePlayBtt.Opacity = 1;
                LoadingCir.IsVisible = false;
                BufferLabel.IsVisible = false;
                isPaused = paused;
            }

            Player.Paused += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    SetIsPaused(true);
                    //   LoadingCir.IsEnabled = false;
                });

            };
            Player.Playing += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    SetIsPaused(false);

                });

                //   LoadingCir.IsEnabled = false;
            };
            Player.TimeChanged += (o, e) => {
                PlayerTimeChanged(Player.Time);
            };


            Player.Buffering += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    if (e.Cache == 100) {
                        SetIsPaused(!Player.IsPlaying);
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
                ErrorWhenLoading();


            };
            SelectMirror(0);

            //  Player.AddSlave(MediaSlaveType.Subtitle,"") // ADD SUBTITLEs
        }

        void ErrorWhenLoading()
        {
            print("ERROR UCCURED");
            int _currentMirrorId = currentMirrorId + 1;
            if (_currentMirrorId >= AllMirrorsUrls.Count) {
                _currentMirrorId = 0;
            }
            App.ShowToast("Error loading media");
            SelectMirror(_currentMirrorId);

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            try { // SETTINGS NOW ALLOWED 
                BrightnessProcentage = App.GetBrightness() * 100;
                print("BRIGHTNESS:::" + BrightnessProcentage + "]]]");
            }
            catch (Exception) {
                canChangeBrightness = false;
            }

            Volyme = 100;
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


        protected override void OnDisappearing()
        {

            // App.ShowStatusBar();
            App.NormalOrientation();
            App.ToggleRealFullScreen(false);
            //App.ToggleFullscreen(!Settings.HasStatusBar);

            Player.Stop();

            // Player.Dispose();
            base.OnDisappearing();
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
            if (Player.Length == -1) return;
            if (!Player.CanPause && !isPaused) return;

            if (isPaused) { // UNPAUSE
                StartFade();
            }

            //Player.SetPause(true);
            if (!dragingVideo) {
                Player.Pause();
            }

        }


        // =========================================================================== LOGIC ====================================================================================================




        static bool dragingVideo = false;
        private void VideoSlider_DragStarted(object sender, EventArgs e)
        {
            CurrentTap++;
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (Player.Length == -1) return;
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
                if (!Player.IsSeekable) return;
                if (Player.Length == -1) return;

                long len = (long)(VideoSlider.Value * Player.Length);
                Player.Time = len;
                Player.SetPause(false);
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
            if (Player == null) return;
            if (Player.State == VLCState.Error || Player.State == VLCState.Opening) return;
            if (!Player.IsSeekable) return;
            if (Player.Length == -1) return;

            ChangeTime(e.NewValue);
            if (dragingVideo) {
                long timeChange = (long)(VideoSlider.Value * Player.Length) - Player.Time;
                CurrentTap++;
                SlideChangedLabel.TranslationX = ((e.NewValue - 0.5)) * (VideoSlider.Width - 30);

                //    var time = TimeSpan.FromMilliseconds(timeChange);

                string before = ((Math.Abs(timeChange) < 1000) ? "" : timeChange > 0 ? "+" : "-") + ConvertTimeToString(Math.Abs(timeChange / 1000)); //+ (int)time.Seconds + "s";

                SkiptimeLabel.Text = $"[{ConvertTimeToString(VideoSlider.Value * Player.Length / 1000)}]";
                SlideChangedLabel.Text = before;//CloudStreamCore.ConvertTimeToString((timeChange / 1000.0));
            }
        }

        const int TRANSLATE_SKIP_X = 100;
        const int TRANSLATE_START_X = 170;

        async void SkipForAni()
        {
            SkipForwardImg.AbortAnimation("RotateTo");
            SkipForwardImg.Rotation = 0;
            await SkipForwardImg.RotateTo(90, 100, Easing.SinInOut);
            await SkipForwardImg.RotateTo(0, 100, Easing.SinInOut);
        }

        async void SkipBacAni()
        {
            SkipBackImg.AbortAnimation("RotateTo");
            SkipBackImg.Rotation = 0;
            await SkipBackImg.RotateTo(-90, 100, Easing.SinInOut);
            await SkipBackImg.RotateTo(0, 100, Easing.SinInOut);
        }

        async void SkipFor()
        {
            SkipForward.AbortAnimation("TranslateTo");
            SkipForAni();

            SkipForward.Opacity = 1;
            SkipForwardSmall.Opacity = 0;
            SkipForward.TranslationX = TRANSLATE_START_X;

            await SkipForward.TranslateTo(TRANSLATE_START_X + TRANSLATE_SKIP_X, SkipForward.TranslationY, 200, Easing.SinInOut);

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

            await SkipBack.TranslateTo(-TRANSLATE_START_X - TRANSLATE_SKIP_X, SkipBack.TranslationY, 200, Easing.SinInOut);

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


        double _Volyme = 100;
        double Volyme
        {
            set {
                _Volyme = value; Player.Volume = (int)value;
            }
            get { return _Volyme; }
        }

        int maxVol = 100;
        bool canChangeBrightness = true;
        double _BrightnessProcentage = 100;
        double BrightnessProcentage
        {
            set {
                _BrightnessProcentage = value; /*OverlayBlack.Opacity = 1 - (value / 100.0);*/ App.SetBrightness(value / 100);
            }
            get { return _BrightnessProcentage; }
        }
        TouchTracking.TouchTrackingPoint cursorPosition;
        TouchTracking.TouchTrackingPoint startCursorPosition;


        const uint fadeTime = 100;
        const int timeUntilFade = 2500;
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
            print("TOUCH ACCTION0");

            bool lockActionOnly = false;

            void CheckLock()
            {
                if (Player == null) {
                    lockActionOnly = true; return;
                }
                else if (Player.State == VLCState.Error) {
                    lockActionOnly = true; return;
                }
                else if (Player.State == VLCState.Opening) {
                    lockActionOnly = true; return;
                }
                else if (Player.Length == -1) {
                    lockActionOnly = true; return;
                }
                else if (!Player.IsSeekable) {
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

            if (lockActionOnly) return;

            // ========================================== NORMAL LOGIC ==========================================
            if (args.Type == TouchTracking.TouchActionType.Pressed) {
                if (DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) { // Doubble click
                    lastRelease = DateTime.Now;

                    bool forward = (TapRec.Width / 2.0 < args.Location.X);
                    SeekMedia(SKIPTIME * (forward ? 1 : -1));
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
                isMovingStartTime = Player.Time;
                isMovingSkipTime = 0;
                isMovingCursor = false;
                cursorPosition = args.Location;

                maxVol = Volyme >= 100 ? 200 : 100;
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
                        isMovingSkipTime = (long)((Player.Length * (diffX * diffX) / 10) * (diffX < 0 ? -1 : 1)); // EXPONENTIAL SKIP LIKE VLC

                        if (isMovingSkipTime + isMovingStartTime > Player.Length) { // SKIP TO END
                            isMovingSkipTime = Player.Length - isMovingStartTime;
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
                            Volyme -= (args.Location.Y - cursorPosition.Y) / 2.0;
                            Volyme = Math.Max(Math.Min(Volyme, maxVol), 0); // CLAM
                            SkiptimeLabel.Text = $"Volyme {(int)Volyme}%";
                        }
                    }

                    cursorPosition = args.Location;

                }
            }
            else if (args.Type == TouchTracking.TouchActionType.Released) {
                if (isMovingCursor && isMovingHorozontal && Math.Abs(isMovingSkipTime) > 1000) { // SKIP TIME
                    FadeEverything(true);
                    SeekMedia(isMovingSkipTime - Player.Time + isMovingStartTime);
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
            try {
                if (Player == null) return;
                if (Player.State == VLCState.Error) return;
                if (Player.State == VLCState.Opening) return;
                if (Player.Length == -1) return;
                if (!Player.IsSeekable) return;
            }
            catch (Exception _ex) {
                print("ERRORIN TOUCH: " + _ex);
                return;
            }

            print("SEEK MEDIA to " + ms);
            Player.Time = Player.Time + ms;

            PlayerTimeChanged(Player.Time);



        }
    }
}