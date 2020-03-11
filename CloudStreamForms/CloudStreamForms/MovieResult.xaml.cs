using Acr.UserDialogs;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.CloudStreamCore;
using static CloudStreamForms.MainPage;
using static CloudStreamForms.Settings;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MovieResult : ContentPage
    {
        const uint FATE_TIME_MS = 500;

        public int SmallFontSize { get; set; } = 11;
        public int WithSize { get; set; } = 50;
        List<Episode> CurrentEpisodes { set { currentMovie.episodes = value; } get { return currentMovie.episodes; } }

        public MovieResultMainEpisodeView epView;

        LabelList SeasonPicker;
        LabelList DubPicker;
        LabelList FromToPicker;


        public Poster mainPoster;
        public string trailerUrl = "";
        List<Button> recBtts = new List<Button>();

        public static List<Movie> lastMovie;
        List<Poster> RecomendedPosters { set { currentMovie.title.recomended = value; } get { return currentMovie.title.recomended; } }  //= new List<Poster>();

        bool loadedTitle = false;
        int currentSeason = 0;
        //ListView episodeView;
        public const int heightRequestPerEpisode = 120;
        public const int heightRequestAddEpisode = 40;
        public const int heightRequestAddEpisodeAndroid = 0;

        bool isMovie = false;
        Movie currentMovie = new Movie();
        bool isDub = true;
        bool RunningWindows { get { return DeviceInfo.Platform == DevicePlatform.UWP; } }
        string CurrentMalLink
        {
            get {

                try {
                    string s = currentMovie.title.MALData.seasonData[currentSeason].malUrl;
                    if (s != "https://myanimelist.net") {
                        return s;
                    }
                    else {
                        return "";
                    }
                }
                catch (Exception) {
                    return "";
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            Search.mainPoster = new Poster();
            if (lastMovie != null) {
                if (lastMovie.Count > 1) {
                    activeMovie = lastMovie[lastMovie.Count - 1];
                    lastMovie.RemoveAt(lastMovie.Count - 1);
                }
            }
            if (setKey) {
                App.RemoveKey("BookmarkData", currentMovie.title.id);
            }
            return base.OnBackButtonPressed();

            //     Navigation.PopModalAsync(false);
            //     return true;
        }



        bool setKey = false;
        void SetKey()
        {
            App.SetKey("BookmarkData", currentMovie.title.id, "Name=" + currentMovie.title.name + "|||PosterUrl=" + currentMovie.title.hdPosterUrl + "|||Id=" + currentMovie.title.id + "|||TypeId=" + ((int)currentMovie.title.movieType) + "|||ShortEpView=" + currentMovie.title.shortEpView + "|||=EndAll");
            setKey = false;
        }

        private void StarBttClicked(object sender, EventArgs e)
        {
            bool keyExists = App.KeyExists("BookmarkData", currentMovie.title.id);
            if (keyExists) {
                App.RemoveKey("BookmarkData", currentMovie.title.id);
            }
            else {
                if (currentMovie.title.name == null) {
                    App.SetKey("BookmarkData", currentMovie.title.id, "Name=" + currentMovie.title.name + "|||Id=" + currentMovie.title.id + "|||");

                    setKey = true;
                }
                else {
                    SetKey();
                }
            }
            ChangeStar(!keyExists);
        }

        private void SubtitleBttClicked(object sender, EventArgs e)
        {
            Settings.SubtitlesEnabled = !Settings.SubtitlesEnabled;
            ChangeSubtitle(SubtitlesEnabled);
        }

        private void ShareBttClicked(object sender, EventArgs e)
        {
            if (currentMovie.title.id != "" && currentMovie.title.name != "") {
                Share();
            }
        }

        async void Share()
        {
            List<string> actions = new List<string>() { "Everything", "CloudStream Link", "IMDb Link", "Title", "Title and Description" };
            if (CurrentMalLink != "") {
                actions.Insert(3, "MAL Link");
            }
            if (trailerUrl != "") {
                actions.Insert(actions.Count - 2, "Trailer Link");
            }
            string a = await DisplayActionSheet("Copy", "Cancel", null, actions.ToArray());
            string copyTxt = "";
            if (a == "CloudStream Link") {
                string _s = CloudStreamCore.ShareMovieCode(currentMovie.title.id + "Name=" + currentMovie.title.name + "=EndAll");
                if (_s != "") {
                    copyTxt = _s;
                }
            }
            else if (a == "IMDb Link") {
                copyTxt = "https://www.imdb.com/title/" + currentMovie.title.id;
            }
            else if (a == "Title") {
                copyTxt = currentMovie.title.name;
            }
            else if (a == "MAL Link") {
                copyTxt = CurrentMalLink;
            }
            else if (a == "Title and Description") {
                copyTxt = currentMovie.title.name + "\n" + currentMovie.title.description;
            }
            else if (a == "Trailer Link") {
                copyTxt = trailerUrl;
            }
            else if (a == "Everything") {
                copyTxt = currentMovie.title.name + " | " + RatingLabel.Text + "\n" + currentMovie.title.description;

                string _s = CloudStreamCore.ShareMovieCode(currentMovie.title.id + "Name=" + currentMovie.title.name + "=EndAll");
                if (_s != "") {
                    copyTxt = copyTxt + "\nCloudStream: " + _s;
                }
                copyTxt = copyTxt + "\nIMDb: " + "https://www.imdb.com/title/" + currentMovie.title.id;
                if (CurrentMalLink != "") {
                    copyTxt = copyTxt + "\nMAL: " + CurrentMalLink;
                }
                if (trailerUrl != "") {
                    copyTxt = copyTxt + "\nTrailer: " + trailerUrl;
                }
            }
            if (a != "Cancel" && copyTxt != "") {
                await Clipboard.SetTextAsync(copyTxt);
                App.ShowToast("Copied " + a + " to Clipboard");
            }

        }

        void ChangeStar(bool? overrideBool = null, string key = null)
        {
            bool keyExists = false;
            if (key == null) {
                key = currentMovie.title.id;
            }
            if (overrideBool == null) {
                keyExists = App.KeyExists("BookmarkData", key);
                print("KEYEXISTS:" + keyExists + "|" + currentMovie.title.id);
            }
            else {
                keyExists = (bool)overrideBool;
            }
            StarBtt.Source = GetImageSource((keyExists ? "bookmarkedBtt.png" : "notBookmarkedBtt.png"));
            Device.BeginInvokeOnMainThread(() => {
                StarBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(keyExists ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        void ChangeSubtitle(bool? overrideBool = null)
        {
            bool res = false;
            if (overrideBool == null) {

                res = SubtitlesEnabled;
            }
            else {
                res = (bool)overrideBool;
                //SubtitlesEnabled = res;
            }

            Device.BeginInvokeOnMainThread(() => {
                SubtitleBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(res ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
            });
        }

        public void SetChromeCast(bool enabled)
        {
            ChromeCastBtt.IsVisible = enabled;
            ChromeCastBtt.IsEnabled = enabled;
            ImgChromeCastBtt.IsVisible = enabled;
            ImgChromeCastBtt.IsEnabled = enabled;
            if (enabled) {
                ImgChromeCastBtt.Source = GetImageSource(MainChrome.CurrentImageSource);
            }
            NameLabel.Margin = new Thickness((enabled ? 50 : 15), 10, 10, 10);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);
        }

        private void ChromeCastBtt_Clicked(object sender, EventArgs e)
        {
            WaitChangeChromeCast();
        }

        private void OpenChromecastView(object sender, EventArgs e)
        {
            if (sender != null) {
                ChromeCastPage.isActive = false;
            }
            if (!ChromeCastPage.isActive) {
                Page p = new ChromeCastPage() { episodeResult = chromeResult, chromeMovieResult = chromeMovieResult };
                Navigation.PushModalAsync(p, false);
            }
        }

        public static EpisodeResult chromeResult;
        public static Movie chromeMovieResult;
        async void WaitChangeChromeCast()
        {
            if (MainChrome.IsCastingVideo) {
                Device.BeginInvokeOnMainThread(() => {
                    OpenChromecastView(1, EventArgs.Empty);
                });
            }
            else {
                List<string> names = MainChrome.GetChromeDevicesNames();
                string a = await DisplayActionSheet("Cast to", "Cancel", MainChrome.IsConnectedToChromeDevice ? "Disconnect" : null, names.ToArray());
                if (a != "Cancel") {
                    MainChrome.ConnectToChromeDevice(a);
                }
            }
        }

        public static ImageSource GetGradient()
        {
            return GetImageSource("gradient" + Settings.BlackColor + ".png");//BlackBg ? "gradient.png" : "gradientGray.png");
        }

        public MovieResult()
        {
            InitializeComponent();

            mainPoster = Search.mainPoster;

            Gradient.Source = GetGradient();
            IMDbBtt.Source = GetImageSource("IMDbWhite.png");//"imdbIcon.png");
            MALBtt.Source = GetImageSource("MALWhite.png");//"MALIcon.png");
            ShareBtt.Source = GetImageSource("baseline_share_white_48dp.png");//GetImageSource("round_reply_white_48dp_inverted.png");
            StarBtt.Source = GetImageSource("notBookmarkedBtt.png");
            SubtitleBtt.Source = GetImageSource("outline_subtitles_white_48dp.png"); //GetImageSource("round_subtitles_white_48dp.png");
                                                                                     //  IMDbBlue.Source = GetImageSource("IMDbBlue.png"); //GetImageSource("round_subtitles_white_48dp.png");


            SeasonPicker = new LabelList(SeasonBtt, new List<string>());
            DubPicker = new LabelList(DubBtt, new List<string>());
            // FromToPicker = new LabelList(FromToBtt, new List<string>());

            // -------------- CHROMECASTING THINGS --------------

            if (Device.RuntimePlatform == Device.UWP) {
                ImgChromeCastBtt.TranslationX = 0;
                ImgChromeCastBtt.TranslationY = 0;
                OffBar.IsVisible = false;
                OffBar.IsEnabled = false;
            }
            else {
                OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
            }

            MainChrome.OnChromeImageChanged += (o, e) => {
                ImgChromeCastBtt.Source = GetImageSource(e);
                ImgChromeCastBtt.Transformations.Clear();
                if (MainChrome.IsConnectedToChromeDevice) {
                    // ImgChromeCastBtt.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation("#303F9F")) };
                }
            };

            MainChrome.OnChromeDevicesFound += (o, e) => {
                SetChromeCast(MainChrome.IsChromeDevicesOnNetwork);
            };

            MainChrome.OnVideoCastingChanged += (o, e) => {
                if (e) {
                    OpenChromecastView(null, null);
                }
            };

            if (!MainChrome.IsConnectedToChromeDevice) {
                MainChrome.GetAllChromeDevices();
            }

            RecStack.SizeChanged += (o, e) => {
                SetRecs();
            };


            //ViewToggle.Source = GetImageSource("viewOnState.png");
            ChangeViewToggle();
            ChangeSubtitle();

            //NameLabel.Text = activeMovie.title.name;
            NameLabel.Text = mainPoster.name;
            RatingLabel.Text = mainPoster.year;

            titleLoaded += MovieResult_titleLoaded;
            trailerLoaded += MovieResult_trailerLoaded;
            episodeLoaded += MovieResult_epsiodesLoaded;


            // TrailerBtt.Clicked += TrailerBtt_Clicked;
            Gradient.Clicked += TrailerBtt_Clicked;
            linkAdded += MovieResult_linkAdded;

            fishingDone += MovieFishingDone;

            moeDone += MovieResult_moeDone;

            BackgroundColor = Settings.BlackRBGColor;

            MALB.IsVisible = false;
            MALB.IsEnabled = false;
            MALA.IsVisible = false;
            MALA.IsEnabled = false;
            Grid.SetColumn(MALB, 0);
            Grid.SetColumn(MALA, 0);

            DubBtt.IsVisible = false;
            SeasonBtt.IsVisible = false;

            epView = new MovieResultMainEpisodeView();
            SetHeight();

            BindingContext = epView;
            episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;
            MScroll.HorizontalScrollBarVisibility = Settings.ScrollBarVisibility; // REPLACE REC

            ReloadAllBtt.Clicked += (o, e) => {
                App.RemoveKey("CacheImdb", currentMovie.title.id);
                App.RemoveKey("CacheMAL", currentMovie.title.id);
                Navigation.PopModalAsync(false);
                Search.mainPoster = new Poster();
                PushPageFromUrlAndName(currentMovie.title.id, currentMovie.title.name);
            };
            ReloadAllBtt.Source = GetImageSource("round_refresh_white_48dp.png");

            GetImdbTitle(mainPoster);
            currentMovie.title.id = mainPoster.url.Replace("https://imdb.com/title/", "");
            ChangeStar();

            ChangedRecState(0, true);


            Commands.SetTap(NotificationBtt, new Command(() => {
                ToggleNotify();
            }));



        }


        void ToggleNotify()
        {
            bool hasNot = App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            App.SetKey("Notifications", currentMovie.title.id, !hasNot);
            UpdateNotification(!hasNot);

            if (!hasNot) {
                List<int> keys = new List<int>();

                for (int i = 0; i < setNotificationsTimes.Count; i++) {

                    int _id = 1337 + setNotificationsTimes[i].number* 100000000 + int.Parse(currentMovie.title.id.Replace("tt", ""));// int.Parse(setNotificationsTimes[i].number + currentMovie.title.id.Replace("tt", ""));
                    keys.Add(_id);
                    print("BIGICON:::" + currentMovie.title.hdPosterUrl + "|" + currentMovie.title.posterUrl);
                    App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, currentMovie.title.id, currentMovie.title.name,bigIconUrl:currentMovie.title.hdPosterUrl,time:DateTime.UtcNow.AddSeconds(1));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);
                }
                App.SetKey("NotificationsIds", currentMovie.title.id, keys);
            }
            else {
                var keys = App.GetKey<List<int>>("NotificationsIds", currentMovie.title.id, new List<int>());
                for (int i = 0; i < keys.Count; i++) {
                    App.CancelNotifaction(keys[i]);
                }
            }
        }

        void UpdateNotification(bool? overrideNot = null)
        {
            bool hasNot = overrideNot ?? App.GetKey<bool>("Notifications", currentMovie.title.id, false);
            NotificationImg.Source = App.GetImageSource(hasNot ? "baseline_notifications_active_white_48dp.png" : "baseline_notifications_none_white_48dp.png");
            NotificationImg.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(hasNot ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
        }

        List<MoeEpisode> setNotificationsTimes = new List<MoeEpisode>();


        private void MovieResult_moeDone(object sender, List<MoeEpisode> e)
        {
            if (e == null) return;
            void FadeIn()
            {
                NotificationTime.Opacity = 0;
                NotificationTime.FadeTo(1, FATE_TIME_MS);
            }

            if (e.Count <= 0) {
                Device.BeginInvokeOnMainThread(() => {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                });
                return;
            };

            setNotificationsTimes = e;


            if (!SameAsActiveMovie()) return;
            Device.BeginInvokeOnMainThread(() => {
                try {
                    NotificationTime.Text = "Completed";
                    FadeIn();
                    for (int i = e.Count - 1; i > 0; i++) {
                        var diff = e[i].DiffTime;
                        print("DIFFTIME:::::" + e[i].DiffTime);
                        if (diff.TotalSeconds > 0) {
                            NotificationTime.Text = "Next Epiode: " + (diff.Days == 0 ? "" : (diff.Days + "d ")) + (diff.Hours == 0 ? "" : (diff.Hours + "h ")) + diff.Minutes + "m";
                            UpdateNotification();
                            NotificationBtt.IsEnabled = true;
                            return;
                        }
                    }
                }
                catch (Exception _ex) {
                    print("EXKKK::" + _ex);
                }
            });
        }


        public void SetColor(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            if (id != "") {
                List<string> hexColors = new List<string>() { "#ffffff", LIGHT_BLUE_COLOR, "#e5e598" };
                List<string> darkHexColors = new List<string>() { "#909090", DARK_BLUE_COLOR, "#d3c450" };
                int color = 0;
                if (App.KeyExists("ViewHistory", id)) {
                    color = 1;
                }
                if (App.KeyExists("Download", id)) {
                    color = 2;
                }

                episodeResult.MainTextColor = hexColors[color];
                episodeResult.MainDarkTextColor = darkHexColors[color];
            }
        }


        void SetEpisodeFromTo(int segment, int max = -1)
        {
            epView.MyEpisodeResultCollection.Clear();

            int start = MovieResultMainEpisodeView.MAX_EPS_PER * segment;
            if (max == -1) {
                max = epView.AllEpisodes.Length;
            }
            else {
                max = Math.Min(max, epView.AllEpisodes.Length);
            }

            int end = Math.Min(MovieResultMainEpisodeView.MAX_EPS_PER * (segment + 1), max);
            for (int i = start; i < end; i++) {
                epView.MyEpisodeResultCollection.Add(epView.AllEpisodes[i]);
            }
            SetHeight();
        }

        int maxEpisodes = 1;
        int episodeCounter = 0;
        public void AddEpisode(EpisodeResult episodeResult, int index)
        {
            ChangeEpisode(ref episodeResult);
            epView.AllEpisodes[index] = episodeResult;
        }

        void ChangeEpisode(ref EpisodeResult episodeResult)
        {
            episodeResult.OgTitle = episodeResult.Title;
            SetColor(episodeResult);
            if (episodeResult.Rating != "") {
                episodeResult.Title += " | ★ " + episodeResult.Rating;
            }
            if (episodeResult.Rating == "") {
                episodeResult.Rating = currentMovie.title.rating;
            }

            if (episodeResult.PosterUrl == "") {
                if (activeMovie.title.posterUrl != "") {
                    string posterUrl = "";
                    try {
                        if (activeMovie.title.trailers.Count > 0) {
                            if (activeMovie.title.trailers[0].PosterUrl != null) {
                                posterUrl = activeMovie.title.trailers[0].PosterUrl;
                            }
                        }
                    }
                    catch (Exception) {

                    }
                    episodeResult.PosterUrl = posterUrl;
                }
            }
            if (episodeResult.PosterUrl == "") {
                episodeResult.PosterUrl = CloudStreamCore.VIDEO_IMDB_IMAGE_NOT_FOUND;
            }
            else {
                episodeResult.PosterUrl = CloudStreamCore.ConvertIMDbImagesToHD(episodeResult.PosterUrl, 224, 126); //episodeResult.PosterUrl.Replace(",126,224_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY126", "UY" + pheight).Replace("UX224", "UX" + pwidth);
            }
        }



        public void ClearEpisodes()
        {
            episodeView.ItemsSource = null;
            epView.MyEpisodeResultCollection.Clear();
            episodeView.ItemsSource = epView.MyEpisodeResultCollection;
            SetHeight();
        }


        void SetHeight(bool? setNull = null)
        {
            episodeView.RowHeight = Settings.EpDecEnabled ? 170 : 100;
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = ((setNull ?? showState != 0) ? 0 : (epView.MyEpisodeResultCollection.Count * (episodeView.RowHeight) + 40)));
        }

        private void MovieFishingDone(object sender, Movie e)
        {
            if (!SameAsActiveMovie()) return;
            currentMovie = e;
        }

        bool SameAsActiveMovie()
        {
            return currentMovie.title.id == activeMovie.title.id;
        }

        private void MovieResult_linkAdded(object sender, Link e)
        {
            if (!SameAsActiveMovie()) return;
            if (currentMovie.episodes[0].name + currentMovie.episodes[0].description == activeMovie.episodes[0].name + activeMovie.episodes[0].description && epView.MyEpisodeResultCollection.Count > 0) {
            }
            else {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => {
                currentMovie = activeMovie;
                if (CurrentEpisodes != null) {
                    for (int i = 0; i < CurrentEpisodes.Count; i++) {
                        if (CurrentEpisodes[i].links != null) {
                            if (CurrentEpisodes[i].links.Count > 0) {
                                List<Link> links = CurrentEpisodes[i].links;
                                try {
                                    links = links.OrderBy(l => -l.priority).ToList();
                                }
                                catch (Exception) { }
                                //  if (links.Count != 0) {
                                // print("LINK ADDED" + links.Count + "|" + links[links.Count - 1].name);
                                //  }
                                int realFrom = i % MovieResultMainEpisodeView.MAX_EPS_PER;
                                epView.MyEpisodeResultCollection[realFrom].epVis = true;
                                List<string> mirrors = new List<string>();
                                List<string> mirrorsUrls = new List<string>();
                                int mirrorCounter = 0;
                                // myEpisodeResultCollection[i].Mirros.Clear();
                                for (int f = 0; f < links.Count; f++) {
                                    try {
                                        Link link = links[f];
                                        if (CheckIfURLIsValid(link.url)) {
                                            string name = link.name;
                                            if (name.Contains("[MIRRORCOUNTER]")) {
                                                mirrorCounter++;
                                                name = name.Replace("[MIRRORCOUNTER]", mirrorCounter.ToString());
                                            }
                                            mirrors.Add(name);
                                            mirrorsUrls.Add(link.url);
                                        }
                                    }
                                    catch (Exception) { }
                                }

                                if (mirrors.Count > epView.MyEpisodeResultCollection[realFrom].Mirros.Count) {
                                    //EpisodeResult epRes = epView.MyEpisodeResultCollection[i];
                                    epView.MyEpisodeResultCollection[realFrom].mirrosUrls = mirrorsUrls;
                                    epView.MyEpisodeResultCollection[realFrom].epVis = mirrors.Count > 0;
                                    epView.MyEpisodeResultCollection[realFrom].Mirros = mirrors;// = new EpisodeResult() { mirros = mirrors, Description = epRes.Description, epVis = mirrors.Count > 0, Id = epRes.Id, mirrosUrls = mirrorsUrls, PosterUrl = epRes.PosterUrl, progress = epRes.progress, Rating = epRes.Rating, subtitles = epRes.subtitles, Title = epRes.Title };
                                }
                            }
                        }
                    }
                }
            });
            //print(e + "|" + activeMovie.episodes[1].maxProgress);
        }


        private void TrailerBtt_Clicked(object sender, EventArgs e)
        {
            if (trailerUrl != null) {
                if (trailerUrl != "") {
                    App.PlayVLCWithSingleUrl(trailerUrl, currentMovie.title.name + " - Trailer");
                }
            }
        }



        async void FadeTitles(bool fadeSeason)
        {
            print("FAFSAFAFAFA:::");
            DescriptionLabel.Opacity = 0;
            RatingLabel.Opacity = 0;
            RatingLabelRating.Opacity = 0;
            SeasonBtt.Opacity = 0;
            await RatingLabelRating.FadeTo(1, FATE_TIME_MS);
            await RatingLabel.FadeTo(1, FATE_TIME_MS);
            await DescriptionLabel.FadeTo(1, FATE_TIME_MS);
            if (fadeSeason) {
                await SeasonBtt.FadeTo(1, FATE_TIME_MS);
            }


        }

        private void MovieResult_titleLoaded(object sender, Movie e)
        {
            if (loadedTitle) return;
            if (e.title.name != mainPoster.name) return;
            if (!SameAsActiveMovie()) return;

            loadedTitle = true;
            isMovie = (e.title.movieType == MovieType.Movie || e.title.movieType == MovieType.AnimeMovie);

            currentMovie = e;
            if (setKey) {
                SetKey();
            }
            MainThread.BeginInvokeOnMainThread(() => {
                //App.ShowNotIntent("NEW EPISODE - " + currentMovie.title.name, currentMovie.title.name, 1337, currentMovie.title.id, currentMovie.title.name, bigIconUrl: currentMovie.title.hdPosterUrl, time: DateTime.UtcNow.AddSeconds(1));//ShowNotification("NEW EPISODE - " + currentMovie.title.name, setNotificationsTimes[i].episodeName, _id, i * 10);

                EPISODES.Text = isMovie ? "MOVIE" : "EPISODES";

                try {
                    string souceUrl = e.title.trailers.First().PosterUrl;
                    if (CheckIfURLIsValid(souceUrl)) {
                        TrailerBtt.Source = souceUrl;
                    }
                    else {
                        TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                    }
                }
                catch (Exception) {
                    TrailerBtt.Source = ImageSource.FromResource("CloudStreamForms.Resource.gradient.png", Assembly.GetExecutingAssembly());
                }

                ChangeStar();

                string extra = "";
                bool haveSeasons = e.title.seasons != 0;

                if (haveSeasons) {
                    extra = e.title.seasons + " Season" + (e.title.seasons == 1 ? "" : "s") + " | ";
                }

                string rYear = mainPoster.year;
                if (rYear == null || rYear == "") {
                    rYear = e.title.year;
                }
                RatingLabel.Text = ((rYear + " | " + e.title.runtime).Replace("|  |", "|")).Replace("|", "  "); //+ " | " + extra + "★ " + e.title.rating).Replace("|  |", "|")).Replace("|", "  ");
                RatingLabelRating.Text = "Rated: " + e.title.rating;
                DescriptionLabel.Text = Settings.MovieDecEnabled ? CloudStreamCore.RemoveHtmlChars(e.title.description) : "";
                if (e.title.description == "") {
                    DescriptionLabel.HeightRequest = 0;
                }



                // ---------------------------- SEASONS ----------------------------


                SeasonPicker.IsVisible = haveSeasons;
                FadeTitles(haveSeasons);

                DubPicker.SelectedIndexChanged += DubPicker_SelectedIndexChanged;
                if (haveSeasons) {
                    List<string> season = new List<string>();
                    for (int i = 1; i <= e.title.seasons; i++) {
                        season.Add("Season " + i);
                    }
                    SeasonPicker.ItemsSource = season;

                    int selIndex = App.GetKey<int>("SeasonIndex", activeMovie.title.id, 0);
                    try {
                        SeasonPicker.SelectedIndex = Math.Min(selIndex, SeasonPicker.ItemsSource.Count - 1);
                    }
                    catch (Exception) {
                        SeasonPicker.SelectedIndex = 0; // JUST IN CASE
                    }

                    currentSeason = SeasonPicker.SelectedIndex + 1;


                    print("GetImdbEpisodes>>>>>>>>>>>>>>><<");
                    GetImdbEpisodes(currentSeason);
                }
                else {
                    currentSeason = 0; // MOVIES
                    GetImdbEpisodes();
                }
                SeasonPicker.SelectedIndexChanged += SeasonPicker_SelectedIndexChanged;

                // ---------------------------- RECOMMENDATIONS ----------------------------

                foreach (var item in Recommendations.Children) { // SETUP
                    Grid.SetColumn(item, 0);
                    Grid.SetRow(item, 0);
                }
                Recommendations.Children.Clear();

                for (int i = 0; i < RecomendedPosters.Count; i++) {
                    Poster p = e.title.recomended[i];
                    string posterURL = ConvertIMDbImagesToHD(p.posterUrl, 76, 113, 1.75); //.Replace(",76,113_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY113", "UY" + pheight).Replace("UX76", "UX" + pwidth);
                    if (CheckIfURLIsValid(posterURL)) {
                        Grid stackLayout = new Grid();
                        Button imageButton = new Button() { HeightRequest = RecPosterHeight, WidthRequest = RecPosterWith, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Center };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = posterURL,
                            HeightRequest = RecPosterHeight,
                            WidthRequest = RecPosterWith,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Start,
                            Transformations = {
                            //  new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1, 1.5, 0, "#303F9F")
                        },
                            InputTransparent = true,
                        };

                        // ================================================================ RECOMMENDATIONS CLICKED ================================================================
                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
                        Commands.SetTap(stackLayout, new Command((o) => {
                            int z = (int)o;
                            if (Search.mainPoster.url != RecomendedPosters[z].url) {
                                if (lastMovie == null) {
                                    lastMovie = new List<Movie>();
                                }
                                lastMovie.Add(activeMovie);
                                Search.mainPoster = RecomendedPosters[z];
                                Page _p = new MovieResult();// { mainPoster = mainPoster };
                                Navigation.PushModalAsync(_p);
                            }
                            //do something
                        }));
                        Commands.SetTapParameter(stackLayout, i);
                        recBtts.Add(imageButton);

                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(imageButton);

                        Recommendations.Children.Add(stackLayout);
                    }
                }
                RecomendationLoaded.IsVisible = false;

                SetRecs();

            });



        }

        const double _RecPosterMulit = 1.75;
        const int _RecPosterHeight = 100;
        const int _RecPosterWith = 65;
        int RecPosterHeight { get { return (int)Math.Round(_RecPosterHeight * _RecPosterMulit); } }
        int RecPosterWith { get { return (int)Math.Round(_RecPosterWith * _RecPosterMulit); } }

        void SetRecs()
        {
            Device.BeginInvokeOnMainThread(() => {
                const int total = 12;
                int perCol = (Application.Current.MainPage.Width < Application.Current.MainPage.Height) ? 3 : 6;
                Recommendations.HeightRequest = RecPosterHeight * (total / perCol);

                for (int i = 0; i < Recommendations.Children.Count; i++) { // GRID
                    Grid.SetColumn(Recommendations.Children[i], i % perCol);
                    Grid.SetRow(Recommendations.Children[i], (int)Math.Floor(i / (double)perCol));
                }
            });
        }

        private void DubPicker_SelectedIndexChanged(object sender, int e)
        {
            print("DUBCHANGED::");
            try {
                isDub = "Dub" == DubPicker.ItemsSource[DubPicker.SelectedIndex];
                SetDubExist();
            }
            catch (Exception _ex) {
                print("EXPECTION:" + _ex);
            }
        }

        private void SeasonPicker_SelectedIndexChanged(object sender, int e)
        {
            ClearEpisodes();
            //  epView.MyEpisodeResultCollection.Clear();

            DubPicker.button.FadeTo(0, FATE_TIME_MS);
            currentSeason = SeasonPicker.SelectedIndex + 1;
            App.SetKey("SeasonIndex", activeMovie.title.id, SeasonPicker.SelectedIndex);

            GetImdbEpisodes(currentSeason);
        }

        void SetChangeTo(int maxEp = -1)
        {
            Device.BeginInvokeOnMainThread(() => {
                if (maxEp == -1) {
                    maxEp = maxEpisodes;
                }
                var source = new List<string>();

                int times = (int)Math.Ceiling((decimal)epView.AllEpisodes.Length / (decimal)MovieResultMainEpisodeView.MAX_EPS_PER);

                for (int i = 0; i < times; i++) {
                    int fromTo = maxEp - i * MovieResultMainEpisodeView.MAX_EPS_PER;
                    string f = (i * MovieResultMainEpisodeView.MAX_EPS_PER + 1) + "-" + ((i) * MovieResultMainEpisodeView.MAX_EPS_PER + Math.Min(fromTo, MovieResultMainEpisodeView.MAX_EPS_PER));
                    source.Add(f);
                }

                FromToPicker = new LabelList(FromToBtt, source);//.IsVisible = FromToPicker.ItemsSource.Count > 1;                
                FromToPicker.SelectedIndex = 0;//.IsVisible = FromToPicker.ItemsSource.Count > 1;           
                FromToPicker.IsVisible = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.button.IsEnabled = FromToPicker.ItemsSource.Count > 1;
                FromToPicker.SelectedIndexChanged += (o, e) => {
                    SetEpisodeFromTo(e, maxEpisodes);
                };
            });

        }

        private void MovieResult_epsiodesLoaded(object sender, List<Episode> e)
        {
            if (e == null) return;
            if (!SameAsActiveMovie()) return;
            print("episodes loaded");

            currentMovie = activeMovie;

            currentMovie.episodes = e;
            MainThread.BeginInvokeOnMainThread(() => {
                CurrentEpisodes = e;
                ClearEpisodes();
                //bool isLocalMovie = false;
                bool isAnime = currentMovie.title.movieType == MovieType.Anime;

                if (currentMovie.title.movieType != MovieType.Movie && currentMovie.title.movieType != MovieType.AnimeMovie) { // SEASON ECT
                    print("MAXEPS:::" + CurrentEpisodes.Count);
                    epView.AllEpisodes = new EpisodeResult[CurrentEpisodes.Count];
                    maxEpisodes = epView.AllEpisodes.Length;
                    for (int i = 0; i < CurrentEpisodes.Count; i++) {
                        AddEpisode(new EpisodeResult() { Title = (i + 1) + ". " + CurrentEpisodes[i].name, Id = i, Description = CurrentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = CurrentEpisodes[i].posterUrl, Rating = CurrentEpisodes[i].rating, Progress = 0, epVis = false, subtitles = new List<string>() { "None" }, Mirros = new List<string>() }, i);
                    }
                    if (!isAnime) {
                        SetEpisodeFromTo(0);
                        SetChangeTo();
                    }
                }
                else { // MOVE
                    maxEpisodes = 1;
                    epView.AllEpisodes = new EpisodeResult[1];
                    AddEpisode(new EpisodeResult() { Title = currentMovie.title.name, Description = currentMovie.title.description, Id = 0, PosterUrl = "", Progress = 0, Rating = "", epVis = false, subtitles = new List<string>() { "None" }, Mirros = new List<string>() }, 0);
                    SetEpisodeFromTo(0);
                }

                DubPicker.ItemsSource.Clear();

                // SET DUB SUB
                if (isAnime) {
                    bool dubExists = false;
                    bool subExists = false;
                    try {
                        for (int q = 0; q < currentMovie.title.MALData.seasonData[currentSeason].seasons.Count; q++) {
                            MALSeason ms = currentMovie.title.MALData.seasonData[currentSeason].seasons[q];
                            try {
                                if (ms.dubbedAnimeData.dubExists) {
                                    dubExists = true;
                                }
                            }
                            catch (Exception) { }
                            try {
                                if (ms.gogoData.dubExists) {
                                    dubExists = true;
                                }
                                if (ms.gogoData.subExists) {
                                    subExists = true;
                                }
                            }
                            catch (Exception) { }
                            try {
                                if (ms.kickassAnimeData.dubExists) {
                                    dubExists = true;
                                }
                                if (ms.kickassAnimeData.subExists) {
                                    subExists = true;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }

                    isDub = dubExists;

                    if (Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }
                    if (subExists) {
                        DubPicker.ItemsSource.Add("Sub");
                    }
                    if (!Settings.DefaultDub) {
                        if (dubExists) {
                            DubPicker.ItemsSource.Add("Dub");
                        }
                    }

                    if (DubPicker.ItemsSource.Count > 0) {
                        DubPicker.SelectedIndex = 0;
                    }
                    SetDubExist();
                }



                bool enabled = CurrentMalLink != "";

                MALB.IsVisible = enabled;
                MALB.IsEnabled = enabled;
                MALA.IsVisible = enabled;
                MALA.IsEnabled = enabled;

                Grid.SetColumn(MALB, enabled ? 5 : 0);
                Grid.SetColumn(MALA, enabled ? 5 : 0);

                DubPicker.button.Opacity = 0;
                DubPicker.IsVisible = DubPicker.ItemsSource.Count > 0;
                DubPicker.button.FadeTo(DubPicker.IsVisible ? 1 : 0, FATE_TIME_MS);
            });
        }


        void SetDubExist()
        {
            print("SETDUB:::");
            if (!SameAsActiveMovie()) return;
            print("SETDUB:::SET");

            TempThred tempThred = new TempThred();
            tempThred.typeId = 6; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
            tempThred.Thread = new System.Threading.Thread(() => {
                try {
                    int max = GetMaxEpisodesInAnimeSeason(currentMovie, currentSeason, isDub, tempThred);
                    if (max > 0) {
                        print("CLEAR AND ADD");
                        MainThread.BeginInvokeOnMainThread(() => {
                            /*
                            try {
                                DubBtt.Text = DubPicker.ItemsSource[DubPicker.SelectedIndex];
                            }
                            catch (Exception) {

                            }*/

                            //  DubBtt.IsVisible = DubPicker.IsVisible;

                            maxEpisodes = max;
                            print("MAXUSsssss" + maxEpisodes + "|" + max + "|" + (int)Math.Ceiling((double)max / (double)MovieResultMainEpisodeView.MAX_EPS_PER));

                            SetEpisodeFromTo(0, max);
                            SetChangeTo(max);
                        });
                    }
                }
                finally {
                    JoinThred(tempThred);
                }
            });
            tempThred.Thread.Name = "Set SUB/DUB";
            tempThred.Thread.Start();
        }


        private void MovieResult_trailerLoaded(object sender, List<Trailer> e)
        {
            if (!SameAsActiveMovie()) return;
            if (e == null) return;
            currentMovie.title.trailers = e;
            epView.CurrentTrailers.Clear();
            for (int i = 0; i < e.Count; i++) {
                epView.CurrentTrailers.Add(e[i]);
            }

            if (e.Count > 4) return; // MAX 4 TRAILERS

            if (trailerUrl == "") {
                trailerUrl = e[0].Url;
            }

            Device.BeginInvokeOnMainThread(() => {
                TRAILERSTAB.IsVisible = true;
                TRAILERSTAB.IsEnabled = true;
                trailerView.Children.Clear();
                trailerView.HeightRequest = e.Count * 240 + 200;
                if (PlayBttGradient.Source == null) {
                    PlayBttGradient.Source = GetImageSource("nexflixPlayBtt.png");
                    PlayBttGradient.Opacity = 0;
                    PlayBttGradient.FadeTo(1, FATE_TIME_MS);
                }

                for (int i = 0; i < e.Count; i++) {
                    string p = e[i].PosterUrl;
                    if (CheckIfURLIsValid(p)) {
                        Grid stackLayout = new Grid();
                        Label textLb = new Label() { Text = e[i].Name, TextColor = Color.FromHex("#e7e7e7"), FontAttributes = FontAttributes.Bold, FontSize = 15, TranslationX = 10 };
                        Image playBtt = new Image() { Source = GetImageSource("nexflixPlayBtt.png"), VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center, Scale = 0.5, InputTransparent = true };
                        var ff = new FFImageLoading.Forms.CachedImage {
                            Source = p,
                            BackgroundColor = Color.Transparent,
                            VerticalOptions = LayoutOptions.Fill,
                            Aspect = Aspect.AspectFill,
                            HorizontalOptions = LayoutOptions.Fill,
                            Transformations = {
                            new FFImageLoading.Transformations.RoundedTransformation(1, 1.7, 1, 0, "#303F9F")

                        },
                            InputTransparent = true,
                        };

                        int _sel = int.Parse(i.ToString());
                        stackLayout.Children.Add(ff);
                        stackLayout.Children.Add(playBtt);
                        trailerView.Children.Add(stackLayout);
                        trailerView.Children.Add(textLb);

                        stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, new Color(1, 1, 1, 0.3));
                        Commands.SetTap(stackLayout, new Command((o) => {
                            int z = (int)o;
                            var _t = epView.CurrentTrailers[z];
                            PlayVLCWithSingleUrl(_t.Url, _t.Name);
                        }));
                        Commands.SetTapParameter(stackLayout, _sel);
                        Grid.SetRow(stackLayout, (i + 1) * 2 - 2);
                        Grid.SetRow(textLb, (i + 1) * 2 - 1);

                    }
                }
            });

            //  trailerView.Children.Add(new )
            /*
            MainThread.BeginInvokeOnMainThread(() => {
                Trailer trailer = activeMovie.title.trailers.First();
                trailerUrl = trailer.url;
                print(trailer.posterUrl);
                TrailerBtt.Source = trailer.posterUrl;//ImageSource.FromUri(new System.Uri(trailer.posterUrl));

            });*/

        }
        private void IMDb_Clicked(object sender, EventArgs e)
        {
            // if (!SameAsActiveMovie()) return;
            App.OpenBrowser("https://www.imdb.com/title/" + mainPoster.url);
        }
        private void MAL_Clicked(object sender, EventArgs e)
        {
            //   if (!SameAsActiveMovie()) return;
            App.OpenBrowser(CurrentMalLink);
        }

        void PlayEpisodeRes(EpisodeResult episodeResult)
        {
            string hasDownloadedFile = App.GetKey("Download", GetId(episodeResult), "");
            if (hasDownloadedFile != "") {
                Download.PlayFile(hasDownloadedFile, episodeResult.Title);
            }
            else {
                LoadLinksForEpisode(episodeResult);
            }
        }

        private void ImageButton_Clicked(object sender, EventArgs e) // LOAD
        {
            if (!SameAsActiveMovie()) return;
            EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            PlayEpisodeRes(episodeResult);

            episodeView.SelectedItem = null;
        }

        bool loadingLinks = false;

        async Task<EpisodeResult> LoadLinksForEpisode(EpisodeResult episodeResult, bool autoPlay = true, bool overrideLoaded = false)
        {
            if (loadingLinks) return episodeResult;

            if (episodeResult.LoadedLinks && !overrideLoaded) {
                print("OPEN : " + episodeResult.Title);
                if (episodeResult.mirrosUrls.Count > 0) {

                    if (autoPlay) { PlayEpisode(episodeResult); }
                }
                else {
                    episodeView.SelectedItem = null;
                    App.ShowToast(errorEpisodeToast);
                }
            }
            else {
                GetEpisodeLink(isMovie ? -1 : (episodeResult.Id + 1), currentSeason, isDub: isDub);

                await Device.InvokeOnMainThreadAsync(async () => {
                    NormalStack.IsEnabled = false;
                    loadingLinks = true;

                    UserDialogs.Instance.ShowLoading("Loading links...", MaskType.Gradient);
                    await Task.Delay(LoadingMiliSec);
                    UserDialogs.Instance.HideLoading();

                    loadingLinks = false;

                    if (SameAsActiveMovie()) {
                        currentMovie = activeMovie;
                    }
                    NormalStack.IsEnabled = true;
                    NormalStack.Opacity = 1f;
                    if (episodeResult == null) {
                        print("NULLEP"); episodeView.SelectedItem = null;

                        App.ShowToast(errorEpisodeToast);

                    }
                    else {
                        if (episodeResult.mirrosUrls == null) {
                            print("NULLE2");
                            episodeView.SelectedItem = null;
                            App.ShowToast(errorEpisodeToast);
                        }
                        else {
                            print("NULLEP3");

                            print("LINKCOUNT: " + episodeResult.mirrosUrls.Count);
                            if (episodeResult.mirrosUrls.Count > 0) {

                                if (autoPlay) { PlayEpisode(episodeResult); }
                                episodeResult.LoadedLinks = true;
                            }
                            else {
                                print("NULL3P3");
                                episodeView.SelectedItem = null;

                                App.ShowToast(errorEpisodeToast);
                            }
                        }
                    }
                });
            }
            return episodeResult;
        }


        // ============================== PLAY VIDEO ==============================
        void PlayEpisode(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            if (id != "") {
                if (ViewHistory) {
                    App.SetKey("ViewHistory", id, true);
                    SetColor(episodeResult);
                    // FORCE UPDATE
                    ForceUpdate();
                }
            }

            string _sub = "";
            if (currentMovie.subtitles != null) {
                if (currentMovie.subtitles.Count > 0) {
                    _sub = currentMovie.subtitles[0].data;
                }
            }
            App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls, episodeResult.Mirros, _sub);
        }

        // ============================== FORCE UPDATE ==============================
        void ForceUpdate()
        {
            var _e = epView.MyEpisodeResultCollection.ToList();
            Device.BeginInvokeOnMainThread(() => {
                epView.MyEpisodeResultCollection.Clear();
                for (int i = 0; i < _e.Count; i++) {
                    // print("Main::" + _e[i].MainTextColor);
                    EpisodeResult e = _e[i];
                    epView.MyEpisodeResultCollection.Add(new EpisodeResult() { Title = e.Title, Description = e.Description, MainTextColor = e.MainTextColor, MainDarkTextColor = e.MainDarkTextColor, Rating = e.Rating, epVis = e.epVis, Id = e.Id, LoadedLinks = e.LoadedLinks, Mirros = e.Mirros, mirrosUrls = e.mirrosUrls, OgTitle = e.OgTitle, PosterUrl = e.PosterUrl, Progress = e.Progress, subtitles = e.subtitles, subtitlesUrls = e.subtitlesUrls }); // , loadResult = e.loadResult
                }
            });
        }

        // public string ChromeColor { set; get; }//{ get { return (MainChrome.IsPendingConnection || MainChrome.IsCastingVideo) ? "#303F9F" : "#ffffff"; } set { } }

        async void EpisodeSettings(EpisodeResult episodeResult)
        {
            if (!episodeResult.LoadedLinks) {
                try {
                    await LoadLinksForEpisode(episodeResult, false);
                }
                catch (Exception) { }
            }
            if (loadingLinks) {
                await Task.Delay(LoadingMiliSec + 40);
            }

            if (!episodeResult.LoadedLinks) {
                App.ShowToast(errorEpisodeToast); episodeView.SelectedItem = null;
                return;
            }


            // ============================== GET ACTION ==============================
            string action = "";

            bool hasDownloadedFile = App.KeyExists("Download", GetId(episodeResult));
            string downloadKeyData = "";

            List<string> actions = new List<string>() { "Play", "Play in Browser", "Download", "Download Subtitles", "Copy Link", "Reload" };

            if (hasDownloadedFile) {
                downloadKeyData = App.GetKey("Download", GetId(episodeResult), "");
                actions.Add("Play Downloaded File"); actions.Add("Delete Downloaded File");
            }
            if (MainChrome.IsConnectedToChromeDevice) {
                actions.Insert(0, "Chromecast");
            }

            action = await DisplayActionSheet(episodeResult.Title, "Cancel", null, actions.ToArray());


            if (action == "Play in Browser") {
                string copy = await DisplayActionSheet("Open Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == copy) {
                        App.OpenBrowser(episodeResult.mirrosUrls[i]);
                    }
                }
            }

            if (action == "Chromecast") { // ============================== CHROMECAST ==============================
                chromeResult = episodeResult;
                chromeMovieResult = currentMovie;
                bool succ = false;
                int count = -1;
                episodeView.SelectedItem = null;

                while (!succ) {
                    count++;

                    if (count >= episodeResult.Mirros.Count) {
                        succ = true;
                    }
                    else {
                        succ = await MainChrome.CastVideo(episodeResult.mirrosUrls[count], episodeResult.Mirros[count]);
                    }
                }
                ChromeCastPage.currentSelected = count;

                print("CASTOS");
                /*
                string download = await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == download) {
                        MainChrome.CastVideo(episodeResult.mirrosUrls[i], episodeResult.Mirros[i]);
                    }
                }*/
            }

            if (action == "Play") { // ============================== PLAY ==============================
                PlayEpisode(episodeResult);
            }
            else if (action == "Copy Link") { // ============================== COPY LINK ==============================
                string copy = await DisplayActionSheet("Copy Link", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == copy) {
                        await Clipboard.SetTextAsync(episodeResult.mirrosUrls[i]);
                        App.ShowToast("Copied Link to Clipboard");
                    }
                }
            }
            else if (action == "Download") {  // ============================== DOWNLOAD FILE ==============================
                string download = await DisplayActionSheet("Download", "Cancel", null, episodeResult.Mirros.ToArray());
                for (int i = 0; i < episodeResult.Mirros.Count; i++) {
                    if (episodeResult.Mirros[i] == download) {
                        string s = episodeResult.mirrosUrls[i];

                        TempThred tempThred = new TempThred();
                        tempThred.typeId = 4; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                        tempThred.Thread = new System.Threading.Thread(() => {
                            try {

                                UserDialogs.Instance.ShowLoading("Checking link...", MaskType.Gradient);
                                double fileSize = CloudStreamCore.GetFileSize(s);
                                UserDialogs.Instance.HideLoading();
                                if (fileSize > 1) {
                                    string dpath = App.DownloadUrl(s, episodeResult.Title + ".mp4", true, "/" + GetPathFromType(), "Download complete!", true, episodeResult.Title);
                                    //  string ppath = App.DownloadUrl(episodeResult.PosterUrl, "epP" + episodeResult.Title + ".jpg", false, "/Posters");
                                    // string mppath = App.DownloadUrl(currentMovie.title.hdPosterUrl, "hdP" + episodeResult.Title + ".jpg", false, "/TitlePosters");
                                    string mppath = currentMovie.title.hdPosterUrl;
                                    string ppath = episodeResult.PosterUrl;
                                    string key = "_dpath=" + dpath + "|||_ppath=" + ppath + "|||_mppath=" + mppath + "|||_descript=" + episodeResult.Description + "|||_maindescript=" + currentMovie.title.description + "|||_epCounter=" + episodeResult.Id + "|||_epId=" + GetId(episodeResult) + "|||_movieId=" + currentMovie.title.id + "|||_title=" + episodeResult.Title + "|||_movieTitle=" + currentMovie.title.name + "|||=EndAll";
                                    print("DKEY: " + key);
                                    App.SetKey("Download", GetId(episodeResult), key);
                                    App.ShowToast("Download Started - " + fileSize + "MB");
                                    App.SetKey("DownloadSize", GetId(episodeResult), fileSize);
                                    SetColor(episodeResult);
                                    ForceUpdate();
                                }
                                else {
                                    EpisodeSettings(episodeResult);
                                    App.ShowToast("Download Failed");
                                    ForceUpdate();
                                }
                            }
                            finally {
                                UserDialogs.Instance.HideLoading();
                                JoinThred(tempThred);
                            }
                        });
                        tempThred.Thread.Name = "DownloadThread";
                        tempThred.Thread.Start();
                    }
                }
            }
            else if (action == "Reload") { // ============================== RELOAD ==============================
                try {
                    await LoadLinksForEpisode(episodeResult, false, true);
                }
                catch (Exception) { }

                await Task.Delay(LoadingMiliSec + 40);

                if (!episodeResult.LoadedLinks) {
                    episodeView.SelectedItem = null;
                    App.ShowToast(errorEpisodeToast);
                    return;
                }
                EpisodeSettings(episodeResult);
            }
            else if (action == "Play Downloaded File") { // ============================== PLAY FILE ==============================
                Download.PlayFile(downloadKeyData, episodeResult.Title);
            }
            else if (action == "Delete Downloaded File") {  // ============================== DELETE FILE ==============================
                Download.DeleteFileFromFolder(downloadKeyData, "Download", GetId(episodeResult));
                SetColor(episodeResult);
                ForceUpdate();
            }
            else if (action == "Download Subtitles") {  // ============================== DOWNLOAD SUBTITLE ==============================
                TempThred tempThred = new TempThred();
                tempThred.typeId = 4; // MAKE SURE THIS IS BEFORE YOU CREATE THE THRED
                tempThred.Thread = new System.Threading.Thread(() => {
                    try {
                        string id = GetId(episodeResult);
                        if (id.Replace(" ", "") == "") { App.ShowToast("Id not found"); return; }

                        string s = DownloadSubtitle(id);
                        if (s == "") {
                            App.ShowToast("No Subtitles Found");
                            return;
                        }
                        else {
                            App.DownloadFile(s, episodeResult.Title + ".srt", true, "/Subtitles");
                            App.ShowToast("Subtitles Downloaded");
                        }
                    }
                    finally {
                        JoinThred(tempThred);
                    }
                });
                tempThred.Thread.Name = "Subtitle Download";
                tempThred.Thread.Start();
            }
            episodeView.SelectedItem = null;

        }


        // ============================== ID OF EPISODE ==============================
        public string GetId(EpisodeResult episodeResult)
        {
            try {
                return (currentMovie.title.movieType == MovieType.TVSeries || currentMovie.title.movieType == MovieType.Anime) ? currentMovie.episodes[episodeResult.Id].id : currentMovie.title.id;

            }
            catch (Exception) {
                return episodeResult.Id + "Extra=" + ToDown(episodeResult.Title) + "=EndAll";
            }
        }


        // ============================== DOWNLOAD PATH ==============================
        string GetPathFromType()
        {
            string path = "Movies";
            if (currentMovie.title.movieType == MovieType.Anime) {
                path = "Anime";
            }
            else if (currentMovie.title.movieType == MovieType.TVSeries) {
                path = "TVSeries";
            }
            return path;
        }


        // ============================== TOGGLE HAS SEEN EPISODE ==============================

        bool toggleViewState = false;
        private void ViewToggle_Clicked(object sender, EventArgs e)
        {
            toggleViewState = !toggleViewState;
            ChangeViewToggle();
        }

        void ChangeViewToggle()
        {
            ViewToggle.Source = GetImageSource((toggleViewState ? "outline_visibility_off_white_48dp.png" : "outline_visibility_white_48dp.png"));// GetImageSource((toggleViewState ? "viewOffIcon.png" : "viewOnIcon.png"));
            ViewToggle.Transformations = new List<FFImageLoading.Work.ITransformation>() { (new FFImageLoading.Transformations.TintTransformation(toggleViewState ? DARK_BLUE_COLOR : LIGHT_LIGHT_BLACK_COLOR)) };
        }

        void ToggleEpisode(EpisodeResult episodeResult)
        {
            string id = GetId(episodeResult);
            if (id != "") {
                if (App.KeyExists("ViewHistory", id)) {
                    App.RemoveKey("ViewHistory", id);
                }
                else {
                    App.SetKey("ViewHistory", id, true);
                }
            }
            //episodeResult.MainTextColor = App.KeyExists("ViewHistory", id) ? primaryLightColor : "#ffffff";
            SetColor(episodeResult);
            ForceUpdate();
        }

        // ============================== USED FOR SMALL VIDEO PLAY ==============================
        private void Grid_LayoutChanged(object sender, EventArgs e)
        {
            var s = ((Grid)sender);
            Commands.SetTap(s, new Command((o) => {
                var episodeResult = (EpisodeResult)s.BindingContext;
                PlayEpisodeRes(episodeResult);
            }));
        }
        // ============================== SHOW SETTINGS OF VIDEO ==============================
        private void ViewCell_Tapped(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = ((EpisodeResult)(((ViewCell)sender).BindingContext));

            if (toggleViewState) {
                ToggleEpisode(episodeResult);
                episodeView.SelectedItem = null;
            }
            else {
                EpisodeSettings(episodeResult);
            }
        }



        #region ===================================================== MOVE RECOMENDATION ECT BAR  =====================================================
        /// <summary>
        /// 0 = episodes, 1 = recommendations, 2 = trailers
        /// </summary>
        int showState = 0;
        int prevState = 0;
        void ChangedRecState(int state, bool overrideCheck = false)
        {
            prevState = int.Parse(showState.ToString());
            if (state == showState && !overrideCheck) return;
            showState = state;
            Device.BeginInvokeOnMainThread(() => {
                Grid.SetRow(EpPickers, (state == 0) ? 1 : 0);

                episodeView.Scale = (state == 0) ? 1 : 0;
                //episodeView.IsEnabled = state == 0;

                trailerStack.Scale = (state == 2) ? 1 : 0;
                trailerStack.IsEnabled = state == 2;
                trailerStack.IsVisible = state == 2;
                trailerStack.InputTransparent = state != 2;
                // trailerView.HeightRequest = state == 2 ? Math.Min(epView.CurrentTrailers.Count, 4) * 350 : 0;

                EpPickers.IsEnabled = state == 0;
                EpPickers.Scale = state == 0 ? 1 : 0;

                RecStack.Scale = state == 1 ? 1 : 0;
                RecStack.IsEnabled = state == 1;
                RecStack.InputTransparent = state != 1;

                SetHeight(state != 0);
                //SetTrailerRec(state == 2);

                if (state == 1) {
                    SetRecs();
                }

            });

            System.Timers.Timer timer = new System.Timers.Timer(10);
            ProgressBar GetBar(int _state)
            {
                switch (_state) {
                    case 0:
                        return EPISODESBar;
                    case 1:
                        return RECOMMENDATIONSBar;
                    case 2:
                        return TRAILERSBar;
                    default:
                        return null;
                }
            }
            GetBar(prevState).ScaleXTo(0, 70, Easing.Linear);
            GetBar(state).ScaleXTo(1, 70, Easing.Linear);
            timer.Start();
        }

        private void Episodes_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(0);
        }

        private void Recommendations_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(1);
        }

        private void Trailers_Clicked(object sender, EventArgs e)
        {
            ChangedRecState(2);
        }

        #endregion
    }
}

public class MovieResultMainEpisodeView
{
    public ObservableCollection<Trailer> CurrentTrailers { get; set; }

    public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set; get; }
    //public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

    public const int MAX_EPS_PER = 50;
    public EpisodeResult[] AllEpisodes;

    // public event EventHandler Added;

    public MovieResultMainEpisodeView()
    {
        MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        CurrentTrailers = new ObservableCollection<Trailer>();
    }
}





