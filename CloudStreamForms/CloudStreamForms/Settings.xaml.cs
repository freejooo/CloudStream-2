using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.MainPage;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Settings : ContentPage
    {

        LabelList ColorPicker;
        public string MainTxtColor { set; get; } = "#e1e1e1";


        public const string errorEpisodeToast = "No Links Found";
        public static int LoadingMiliSec {
            set {
                App.SetKey("Settings", nameof(LoadingMiliSec), value);
            }
            get {
                return App.GetKey("Settings", nameof(LoadingMiliSec), 5000);
            }
        }
        public static int LoadingChromeSec {
            set {
                App.SetKey("Settings", nameof(LoadingChromeSec), value);
            }
            get {
                return App.GetKey("Settings", nameof(LoadingChromeSec), 30);
            }
        }

        public static bool ListViewPopupAnimation {
            set {
                App.SetKey("Settings", nameof(ListViewPopupAnimation), value);
                FastListViewPopupAnimation = value;
            }
            get {
                return App.GetKey("Settings", nameof(ListViewPopupAnimation), true);
            }
        }

        public static bool FastListViewPopupAnimation { get; private set; }

        public static bool DefaultDub {
            set {
                App.SetKey("Settings", nameof(DefaultDub), value);
            }
            get {
                return App.GetKey("Settings", nameof(DefaultDub), true);
            }
        }

        public static bool SubtitlesEnabled {
            set {
                App.SetKey("Settings", nameof(SubtitlesEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(SubtitlesEnabled), true);
            }
        }

        public static bool HasStatusBar {
            set {
                App.SetKey("Settings", nameof(HasStatusBar), value);
            }
            get {
                return App.GetKey("Settings", nameof(HasStatusBar), false);
            }
        }

        /// <summary>
        /// 0 = Almond black, 1 = Dark, 2 = Netflix, 3 = YouTube
        /// </summary>
        public static int BlackBgType {
            set {
                App.SetKey("Settings", nameof(BlackBgType), value);
            }
            get {
                return App.GetKey("Settings", nameof(BlackBgType), 1);
            }
        }

        /// <summary>
        /// Almond black = 0, Dark = 17, Netflix = 26, YouTube = 40
        /// </summary>
        public static int BlackColor {
            get {
                int[] colors = { 0, 17, 26, 40 };
                return colors[BlackBgType];
            }
        }

        public static Color BlackRBGColor {
            get {
                return Color.FromRgb(BlackColor, BlackColor, BlackColor);
            }
        }


        public static bool ViewHistory {
            set {
                App.SetKey("Settings", nameof(ViewHistory), value);
            }
            get {
                return App.GetKey("Settings", nameof(ViewHistory), true);
            }
        }

        public static bool EpDecEnabled {
            set {
                App.SetKey("Settings", nameof(EpDecEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(EpDecEnabled), true);
            }
        }
        public static bool MovieDecEnabled {
            set {
                App.SetKey("Settings", nameof(MovieDecEnabled), value);
            }
            get {
                return true;
                //return App.GetKey("Settings", nameof(MovieDecEnabled), true);
            }
        }

        public static bool SearchEveryCharEnabled {
            set {
                App.SetKey("Settings", nameof(SearchEveryCharEnabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(SearchEveryCharEnabled), true);
            }
        }

        public static bool CacheData {
            set {
                App.SetKey("Settings", nameof(CacheData), value);
            }
            get {
                return App.GetKey("Settings", nameof(CacheData), true);
            }
        }

        public static bool UseVideoPlayer {
            set {
                App.SetKey("Settings", nameof(UseVideoPlayer), value);
            }
            get {
                return App.GetKey("Settings", nameof(UseVideoPlayer), false);
            }
        }

        public static bool Top100Enabled {
            set {
                App.SetKey("Settings", nameof(Top100Enabled), value);
            }
            get {
                return App.GetKey("Settings", nameof(Top100Enabled), true);
            }
        }

        public string MainColor { get { return Device.RuntimePlatform == Device.UWP ? "#303F9F" : "#515467"; } }
        private static String HexConverter(Color c)
        {
            return c.ToHex(); //"#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
        public static string MainBackgroundColor {
            get {
                if (Device.RuntimePlatform == Device.UWP) {
                    return "#000000";
                    // color = "#000811";
                }

                return HexConverter(Color.FromRgba(BlackColor, BlackColor, BlackColor, 255));
            }
        }

        public static bool HasScrollBar {
            get {
                return Device.RuntimePlatform == Device.UWP;
            }
        }

        public static ScrollBarVisibility ScrollBarVisibility {
            get { return HasScrollBar ? ScrollBarVisibility.Default : ScrollBarVisibility.Never; }
        }

        public static bool CacheImdb { get { return CacheData; } }
        public static bool CacheMAL { get { return CacheData; } }


        VisualElement[] displayElements;
        public Settings()
        {
            InitializeComponent();

            displayElements = new VisualElement[] {
                G_GeneralTxt,
                G_InstatSearch,
                G_Subtitles,
                G_CacheData,
                G_Dubbed,
                G_PauseHistory,
                G_UITxt,
                G_Statusbar,
                G_Top100,
                G_VideoPlayer,
                G_Descript,
                G_Animation,

                G_ClearData,
                ClearHistoryTap,
                ClearBookmarksTap,
                ClearCachedTap,
                ResetallTap,
                G_TimeTxt,
                G_CastTime,
                G_SkipTime,
                G_BuildTxt,
                BuildNumber,
                G_GithubTxt,
                StarMe,
                FeedbackBtt,
                SetTheme,
               // UpdateBtt,
            };

            for (int i = 0; i < displayElements.Length; i++) {
                Grid.SetRow(displayElements[i], i);
            }

            InstantSearchImg.Source = App.GetImageSource("searchIcon.png");
            SubtitlesImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");
            CacheImg.Source = App.GetImageSource("outline_cached_white_48dp.png");
            DubbedImg.Source = App.GetImageSource("outline_record_voice_over_white_48dp.png");
            HistoryImg.Source = App.GetImageSource("outline_history_white_48dp.png");

            StatusbarImg.Source = App.GetImageSource("outline_aspect_ratio_white_48dp.png");
            TopImg.Source = App.GetImageSource("outline_reorder_white_48dp.png");
            DescriptImg.Source = App.GetImageSource("outline_description_white_48dp.png");

            VideoImg.Source = App.GetImageSource("baseline_ondemand_video_white_48dp.png");
            ListAniImg.Source = App.GetImageSource("animation.png");

            var ClearSource = App.GetImageSource("outline_delete_white_48dp.png");
            ClearImg1.Source = ClearSource;
            ClearImg2.Source = ClearSource;
            ClearImg3.Source = ClearSource;

            ResetImg.Source = App.GetImageSource("baseline_refresh_white_48dp.png");


            ColorPicker = new LabelList(SetTheme, new List<string> { "Black", "Dark", "Netflix", "YouTube" });
            //outline_reorder_white_48dp.png


            BackgroundColor = Settings.BlackRBGColor;


            if (Device.RuntimePlatform == Device.UWP) {
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }

            //Main.print("COLOR: "+ BlackBgToggle.OnColor);
            //  if (Device.RuntimePlatform == Device.UWP) {
            BindingContext = this;
            // }
            StarMe.Clicked += (o, e) => {
                App.OpenBrowser("https://github.com/LagradOst/CloudStream-2");
            };
            BuildNumber.Text = "Build Version: " + App.GetBuildNumber();
            Apper();

            HistoryToggle.Toggled += (o, e) => {
                ViewHistory = !e.Value;
            };

            DubbedToggle.Toggled += (o, e) => {
                DefaultDub = e.Value;
            };

            VideoToggle.Toggled += (o, e) => {
                UseVideoPlayer = e.Value;
            };
            //  BlackBgToggle.OnChanged += (o, e) => {
            //     BlackBg = e.Value;
            //  };
            SubtitlesToggle.Toggled += (o, e) => {
                SubtitlesEnabled = e.Value;
            };
            /*
            EpsDecToggle.OnChanged += (o, e) => {
                EpDecEnabled = e.Value;
            };*/
            DescriptToggle.Toggled += (o, e) => {
                EpDecEnabled = e.Value;
            };
            InstantSearchToggle.Toggled += (o, e) => {
                SearchEveryCharEnabled = e.Value;
            };
            CacheDataToggle.Toggled += (o, e) => {
                CacheData = e.Value;
            };
            TopToggle.Toggled += (o, e) => {
                Top100Enabled = e.Value;
            };
            StatusbarToggle.Toggled += (o, e) => {
                HasStatusBar = e.Value;
                App.UpdateStatusBar();
            };
            ListAniToggle.Toggled += (o, e) => {
                ListViewPopupAnimation = e.Value;
            }; 

            Commands.SetTap(ClearHistoryTap, new Command((o) => {
                ClearHistory();
            }));

            Commands.SetTap(ClearCachedTap, new Command((o) => {
                ClearCache();
            }));
            Commands.SetTap(ClearBookmarksTap, new Command((o) => {
                ClearBookmarks();
            }));
            Commands.SetTap(ResetallTap, new Command((o) => {
                ResetToDef();
            }));



            ColorPicker.SelectedIndexChanged += (o, e) => {
                if (ColorPicker.SelectedIndex != -1) {
                    BlackBgType = ColorPicker.SelectedIndex;
                    ColorPicker.button.Text = "Current Theme: " + ColorPicker.button.Text;
                    App.UpdateBackground();
                }
            };

            UpdateBtt.Clicked += (o, e) => {
                if (NewGithubUpdate) {
                    App.DownloadNewGithubUpdate(githubUpdateTag);
                }
            };

            /*
            if (Device.RuntimePlatform != Device.Android) {
                for (int i = 0; i < SettingsTable.Count; i++) {
                    if (i >= SettingsTable.Count) break;
                    if (SettingsTable[i][0] is TextCell) {
                        if (((TextCell)(SettingsTable[i][0])).DetailColor == Color.Transparent) {
                            SettingsTable.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }*/
        }

        const double MAX_LOADING_TIME = 30000;
        const double MIN_LOADING_TIME = 1000;
        const double MIN_LOADING_CHROME = 5;
        const double MAX_LOADING_CHROME = 60;

        const int ROUND_LOADING_DECIMALES = 2;
        const int ROUND_LOADING_CHROME_DECIMALES = 0;
        void Apper()
        {
            Device.BeginInvokeOnMainThread(() => {
                SetSliderTime();
                SetSliderChromeTime();
                LoadingSlider.Value = ((LoadingMiliSec - MIN_LOADING_TIME) / (MAX_LOADING_TIME - MIN_LOADING_TIME));
                CastSlider.Value = ((LoadingChromeSec - MIN_LOADING_CHROME) / (MAX_LOADING_CHROME - MIN_LOADING_CHROME));
                SubtitlesToggle.IsToggled = SubtitlesEnabled;
                DubbedToggle.IsToggled = DefaultDub;
                VideoToggle.IsToggled = UseVideoPlayer;
                HistoryToggle.IsToggled = !ViewHistory;
                DescriptToggle.IsToggled = EpDecEnabled;
                InstantSearchToggle.IsToggled = SearchEveryCharEnabled;
                CacheDataToggle.IsToggled = CacheData;
                StatusbarToggle.IsToggled = HasStatusBar;
                TopToggle.IsToggled = Top100Enabled;
                ColorPicker.SelectedIndex = BlackBgType;
                ListAniToggle.IsToggled = ListViewPopupAnimation;
                FastListViewPopupAnimation = ListViewPopupAnimation;

            });
        }

        protected override void OnDisappearing()
        {
            //  OnIconEnd(3); 
            base.OnDisappearing();
        }

        protected override void OnAppearing()
        {
            //  OnIconStart(3);
            base.OnAppearing();
            Apper();

            if (Device.RuntimePlatform == Device.Android) {
                if (NewGithubUpdate) {
                    Grid.SetRow(UpdateBtt, displayElements.Length);
                }
                UpdateBtt.IsEnabled = NewGithubUpdate;
                UpdateBtt.IsVisible = NewGithubUpdate;
                UpdateBtt.Text = "Update " + App.GetBuildNumber() + " to " + githubUpdateTag.Replace("v", "") + " · " + githubUpdateText;
                BackgroundColor = Settings.BlackRBGColor;
            }
        }


        private void Slider_DragCompleted(object sender, EventArgs e)
        {
            LoadingMiliSec = (int)(Math.Round((Math.Round(((Slider)sender).Value * (MAX_LOADING_TIME - MIN_LOADING_TIME)) + MIN_LOADING_TIME) / Math.Pow(10, ROUND_LOADING_DECIMALES)) * Math.Pow(10, ROUND_LOADING_DECIMALES));
            SetSliderTime();
        }

        private void Slider_DragCompleted2(object sender, EventArgs e)
        {
            LoadingChromeSec = (int)(Math.Round((Math.Round(((Slider)sender).Value * (MAX_LOADING_CHROME - MIN_LOADING_CHROME)) + MIN_LOADING_CHROME) / Math.Pow(10, ROUND_LOADING_CHROME_DECIMALES)) * Math.Pow(10, ROUND_LOADING_CHROME_DECIMALES));
            SetSliderChromeTime();
        }

        void SetSliderTime()
        {
            string s = Math.Round(LoadingMiliSec / 1000.0, 1).ToString();
            if (!s.Contains(".")) {
                s += ".0";
            }
            LoadingTime.Text = "Loading Time: " + s + "s";
        }

        void SetSliderChromeTime()
        {
            CastTime.Text = "Skip time: " + LoadingChromeSec + "s";
        }

        private void TextCell_Tapped(object sender, EventArgs e)
        {
            ClearHistory();
        }
        private void TextCell_Tapped2(object sender, EventArgs e)
        {
            ClearBookmarks();
        }
        private void TextCell_Tapped3(object sender, EventArgs e)
        {
            ClearCache();
        }
        private void TextCell_Tapped4(object sender, EventArgs e)
        {
            ResetToDef();
        }

        async void ClearBookmarks()
        {
            bool action = await DisplayAlert("Clear bookmarks", "Are you sure that you want to remove all bookmarks" + " (" + App.GetKeyCount("BookmarkData") + " items)", "Yes", "Cancel");
            if (action) {
                App.RemoveFolder("BookmarkData");
            }
        }

        async void ClearHistory()
        {
            bool action = await DisplayAlert("Clear watch history", "Are you sure that you want to clear all watch history" + " (" + App.GetKeyCount("ViewHistory") + " items)", "Yes", "Cancel");
            if (action) {
                App.RemoveFolder("ViewHistory");
            }
        }

        async void ClearCache()
        {
            bool action = await DisplayAlert("Clear cached data", "Are you sure that you want to clear all cached data" + " (" + (App.GetKeyCount("CacheMAL") + App.GetKeyCount("CacheImdb")) + " items)", "Yes", "Cancel");
            if (action) {
                App.RemoveFolder("CacheMAL");
                App.RemoveFolder("CacheImdb");
            }
        }
        async void ResetToDef()
        {
            bool action = await DisplayAlert("Reset settings to default", "Are you sure that you want to reset settings to default", "Yes", "Cancel");
            if (action) {
                App.RemoveFolder("Settings");
                Apper();
            }
        }

        private void FeedbackBtt_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new Feedback());
        }
    }
}