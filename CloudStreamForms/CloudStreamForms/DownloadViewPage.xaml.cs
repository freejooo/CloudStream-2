using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DownloadViewPage : ContentPage
    {
        public DownloadViewPage()
        {
            InitializeComponent();
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();


            if (Device.RuntimePlatform == Device.UWP) {
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
              //  DownloadSizeGrid.HeightRequest = 25;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }
        }
    }
}