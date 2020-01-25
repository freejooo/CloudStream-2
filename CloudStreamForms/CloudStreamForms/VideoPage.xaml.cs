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
        public VideoPage()
        {

            InitializeComponent();
            Core.Initialize();

            var libVLC = new LibVLC();

            var media = new Media(libVLC,
                "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4",
                FromType.FromLocation);

            vvideo.MediaPlayer = new MediaPlayer(media) { EnableHardwareDecoding = true };
            vvideo.MediaPlayer.Play();


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
            vvideo.MediaPlayer.Stop();
            base.OnDisappearing();
        }




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
                        _finalTime = vvideo.MediaPlayer.Time + timeDiff;

                        if (WillOverflow)
                            break;

                        Message = FormatSeekingMessage(timeDiff, _finalTime, Direction.Left);
                        _timeChanged = true;
                    }
                    else if (e.TotalX > 0 && Math.Abs(e.TotalX) > Math.Abs(e.TotalY)) {
                        var timeDiff = Convert.ToInt64(e.TotalX * 1000);
                        _finalTime = vvideo.MediaPlayer.Time + timeDiff;

                        if (WillOverflow)
                            break;

                        Message = FormatSeekingMessage(timeDiff, _finalTime, Direction.Right);
                        _timeChanged = true;
                    }
                    else if (e.TotalY < 0 && Math.Abs(e.TotalY) > Math.Abs(e.TotalX)) {
                        var volume = (int)(vvideo.MediaPlayer.Volume + e.TotalY * -1);
                        _finalVolume = VolumeRangeCheck(volume);

                        Message = FormatVolumeMessage(_finalVolume, Direction.Top);
                        _volumeChanged = true;
                    }
                    else if (e.TotalY > 0 && e.TotalY > Math.Abs(e.TotalX)) {
                        var volume = (int)(vvideo.MediaPlayer.Volume + e.TotalY * -1);
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
                        vvideo.MediaPlayer.Time = _finalTime;
                    if (_volumeChanged && vvideo.MediaPlayer.Volume != _finalVolume)
                        vvideo.MediaPlayer.Volume = _finalVolume;

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