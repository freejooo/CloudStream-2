using CloudStreamForms.Core;
using CloudStreamForms.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.MainPage;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Settings : ContentPage
    {
        public const bool SUBTITLES_INVIDEO_ENABELD = true;

        public const bool IS_TEST_BUILD = false;

        LabelList ColorPicker;
        LabelList SubLangPicker;
        LabelList SubStylePicker;
        LabelList SubFontPicker;
        LabelList VideoplayerOptionPicker;

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

        public static int PreferedVideoPlayer {
            set {
                App.SetKey("Settings", nameof(PreferedVideoPlayer), value);
            }
            get {
                return App.GetKey("Settings", nameof(PreferedVideoPlayer), -1);
            }
        }

        public static bool SubtitlesHasDropShadow {
            get {
                return SubtitleType == 1 || SubtitleType == 3;
            }
        }

        public static bool SubtitlesHasOutline {
            get {
                return SubtitleType >= 2;
            }
        }

        /// <summary>
        /// 0 = None, 1 = Dropshadow, 2 = Outline, 3 = Outline + dropshadow 
        /// </summary>
        public static int SubtitleType {
            set {
                App.SetKey("Settings", nameof(SubtitleType), value);
            }
            get {
                return App.GetKey("Settings", nameof(SubtitleType), 1);
            }
        }

        public static readonly List<string> GlobalFonts = new List<string>() { "Trebuchet MS", "Open Sans", "Google Sans", "Lucida Grande", "Verdana", "Futura", "STIXGeneral", "Times New Roman", "App default" }; //ARIAL IS TO BIG

        public static string GlobalSubtitleFont {
            set {
                App.SetKey("Settings", nameof(GlobalSubtitleFont), value);
            }
            get {
                // return "Trebuchet MS";
                return App.GetKey("Settings", nameof(GlobalSubtitleFont), GlobalFonts[0]); // Trebuchet MS, Open Sans, Google Sans
            }
        }


        public static bool HasAccountLogin {
            set {
                App.SetKey("Account", nameof(HasAccountLogin), value);
            }
            get {
                return App.GetKey("Account", nameof(HasAccountLogin), false);
            }
        }

        public static string AccountUsername {
            set {
                App.SetKey("Account", nameof(AccountUsername), value);
            }
            get {
                return App.GetKey("Account", nameof(AccountUsername), "");
            }
        }

        public static string AccountSite {
            set {
                App.SetKey("Account", nameof(AccountSite), value);
            }
            get {
                return App.GetKey("Account", nameof(AccountSite), "");
            }
        }

        public static string AccountPassword {
            set {
                App.SetKey("Account", nameof(AccountPassword), value);
            }
            get {
                return App.GetKey("Account", nameof(AccountPassword), "");
            }
        }





        public static string NativeSubShortName {
            get {
                return CloudStreamCore.subtitleShortNames[NativeSubtitles];
            }
        }

        public static string NativeSubLongName {
            get {
                return CloudStreamCore.subtitleNames[NativeSubtitles];
            }
        }

        public static int NativeSubtitles {
            set {
                App.SetKey("Settings", nameof(NativeSubtitles), value);
            }
            get {
                return App.GetKey("Settings", nameof(NativeSubtitles), CloudStreamCore.subtitleNames.IndexOf("English"));
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
                return App.GetKey("Settings", nameof(ListViewPopupAnimation), false);
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

        public static bool UseAniList {
            set {
                App.SetKey("Settings", nameof(UseAniList), value);
            }
            get {
                return App.GetKey("Settings", nameof(UseAniList), false);
            }
        }

        public static bool UseVideoPlayer {
            set {
                App.SetKey("Settings", nameof(UseVideoPlayer), value);
            }
            get {
                return App.GetKey("Settings", nameof(UseVideoPlayer), true);
            }
        }

        public static bool LazyLoadNextLink {
            set {
                App.SetKey("Settings", nameof(LazyLoadNextLink), value);
            }
            get {
                return App.GetKey("Settings", nameof(LazyLoadNextLink), true);
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

        static bool initVideoPlayer = true;
        VisualElement[] displayElements;
        public Settings()
        {
            InitializeComponent();

            displayElements = new VisualElement[] {
                G_GeneralTxt,
                G_InstatSearch,
                G_Subtitles,
                G_CacheData,
                G_AniList,
                G_Dubbed,
                G_Lazyload,
                G_PauseHistory,

                SubtitleStyle,
                SubtitleLang,
                SubtitleFont,

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
                ManageAccount,
                VideoplayerOption,
                SetTheme,
               // UpdateBtt,
            };

            for (int i = 0; i < displayElements.Length; i++) {
                Grid.SetRow(displayElements[i], i);
            }

            InstantSearchImg.Source = App.GetImageSource("searchIcon.png");
            SubtitlesImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");
            CacheImg.Source = App.GetImageSource("outline_cached_white_48dp.png");
            AniListImg.Source = App.GetImageSource("outline_cached_white_48dp.png");
            DubbedImg.Source = App.GetImageSource("outline_record_voice_over_white_48dp.png");
            HistoryImg.Source = App.GetImageSource("outline_history_white_48dp.png");

            StatusbarImg.Source = App.GetImageSource("outline_aspect_ratio_white_48dp.png");
            TopImg.Source = App.GetImageSource("outline_reorder_white_48dp.png");
            DescriptImg.Source = App.GetImageSource("outline_description_white_48dp.png");

            VideoImg.Source = App.GetImageSource("baseline_ondemand_video_white_48dp.png");
            LazyLoadImg.Source = App.GetImageSource("baseline_ondemand_video_white_48dp.png");

            ListAniImg.Source = App.GetImageSource("animation.png");

            var ClearSource = App.GetImageSource("outline_delete_white_48dp.png");
            ClearImg1.Source = ClearSource;
            ClearImg2.Source = ClearSource;
            ClearImg3.Source = ClearSource;

            ResetImg.Source = App.GetImageSource("baseline_refresh_white_48dp.png");


            ColorPicker = new LabelList(SetTheme, new List<string> { "Black", "Dark", "Netflix", "YouTube" }, "Select Theme");
            SubLangPicker = new LabelList(SubtitleLang, CloudStreamCore.subtitleNames.ToList(), "Default Subtitle Language");
            SubStylePicker = new LabelList(SubtitleStyle, new List<string>() { "None", "Dropshadow", "Outline", "Outline + dropshadow" }, "Subtitle Style");
            SubFontPicker = new LabelList(SubtitleFont, GlobalFonts, "Subtitle Font", true);
             
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

            LazyLoadToggle.Toggled += (o, e) => {
                LazyLoadNextLink = e.Value;
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
            AniListToggle.Toggled += (o, e) => {
                UseAniList = e.Value;
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

            SubStylePicker.SelectedIndexChanged += (o, e) => {
                if (SubStylePicker.SelectedIndex != -1) {
                    SubtitleType = SubStylePicker.SelectedIndex;
                    SubStylePicker.button.Text = "Subtitle Style: " + SubStylePicker.button.Text;
                }
            };

            SubFontPicker.SelectedIndexChanged += (o, e) => {
                if (SubFontPicker.SelectedIndex != -1) {
                    GlobalSubtitleFont = SubFontPicker.ItemsSource[SubFontPicker.SelectedIndex];
                    SubFontPicker.button.Text = "Subtitle Font: " + SubFontPicker.button.Text;
                }
            };


            SubLangPicker.SelectedIndexChanged += (o, e) => {
                if (SubLangPicker.SelectedIndex != -1) {
                    NativeSubtitles = SubLangPicker.SelectedIndex;
                    SubLangPicker.button.Text = "Default subtitle language: " + SubLangPicker.button.Text;
                }
            };

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

            ManageAccount.Clicked += async (o, e) => {
                List<string> actions = new List<string>() { };
                // TODO ADD LOGIN
                /*
                if (HasAccountLogin) {
                    actions.Add("Logout from " + AccountUsername);
                }
                else {
                    actions.Add("Create account");
                    actions.Add("Login");
                }*/
                actions.AddRange(new string[] { "Export data", "Export Everything", "Import data", });

                string action = await ActionPopup.DisplayActionSheet("Manage account", actions.ToArray());
                if (action == "Export data" || action == "Export Everything") {
                    string subaction = await ActionPopup.DisplayEntry(InputPopupPage.InputPopupResult.password, "Password", "Encrypt data", autoPaste: false, confirmText: "Encrypt");
                    
                    if (subaction != "Cancel") {
                        string text = Script.SyncWrapper.GenerateTextFile(action == "Export Everything");
                        if (subaction != "") {
                            text = CloudStreamForms.Cryptography.StringCipher.Encrypt(text, subaction);
                        }
                        string fileloc = App.DownloadFile(text, App.DATA_FILENAME, true);
                        if (fileloc != "") {
                            App.ShowToast($"Saved data to {fileloc}");
                        }
                    }
                }
                else if (action == "Import data") {
                    string file = App.ReadFile(App.DATA_FILENAME, true, "");
                    if (file == "") {
                        App.ShowToast("No file found");
                    }
                    else {
                        if (file.StartsWith(Script.SyncWrapper.header)) {
                            string import = await ActionPopup.DisplayActionSheet("Override Current Data", "Yes, import data and override current", "No, dont override current data");
                            if (import.StartsWith("Y")) {
                                Script.SyncWrapper.SetKeysFromTextFile(file);
                                App.ShowToast("File loaded");
                                Apper();
                            }
                        }
                        else {
                            bool success = false;
                            while (!success) {
                                string password = await ActionPopup.DisplayEntry(InputPopupPage.InputPopupResult.password, "Password", "Decrypt data", autoPaste: false, confirmText: "Decrypt");
                                CloudStreamCore.print("PASSWORDD:D::: " + password);
                                if (password != "Cancel" && password != "") {
                                    string subFile = CloudStreamForms.Cryptography.StringCipher.Decrypt(file, password);
                                    CloudStreamCore.print("SUBFILE::: " + subFile);
                                    success = subFile.StartsWith(Script.SyncWrapper.header);
                                    if (success) {
                                        string import = await ActionPopup.DisplayActionSheet("Override Current Data", "Yes, import data and override current", "No, dont override current data");
                                        if (import.StartsWith("Y")) {
                                            Script.SyncWrapper.SetKeysFromTextFile(subFile);
                                            App.ShowToast("File dectypted and loaded");
                                            Apper();
                                        }
                                    }
                                    else {
                                        App.ShowToast("Failed to dectypted file");
                                        await System.Threading.Tasks.Task.Delay(200);
                                    }
                                }
                                else {
                                    success = true;
                                }
                            }

                        }
                    }
                }
                else if (action.StartsWith("Logout from")) {

                }
                else if (action == "Login") {
                    bool tryLogin = true;
                    string password = "";
                    string username = "";
                    string site = "";
                    while (tryLogin) {
                        List<string> data = await ActionPopup.DisplayLogin("Login", "Cancel", "Login to account", new LoginPopupPage.PopupFeildsDatas() { placeholder = "Server Url", setText = site }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Username", setText = username }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Password", isPassword = true, setText = password });
                        if (data.Count == 3) {
                            site = data[0];
                            username = data[1];
                            password = data[2];
                            await ActionPopup.StartIndeterminateLoadinbar("Tryig to login...");
                            string logindata = GetAccountResponse(site, username, password, Logintype.LoginAccount, out LoginErrorType error);
                            await ActionPopup.StopIndeterminateLoadinbar();

                            App.ShowToast(error.ToString());
                        }
                        else {
                            tryLogin = false;
                        }
                    }


                }
                else if (action == "Create account") {

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
                AniListToggle.IsToggled = UseAniList;
                ColorPicker.SelectedIndex = BlackBgType;
                SubLangPicker.SelectedIndex = NativeSubtitles;
                SubStylePicker.SelectedIndex = SubtitleType;
                SubFontPicker.SelectedIndex = GlobalFonts.IndexOf(GlobalSubtitleFont);
                LazyLoadToggle.IsToggled = LazyLoadNextLink;

                ListAniToggle.IsToggled = ListViewPopupAnimation;
                FastListViewPopupAnimation = ListViewPopupAnimation;

            });
        }

        protected override void OnDisappearing()
        {
            //  OnIconEnd(3); 
            base.OnDisappearing();
        }

        void InitVideoPlayer()
        {  
            // VIDEOPLAYER Option
            List<string> videoOptions = new List<string>() { App.GetVideoPlayerName(App.VideoPlayer.None) };
            List<App.VideoPlayer> avalibePlayers = new List<App.VideoPlayer>();
            foreach (var player in App.GetEnumList<App.VideoPlayer>()) {
                if (App.GetVideoPlayerInstalled(player)) {
                    avalibePlayers.Add(player);
                    videoOptions.Add(App.GetVideoPlayerName(player));
                }
            }
            initVideoPlayer = false;
            VideoplayerOptionPicker = new LabelList(VideoplayerOption, videoOptions, "External video player");
            int index = videoOptions.IndexOf(App.GetVideoPlayerName((App.VideoPlayer)Settings.PreferedVideoPlayer));
            if (index == -1) {
                if (avalibePlayers.Count > 0) {
                    index = 1;
                    // PreferedVideoPlayer = (int)avalibePlayers[0];
                }
            }

            VideoplayerOptionPicker.SelectedIndexChanged += (o, e) => {
                if (e != -1) {
                    Settings.PreferedVideoPlayer = e - 1; // 0 = no videoplayer
                    VideoplayerOptionPicker.button.Text = "Current Videoplayer: " + VideoplayerOptionPicker.button.Text;
                }
            };

            VideoplayerOptionPicker.SelectedIndex = index == -1 ? 0 : index;
        }

        protected override void OnAppearing()
        {
            //  OnIconStart(3);
            base.OnAppearing();
            Apper();

            if (initVideoPlayer) {
                InitVideoPlayer();
            }
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
                App.RemoveFolder(App.BOOKMARK_DATA);
            }
        }

        async void ClearHistory()
        {
            bool action = await DisplayAlert("Clear watch history", "Are you sure that you want to clear all watch history" + " (" + App.GetKeyCount("ViewHistory") + " items)", "Yes", "Cancel");
            if (action) {
                App.RemoveFolder(App.VIEW_HISTORY);
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
            bool action = await DisplayAlert("Reset settings to default", "Are you sure that you want to reset settings to default", "Yes", "Cancel");//await ActionPopup.DisplayActionSheet("Reset settings to default", "Clear settings") == "Clear settings"; //
            if (action) {
                App.RemoveFolder("Settings");
                Apper();
            }
        }

        private void FeedbackBtt_Clicked(object sender, EventArgs e)
        {
            Navigation.PushModalAsync(new Feedback());
        }


        public enum Logintype
        {
            CreateAccount = 0,
            LoginAccount = 1,
            EditAccount = 2,
        }

        public enum LoginErrorType
        {
            Ok = 0,
            InternetError = 1,
            WrongPassword = 2,
            UsernameTaken = 3,
        }

        static Random rng = new Random();
        public static string GetAccountResponse(string url, string name, string password, Logintype logintype, out LoginErrorType result, string data = "")
        {
            result = LoginErrorType.InternetError;
            try {
                int waitTime = 400;
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                if (CloudStreamCore.GetRequireCert(url)) { webRequest.ServerCertificateValidationCallback = delegate { return true; }; }
                webRequest.Method = "GET";
                webRequest.Timeout = waitTime * 10;
                webRequest.ReadWriteTimeout = waitTime * 10;
                webRequest.ContinueTimeout = waitTime * 10;
                webRequest.Headers.Add("LOGINTYPE", ((int)logintype).ToString());

                webRequest.Headers.Add("NAME", StringCipher.HashData(name));
                if (logintype == Logintype.CreateAccount) {
                    webRequest.Headers.Add("HASHPASSWORD", StringCipher.HashData(password));
                }
                else {
                    webRequest.Headers.Add("ONETIMEPASSWORD", StringCipher.Encrypt(rng.Next(0, int.MaxValue) + "CORRECTPASS[" + DateTime.Now.ToBinary() + "]", StringCipher.HashData(password)));
                }
                if (logintype != Logintype.LoginAccount) {
                    webRequest.Headers.Add("DATA", StringCipher.Encrypt(data, password));
                }

                using (var webResponse = webRequest.GetResponse()) {
                    try {
                        using (StreamReader httpWebStreamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8)) {
                            try {
                                string s = httpWebStreamReader.ReadToEnd();
                                if (s.StartsWith("OKDATA")) {
                                    result = LoginErrorType.Ok;
                                    if (logintype == Logintype.LoginAccount) {
                                        return StringCipher.Decrypt(s.Split('\n')[1], password);
                                    }
                                    else {
                                        return "";
                                    }
                                }
                                else {
                                    result = (LoginErrorType)int.Parse(CloudStreamCore.FindHTML(s, "ERRORCODE[", "]"));
                                    return "";
                                }
                                //   _s = httpWebStreamReader.ReadToEnd();
                                //  done = true;
                            }
                            catch (Exception _ex) {
                                CloudStreamCore.error(_ex);
                            }

                        }
                    }
                    catch (Exception) {
                        return "";
                    }

                }
                return "";
            }
            catch (Exception _ex) {
                CloudStreamCore.error(_ex);
                return "";
            }

        }

    }
}