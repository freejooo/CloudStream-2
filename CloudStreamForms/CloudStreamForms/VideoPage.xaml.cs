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

        MediaPlayer Player { get { return vvideo.MediaPlayer; } set { vvideo.MediaPlayer = value; } }

        LibVLC _libVLC;
        MediaPlayer _mediaPlayer;

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
        public static string BeforeAddToName { get { return IsSeries ? ("S" + currentVideo.season + ":E" + currentVideo.episode + " ") : ""; } }
        public static string CurrentDisplayName { get { return BeforeAddToName + (IsSeries ? ADD_BEFORE_EPISODE : "") + currentVideo.name + (IsSeries ? ADD_AFTER_EPISODE : ""); } }
        public static string CurrentMirrorName { get { return currentVideo.MirrorNames[currentMirrorId]; } }
        public static string CurrentMirrorUrl { get { return currentVideo.MirrorUrls[currentMirrorId]; } }
        public static string CurrentSubtitles { get { if (currentSubtitlesId == -1) { return ""; } else { return currentVideo.Subtitles[currentMirrorId]; } } }
        public static string CurrentSubtitlesNames { get { if (currentSubtitlesId == -1) { return NONE_SUBTITLES; } else { return currentVideo.SubtitlesNames[currentMirrorId]; } } }
        public static List<string> AllSubtitlesNames { get { var f = new List<string>() { NONE_SUBTITLES }; f.AddRange(currentVideo.SubtitlesNames); return f; } }
        public static List<string> AllSubtitlesUrls { get { var f = new List<string>() { "" }; f.AddRange(currentVideo.Subtitles); return f; } }
        public static List<string> AllMirrorsNames { get { return currentVideo.MirrorNames; } }
        public static List<string> AllMirrorsUrls { get { return currentVideo.MirrorUrls; } }

        public void SelectMirror(int mirror)
        {
            currentMirrorId = mirror;
            var media = new Media(_libVLC, CurrentMirrorUrl, FromType.FromLocation);
            vvideo.MediaPlayer.Play(media);

            EpisodeLabel.Text = CurrentDisplayName;
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
                double val = ((double)(time / 1000) / (double)(Player.Length / 1000));
                ChangeTime(val);
                VideoSlider.Value = val;
            });
        }


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

            InitializeComponent();
            Core.Initialize();

            SkipForward.TranslationX = TRANSLATE_START_X;
            SkipForwardImg.Source = App.GetImageSource("netflixSkipForward.png");
            SkipForwardBtt.TranslationX = TRANSLATE_START_X;
            Commands.SetTap(SkipForwardBtt, new Command(() => {
                SeekMedia(SKIPTIME);
                SkipFor();
            }));

            SkipBack.TranslationX = -TRANSLATE_START_X;
            SkipBackImg.Source = App.GetImageSource("netflixSkipBack.png");
            SkipBackBtt.TranslationX = -TRANSLATE_START_X;
            Commands.SetTap(SkipBackBtt, new Command(() => {
                SeekMedia(-SKIPTIME);
                SkipBac();
            }));



            // ======================= SETUP =======================

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };

            vvideo.MediaPlayer = _mediaPlayer; // = new VideoView() { MediaPlayer = _mediaPlayer };

            SelectMirror(0);


            // ========== IMGS ==========
            // SubtitlesImg.Source = App.GetImageSource("netflixSubtitlesCut.png"); //App.GetImageSource("baseline_subtitles_white_48dp.png");
            MirrosImg.Source = App.GetImageSource("baseline_playlist_play_white_48dp.png");
            EpisodesImg.Source = App.GetImageSource("netflixEpisodesCut.png");
            NextImg.Source = App.GetImageSource("baseline_skip_next_white_48dp.png");
            BacktoMain.Source = App.GetImageSource("baseline_keyboard_arrow_left_white_48dp.png");

            //  GradientBottom.Source = App.GetImageSource("gradient.png");
            // DownloadImg.Source = App.GetImageSource("netflixEpisodesCut.png");//App.GetImageSource("round_more_vert_white_48dp.png");

            LockImg.Source = App.GetImageSource("wlockUnLocked.png");
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
            }));




            void SetIsPaused(bool paused)
            {
                PausePlayBtt.Source = App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
                PausePlayBtt.IsVisible = true;
                LoadingCir.IsVisible = false;
                BufferLabel.IsVisible = false;

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
                        PausePlayBtt.IsVisible = false;
                        LoadingCir.IsVisible = true;
                    }
                });

            };
            Player.EncounteredError += (o, e) => {
                // SKIP TO NEXT
                App.ShowToast("Error when loading media");
            };
            //  Player.AddSlave(MediaSlaveType.Subtitle,"") // ADD SUBTITLEs
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
            App.ToggleFullscreen(true);
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
            App.ShowStatusBar();
            App.NormalOrientation();
            App.ToggleFullscreen(!Settings.HasStatusBar);

            Player.Stop();
            Player.Dispose();
            base.OnDisappearing();
        }


        async void Hide()
        {
            await Task.Delay(100);
            App.HideStatusBar();
        }

        public void PausePlayBtt_Clicked(object sender, EventArgs e)
        {
            //Player.SetPause(true);
            if (!dragingVideo) {
                Player.Pause();
            }
        }


        // =========================================================================== LOGIC ====================================================================================================




        static bool dragingVideo = false;
        private void VideoSlider_DragStarted(object sender, EventArgs e)
        {
            Player.SetPause(true);
            dragingVideo = true;
            SlideChangedLabel.IsVisible = true;
            SlideChangedLabel.Text = "";
        }

        private void VideoSlider_DragCompleted(object sender, EventArgs e)
        {
            long len = (long)(VideoSlider.Value * Player.Length);
            Player.Time = len;
            Player.SetPause(false);
            dragingVideo = false;
            SlideChangedLabel.IsVisible = false;
            SlideChangedLabel.Text = "";
            SkiptimeLabel.Text = "";
        }

        private void VideoSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            ChangeTime(e.NewValue);
            long timeChange = (long)(VideoSlider.Value * Player.Length) - Player.Time;
            if (dragingVideo) {
                SlideChangedLabel.TranslationX = ((e.NewValue - 0.5)) * (VideoSlider.Width - 30);

                //    var time = TimeSpan.FromMilliseconds(timeChange);

                string before = (timeChange > 0 ? "+" : "-") + ConvertTimeToString(Math.Abs(timeChange / 1000)); //+ (int)time.Seconds + "s";

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

            SkipForward.IsVisible = true;
            SkipForwardSmall.IsVisible = false;
            SkipForward.TranslationX = TRANSLATE_START_X;

            await SkipForward.TranslateTo(TRANSLATE_START_X + TRANSLATE_SKIP_X, SkipForward.TranslationY, 200, Easing.SinInOut);

            SkipForward.TranslationX = TRANSLATE_START_X;
            SkipForward.IsVisible = false;
            SkipForwardSmall.IsVisible = true;
        }


        async void SkipBac()
        {
            SkipBack.AbortAnimation("TranslateTo");
            SkipBacAni();

            SkipBack.IsVisible = true;
            SkipBackSmall.IsVisible = false;
            SkipBack.TranslationX = -TRANSLATE_START_X;

            await SkipBack.TranslateTo(-TRANSLATE_START_X - TRANSLATE_SKIP_X, SkipBack.TranslationY, 200, Easing.SinInOut);

            SkipBack.TranslationX = -TRANSLATE_START_X;
            SkipBack.IsVisible = false;
            SkipBackSmall.IsVisible = true;
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




        private void TouchEffect_TouchAction(object sender, TouchTracking.TouchActionEventArgs args)
        {
            if (Player.Length == -1) return;

            if (args.Type == TouchTracking.TouchActionType.Pressed) {
                if (DateTime.Now.Subtract(lastClick).TotalSeconds < 0.25) { // Doubble click
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
            }

            if (args.Type == TouchTracking.TouchActionType.Released) {
                if (isMovingCursor && isMovingHorozontal && Math.Abs(isMovingSkipTime) > 1000) { // SKIP TIME
                    SeekMedia(isMovingSkipTime - Player.Time + isMovingStartTime);
                }
                SkiptimeLabel.Text = "";
                isMovingCursor = false;
            }

            if (args.Type == TouchTracking.TouchActionType.Pressed) {
                startCursorPosition = args.Location;
                isMovingFromLeftSide = (TapRec.Width / 2.0 > args.Location.X);
                isMovingStartTime = Player.Time;
                isMovingSkipTime = 0;
                isMovingCursor = false;
                cursorPosition = args.Location;

                maxVol = Volyme >= 100 ? 200 : 100;

            }

            if (args.Type == TouchTracking.TouchActionType.Moved) {
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
                        SkiptimeLabel.Text = $"{CloudStreamCore.ConvertTimeToString((isMovingStartTime + isMovingSkipTime) / 1000)} [{(isMovingSkipTime > 0 ? "+" : "-")}{CloudStreamCore.ConvertTimeToString(Math.Abs(isMovingSkipTime / 1000))}]";
                    }
                    else {
                        if (isMovingFromLeftSide) {
                            if (canChangeBrightness) {
                                BrightnessProcentage -= args.Location.Y - cursorPosition.Y;
                                BrightnessProcentage = Math.Max(Math.Min(BrightnessProcentage, 100), 0); // CLAM
                                SkiptimeLabel.Text = $"Brightness {(int)BrightnessProcentage}%";
                            }
                        }
                        else {
                            Volyme -= args.Location.Y - cursorPosition.Y;
                            Volyme = Math.Max(Math.Min(Volyme, maxVol), 0); // CLAM
                            SkiptimeLabel.Text = $"Volyme {(int)Volyme}%";
                        }
                    }

                    cursorPosition = args.Location;

                }
            }

            print("LEFT TIGHT " + (TapRec.Width / 2.0 < args.Location.X) + TapRec.Width + "|" + TapRec.X);
            print("TOUCHED::D:A::A" + args.Location.X + "|" + args.Type.ToString());
        }




        void SeekMedia(long ms)
        {
            Player.Time = Player.Time + ms;
            PlayerTimeChanged(Player.Time);
        }
    }
}