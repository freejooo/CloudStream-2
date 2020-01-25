using LibVLCSharp.Forms.Shared;
using LibVLCSharp.Shared;
using MediaElement;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
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

        public VideoPage()
        {

            InitializeComponent();
            Core.Initialize();


            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };

            vvideo.MediaPlayer = _mediaPlayer; // = new VideoView() { MediaPlayer = _mediaPlayer };
            var media = new Media(_libVLC, "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", FromType.FromLocation);
            vvideo.MediaPlayer.Play(media);



            PausePlayBtt.Source = App.GetImageSource(PAUSE_IMAGE);

            void SetIsPaused(bool paused)
            {
                PausePlayBtt.Source = App.GetImageSource(paused ? PLAY_IMAGE : PAUSE_IMAGE);
                PausePlayBtt.IsVisible = true;
                LoadingCir.IsVisible = false;
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

            };
            Player.Buffering += (o, e) => {
                Device.BeginInvokeOnMainThread(() => {
                    if (e.Cache == 100) {
                        SetIsPaused(!Player.IsPlaying);
                    }
                    else {
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
            Player.Pause();
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

    }
}