using LibVLCSharp.Shared;
using MediaElement;
using System;
using System.Collections.Generic;
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

    }
}