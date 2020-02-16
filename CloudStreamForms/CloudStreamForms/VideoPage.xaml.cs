using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        const string PLAY_IMAGE = "baseline_play_arrow_white_48dp.png";
        const string PAUSE_IMAGE = "baseline_pause_white_48dp.png";

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
            //  GradientBottom.Source = App.GetImageSource("gradient.png");
            // DownloadImg.Source = App.GetImageSource("netflixEpisodesCut.png");//App.GetImageSource("round_more_vert_white_48dp.png");
            LockImg.Source = App.GetImageSource("wlockUnLocked.png");
            Commands.SetTap(EpisodesTap, new Command(() => {
                //do something
                print("Hello");
            }));
            //Commands.SetTapParameter(view, someObject);
            // ================================================================================ UI ================================================================================
            PausePlayBtt.Source = App.GetImageSource(PAUSE_IMAGE);

            void SetIsPaused(bool paused)
            {
                PausePlayBtt.Source = App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
                PausePlayBtt.IsVisible = true;
                LoadingCir.IsVisible = false;
                BufferBar.IsVisible = false;

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
                Device.BeginInvokeOnMainThread(() => {

                    double val = ((double)(Player.Time / 1000) / (double)(Player.Length / 1000));
                    ChangeTime(val);
                    VideoSlider.Value = val;
                });
            };


            Player.Buffering += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    if (e.Cache == 100) {
                        SetIsPaused(!Player.IsPlaying);
                    }
                    else {
                        BufferBar.Progress = e.Cache / 100;
                        BufferBar.IsVisible = true;
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
            Hide();

        }

        async void Hide()
        {
            await Task.Delay(100);
            App.HideStatusBar();
        }


        protected override void OnDisappearing()
        {
            App.ShowStatusBar();
            Player.Stop();
            Player.Dispose();
            base.OnDisappearing();
        }


        public void PausePlayBtt_Clicked(object sender, EventArgs e)
        {
            //Player.SetPause(true);
            if (!dragingVideo) {
                Player.Pause();
            }
        }


        // =========================================================================== LOGIC ====================================================================================================


        string _message = string.Empty;

        long _finalTime;
        bool _timeChanged;

        int _finalVolume;
        bool _volumeChanged;
        bool WillOverflow => _finalTime > TimeSpan.MaxValue.TotalMilliseconds || _finalTime < TimeSpan.MinValue.TotalMilliseconds;
        string FormatSeekingMessage(long timeDiff, long finalTime, Direction direction)
        {
            var timeDiffTimeSpan = TimeSpan.FromMilliseconds((double)new decimal(timeDiff));
            var finalTimeSpan = TimeSpan.FromMilliseconds((double)new decimal(finalTime));
            var diffSign = direction == Direction.Right ? "+" : "-";
            return $"Seeking ({direction} swipe): {diffSign}{timeDiffTimeSpan.Minutes}:{Math.Abs(timeDiffTimeSpan.Seconds)} ({finalTimeSpan.Minutes}:{Math.Abs(finalTimeSpan.Seconds)})";
        }

        string FormatVolumeMessage(int volume, Direction direction) => $"Volume ({direction} swipe): {volume}%";

        int VolumeRangeCheck(int volume)
        {
            if (volume < 0)
                volume = 0;
            else if (volume > 200)
                volume = 200;
            return volume;
        }
        public string Message
        {
            get => _message;
            set { Set(nameof(Message), ref _message, value); SeekTxt.Text = value; }
        }
        private void Set<T>(string propertyName, ref T field, T value)
        {
            if (field == null && value != null || field != null && !field.Equals(value)) {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
        {
            //  float brighness = vvideo.MediaPlayer.AdjustFloat(VideoAdjustOption.Brightness);
            // print("BRIGHNESS::" + brighness);
            //vvideo.MediaPlayer.SetAdjustFloat(VideoAdjustOption.Brightness, 0);
            switch (e.StatusType) {
                case GestureStatus.Running:
                    if (e.TotalX < 0 && Math.Abs(e.TotalX) > Math.Abs(e.TotalY)) {
                        var timeDiff = Convert.ToInt64(e.TotalX * 1000);
                        _finalTime = Player.Time + timeDiff;

                        if (WillOverflow)
                            break;

                        Message = FormatSeekingMessage(timeDiff, _finalTime, Direction.Left);
                        _timeChanged = true;
                    }
                    else if (e.TotalX > 0 && Math.Abs(e.TotalX) > Math.Abs(e.TotalY)) {
                        var timeDiff = Convert.ToInt64(e.TotalX * 1000);
                        _finalTime = Player.Time + timeDiff;

                        if (WillOverflow)
                            break;

                        Message = FormatSeekingMessage(timeDiff, _finalTime, Direction.Right);
                        _timeChanged = true;
                    }
                    else if (e.TotalY < 0 && Math.Abs(e.TotalY) > Math.Abs(e.TotalX)) {
                        var volume = (int)(Player.Volume + e.TotalY * -1);
                        _finalVolume = VolumeRangeCheck(volume);

                        Message = FormatVolumeMessage(_finalVolume, Direction.Top);
                        _volumeChanged = true;
                    }
                    else if (e.TotalY > 0 && e.TotalY > Math.Abs(e.TotalX)) {
                        var volume = (int)(Player.Volume + e.TotalY * -1);
                        _finalVolume = VolumeRangeCheck(volume);

                        Message = FormatVolumeMessage(_finalVolume, Direction.Bottom);
                        _volumeChanged = true;
                    }
                    break;
                case GestureStatus.Started:
                case GestureStatus.Canceled:
                    Message = string.Empty;
                    break;
                case GestureStatus.Completed:
                    if (_timeChanged)
                        Player.Time = _finalTime;
                    if (_volumeChanged && Player.Volume != _finalVolume)
                        Player.Volume = _finalVolume;

                    Message = string.Empty;
                    _timeChanged = false;
                    _volumeChanged = false;
                    break;
            }

        }
        enum Direction
        {
            Left,
            Right,
            Top,
            Bottom
        }

        static bool dragingVideo = false;
        private void VideoSlider_DragStarted(object sender, EventArgs e)
        {
            Player.SetPause(true);
            dragingVideo = true;
        }

        private void VideoSlider_DragCompleted(object sender, EventArgs e)
        {
            long len = (long)((VideoSlider.Value * (double)(Player.Length / 1000)) * 1000);
            Player.Time = len;
            Player.SetPause(false);
            dragingVideo = false;
        }

        private void VideoSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            ChangeTime(e.NewValue);
        }
    }
}