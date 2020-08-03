using CloudStreamForms.Core;
using CloudStreamForms.Models;
using FFImageLoading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.InputPopupPage;
using static CloudStreamForms.MainPage;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Download : ContentPage
    {
        private ObservableCollection<EpisodeResult> _MyEpisodeResultCollection;
        public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

        public event EventHandler Added;


        private bool _isRefreshing = false;
        public bool IsRefreshing {
            get { return _isRefreshing; }
            set {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public ICommand RefreshCommand {
            get {
                return new Command(async () => {
                    IsRefreshing = true;
                    print("YEET;;;;");
                    UpdateDownloaded();
                    await Task.Delay(100);
                    // await RefreshData();

                    IsRefreshing = false;
                });
            }
        }

        public Download()
        {
            InitializeComponent();
            if (Settings.IS_TEST_BUILD) {
                return;
            }
            try {
                baseImg.Source = App.GetImageSource("NoDownloads.png");

                bool hasDownloads = App.GetKey("Settings", "hasDownloads", false);

                baseTxt.IsVisible = !hasDownloads;
                baseImg.IsVisible = !hasDownloads;

                /*
                RefreshView refresh = new RefreshView() { BackgroundColor = Color.Black, RefreshColor = Color.Blue, Command = RefreshCommand };
                refresh.SetBinding(RefreshView.IsRefreshingProperty, new Binding(nameof(IsRefreshing)));
                ScrollView scrollView = new ScrollView();
                scrollView.Content = episodeView;
                refresh.Content = episodeView;*/

                // ytBtt.Source = App.GetImageSource("round_movie_white_48dp.png");
                ytBtt.Source = App.GetImageSource("ytIcon.png");

                ytrealBtt.Clicked += async (o, e) => {
                    string txt = await Clipboard.GetTextAsync();
                    string ytUrl = await ActionPopup.DisplayEntry(InputPopupResult.url, "https://youtu.be/", "Youtube link", confirmText: "Download");
                    if (!ytUrl.IsClean() || ytUrl == "Cancel") return;
                    await Device.InvokeOnMainThreadAsync(async () => {
                        //string ytUrl = t.Text;
                        Video v = null;
                        const string errorTxt = "Error Downloading YouTube Video";
                        try {
                            v = await YouTube.GetYTVideo(ytUrl);
                        }
                        catch (Exception) {
                            App.ShowToast(errorTxt);
                        }
                        if (v == null) {
                            App.ShowToast(errorTxt);
                        }
                        else {
                            try {
                                //   string dpath = YouTube.GetYTPath(v.Title);
                                var author = await YouTube.GetAuthorFromVideoAsync(v);
                                var data = await YouTube.GetInfoAsync(v);

                                double mb = App.ConvertBytesToAny(data.Size.TotalBytes, 5, 2);

                                ImageService.Instance.LoadUrl(v.Thumbnails.HighResUrl, TimeSpan.FromDays(30)); // CASHE IMAGE

                                DownloadHeader header = new DownloadHeader() { movieType = MovieType.YouTube, id = author.Id, name = author.Title, hdPosterUrl = author.LogoUrl, posterUrl = author.LogoUrl, ogName = author.Title }; //description = v.Description,hdPosterUrl=v.Thumbnails.HighResUrl, };//ConvertTitleToHeader(title);
                                int id = ConvertStringToInt(v.Id);

                                string filePath = YouTube.DownloadVideo(data, v.Title, data, v, id, header, true);

                                //  YouTube.client.Videos.ClosedCaptions.GetManifestAsync().Result.Tracks.
                                // YouTube.client.Videos.ClosedCaptions.DownloadAsync(v.,)

                                App.ShowToast("YouTube download started");
                                App.SetKey(nameof(DownloadHeader), "id" + header.RealId, header);
                                App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.YouTube, source = v.Url, description = v.Description, downloadHeader = header.RealId, episode = -1, season = -1, fileUrl = filePath, id = id, name = v.Title, hdPosterUrl = v.Thumbnails.HighResUrl });
                                App.SetKey("DownloadIds", id.ToString(), id);
                                await YouTube.DownloadSubtitles(v, filePath.Replace(".mp4", ".srt"));
                            }
                            catch (Exception _ex) {
                                print("MAINERROR:: " + _ex);
                                App.ShowToast(errorTxt);
                            }
                        }
                    });
                    /*UserDialogs.Instance.Prompt(new PromptConfig() {
                        Title = "YouTube Download",
                        CancelText = "Cancel",
                        OkText = "Download",
                        InputType = InputType.Url,
                        Text = txt,
                        Placeholder = "YouTube Link",
                        OnAction = new Action<PromptResult>(async t => {
                            if (t.Ok) {

                            }
                        })
                    });*/
                };

                MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
                BindingContext = this;
                BackgroundColor = Settings.BlackRBGColor;
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }
        protected override void OnDisappearing()
        {
            // OnIconEnd(2);
            base.OnDisappearing();
        }


        protected override void OnAppearing()
        {
            //OnIconStart(2);

            base.OnAppearing();
            if (Settings.IS_TEST_BUILD) {
                return;
            }

            try {
                BackgroundColor = Settings.BlackRBGColor;

                UpdateDownloaded();

                var d = App.GetStorage();
                try {
                    SpaceProgress.Progress = d.UsedProcentage;

                    FreeSpace.Text = "Free Space · " + App.ConvertBytesToGB(d.FreeSpace) + "GB";
                    UsedSpace.Text = "Used Space · " + App.ConvertBytesToGB(d.UsedSpace) + "GB";
                    if (Device.RuntimePlatform == Device.UWP) {
                        OffBar.IsVisible = false;
                        OffBar.IsEnabled = false;
                        DownloadSizeGrid.HeightRequest = 25;
                    }
                    else {
                        OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
                    }
                }
                catch (Exception) {

                }
                episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;

            }
            catch (Exception _ex) {
                error(_ex);
            }
            //print("PRO:" + d.UsedProcentage + " Total Size: " + App.ConvertBytesToGB(d.TotalSpace, 2) + "GB Current Space: " + App.ConvertBytesToGB(d.FreeSpace, 2) + "GB" + " Used Space: " + App.ConvertBytesToGB(d.UsedSpace, 2) + "GB");

        }

        void SetHeight()
        {
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = 10000);//episodeView.HeightRequest = MyEpisodeResultCollection.Count * episodeView.RowHeight + 200);
        }

        public static void RemoveDownloadCookie(int? id = null, int? headerId = null)
        {
            if (id != null) {
                App.RemoveKey(App.hasDownloadedFolder, id.ToString());
                App.RemoveKey(nameof(DownloadEpisodeInfo), "id" + id);
                App.RemoveKey("DownloadIds", id.ToString());
                App.RemoveKey("dlength", "id" + id);
            }

            if (headerId != null) {
                print("REMOVE COOKIIE:: " + headerId);
                App.RemoveKey(nameof(DownloadHeader), "id" + headerId);
            }
        }

        void UpdateDownloaded()
        {
            if (Settings.IS_TEST_BUILD) {
                return;
            }

            try {
                object clock = new object();

                Thread mThread = new Thread(() => {
                    Thread.Sleep(100);

                    try {
                        List<string> keys = App.GetKeys<string>("DownloadIds");
                        List<string> keysPaths = App.GetKeysPath("DownloadIds");

                        downloads.Clear();
                        downloadHeaders.Clear();
                        downloadHelper.Clear();

                        List<int> headerRemovers = new List<int>();
                        Dictionary<int, bool> validHeaders = new Dictionary<int, bool>();

                        Parallel.For(0, keys.Count, (i) => {
                            try {

                                // Thread.Sleep(1000);

                                //  for (int i = 0; i < keys.Count; i++) {
                                int id = App.GetKey<int>(keysPaths[i], 0);
                                var info = App.GetDownloadInfo(id);

                                //if (!downloads.ContainsKey(id)) {
                                lock (clock) {
                                    downloads[id] = info;
                                }
                                //} 
                                if (info.state.totalBytes == 0 && info.state.bytesDownloaded != 1) {
                                    print("REMOVE HEADER INFO ID BC 0 data");
                                    RemoveDownloadCookie(id);
                                    App.UpdateDownload(id, 2);
                                    headerRemovers.Add(info.info.downloadHeader);
                                }
                                else {
                                    int headerId = info.info.downloadHeader;
                                    print("HEADERSTAET::" + headerId);
                                    lock (clock) {
                                        validHeaders[headerId] = true;
                                    }
                                    bool containsKey;
                                    lock (clock) {
                                        containsKey = downloadHeaders.ContainsKey(headerId);
                                    }

                                    if (!containsKey) {
                                        var header = App.GetDownloadHeaderInfo(headerId);
                                        lock (clock) {
                                            downloadHeaders[headerId] = header;
                                        }
                                    }
                                    lock (clock) {
                                        bool containsHelperKey = downloadHelper.ContainsKey(headerId);

                                        if (!containsHelperKey) {
                                            downloadHelper[headerId] = new DownloadHeaderHelper() { infoIds = new List<int>() { id }, bytesUsed = new List<long>() { info.state.bytesDownloaded }, totalBytesUsed = new List<long>() { info.state.totalBytes } };
                                        }
                                        else {
                                            var helper = downloadHelper[headerId];
                                            helper.infoIds.Add(id);
                                            helper.totalBytesUsed.Add(info.state.totalBytes);
                                            helper.bytesUsed.Add(info.state.bytesDownloaded);
                                            downloadHelper[headerId] = helper;
                                        }
                                    }
                                }

                                print("ID???????==" + id + "  > " + info.info.name + " |" + info.info.season + "|" + info.state.state.ToString() + " Bytes: " + info.state.bytesDownloaded + "|" + info.state.totalBytes + "|" + info.state.ProcentageDownloaded + "%");


                            }
                            catch (Exception _ex) {
                                print("ERROR WHILE DOWNLOADLOADING::" + _ex);
                            }
                        });

                        for (int i = 0; i < headerRemovers.Count; i++) {
                            if (!validHeaders.ContainsKey(headerRemovers[i])) {
                                print("HEADER:::==" + headerRemovers[i]);
                                RemoveDownloadCookie(null, headerRemovers[i]);
                            }
                        }

                        // ========================== SET VALUES ==========================


                        //   List<EpisodeResult> eps = new List<EpisodeResult>();
                        var ckeys = downloadHeaders.Keys.OrderBy(t => t).ToList();
                        EpisodeResult[] epres = new EpisodeResult[ckeys.Count];
                        Parallel.For(0, ckeys.Count, i => {
                            int key;
                            lock (clock) {
                                key = ckeys[i];
                            }
                            // });
                            //  Parallel.ForEach(, key => {
                            //  foreach (var key in downloadHeaders.Keys) { 
                            DownloadHeader val;
                            DownloadHeaderHelper helper;
                            lock (clock) {
                                val = downloadHeaders[key];
                                helper = downloadHelper[key];
                            }

                            EpisodeResult ep = new EpisodeResult() { Title = val.name, PosterUrl = val.hdPosterUrl, Description = App.ConvertBytesToAny(helper.TotalBytes, 0, 2) + " MB", Id = key };

                            if (val.movieType == MovieType.TVSeries || val.movieType == MovieType.Anime || val.movieType == MovieType.YouTube) {
                                int count = helper.infoIds.Count;
                                ep.Description = count + $" {(val.movieType == MovieType.YouTube ? "Video" : "Episode")}{(count > 1 ? "s" : "")} | " + ep.Description;

                                int downloadingRn = 0;
                                foreach (var id in helper.infoIds) {
                                    switch (downloads[id].state.state) {
                                        case App.DownloadState.Downloading:
                                            downloadingRn++;
                                            break;
                                        case App.DownloadState.Downloaded:
                                            break;
                                        case App.DownloadState.NotDownloaded:
                                            break;
                                        case App.DownloadState.Paused:
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                ep.ExtraDescription = downloadingRn == 0 ? "" : $"Downloading {downloadingRn} of {count}";  //extraString + (info.state.state == App.DownloadState.Downloaded ? "" : $" {(int)info.state.ProcentageDownloaded}%");
                            }

                            if (val.movieType == MovieType.Movie || val.movieType == MovieType.AnimeMovie) {
                                var info = downloads[helper.infoIds[0]];

                                ep.Description = (info.state.state == App.DownloadState.Downloaded ? "" : App.ConvertBytesToAny(helper.Bytes, 0, 2) + " MB of ") + ep.Description;
                                string extraString = GetExtraString(info.state.state);

                                ep.ExtraDescription = extraString + (info.state.state == App.DownloadState.Downloaded ? "" : $" {(int)info.state.ProcentageDownloaded}%");
                            }
                            else if (val.movieType == MovieType.YouTube) {

                            }
                            else if (val.movieType == MovieType.TVSeries) {
                                // redirect to real  
                            }
                            else if (val.movieType == MovieType.Anime) {
                                // redirect to real 
                            }
                            // AddEpisode(ep);
                            ep.Episode = ConvertToSortOrder(val.movieType) * 1000 + i;
                            lock (clock) {
                                epres[i] = ep;
                                //  MyEpisodeResultCollection.Add(ep);
                            }
                            print("ADD EP::: " + ep.Title);
                            // epView.MyEpisodeResultCollection.Add(ep);
                            // eps.Add(ep);
                            // }
                        });

                        for (int i = 0; i < ckeys.Count; i++) {
                            print(i + "KEY:::" + ckeys[i]);
                        }

                        epres = epres.OrderBy(t => t.Episode).ToArray(); // MOVIE -> ANIMEMOVIE -> TV-SERIES -> ANIME -> YOUTUBE

                        MyEpisodeResultCollection.Clear();
                        for (int i = 0; i < epres.Length; i++) {
                            print("IIIIII::: " + i + "." + epres[i].Title);
                            MyEpisodeResultCollection.Add(epres[i]);
                        }

                        App.SetKey("Settings", "hasDownloads", epres.Length != 0);

                        Device.BeginInvokeOnMainThread(() => {
                            // episodeView.Opacity = 0;
                            // episodeView.FadeTo(1, 200);

                            bool noDownloads = epres.Length == 0;
                            baseTxt.IsVisible = noDownloads;
                            baseImg.IsVisible = noDownloads;
                            SetHeight();
                        });
                    }
                    catch (Exception _ex) {
                        error(_ex);
                    }
                });
                //mThread.SetApartmentState(ApartmentState.STA);
                mThread.Start();
            }
            catch (Exception _ex) {
                error(_ex);
            }
            /*
            foreach (var dload in downloads.Values) {
                if (dload.info.dtype == App.DownloadType.YouTube) {
                    // ADD YOUTUBE EPISODE
                }
                else {

                }
            }*/
        }

        public static int ConvertToSortOrder(MovieType movieType)
        {
            switch (movieType) {
                case MovieType.Movie:
                    return 0;
                case MovieType.TVSeries:
                    return 2;
                case MovieType.Anime:
                    return 3;
                case MovieType.AnimeMovie:
                    return 1;
                case MovieType.YouTube:
                    return 4;
                default:
                    return -1;
            }
        }


        public static string GetExtraString(DownloadState state)
        {
            switch (state) {
                case App.DownloadState.Downloading:
                    return "Downloading";
                case App.DownloadState.Downloaded:
                    return "Downloaded";
                case App.DownloadState.NotDownloaded: // CAN HEPPEND IF DOWNLOADED, BUT STOPPED DUE TO INTERNET or NOT DOWNLOADED
                    return "Stopped";
                case App.DownloadState.Paused:
                    return "Paused";
                default:
                    return "";
            }
        }

        public class DownloadHeaderHelper
        {
            public List<int> infoIds;
            public List<long> bytesUsed;
            public List<long> totalBytesUsed;
            public long TotalBytes { get { return totalBytesUsed.Sum(); } }
            public long Bytes { get { return bytesUsed.Sum(); } }
        }

        public static Dictionary<int, DownloadHeaderHelper> downloadHelper = new Dictionary<int, DownloadHeaderHelper>();

        public static Dictionary<int, App.DownloadInfo> downloads = new Dictionary<int, App.DownloadInfo>();
        public static Dictionary<int, App.DownloadHeader> downloadHeaders = new Dictionary<int, App.DownloadHeader>();



        private void episodeView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            episodeView.SelectedItem = null;

            //  EpisodeResult episodeResult = ((EpisodeResult)((ListView)sender).BindingContext);

            //PlayEpisode(episodeResult);

        }

        /*
        void PlayEpisode(EpisodeResult episodeResult)
        {
            App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
            print("MAIN EP FROM DLOAD: " + episodeResult.Id.ToString());
            
            MovieResult.SetEpisode("tt"+ episodeResult.Id);
            episodeView.SelectedItem = null;
        }*/

        private void ViewCell_Tapped(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = (EpisodeResult)(((ViewCell)sender).BindingContext);
            HandleEpisodeAsync(episodeResult);
            episodeView.SelectedItem = null;
            //            EpsodeShow(episodeResult);
            //EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
            //episodeView.SelectedItem = null;
        }

        async Task HandleEpisodeAsync(EpisodeResult episodeResult)
        {
            await HandleEpisode(episodeResult, this);
            UpdateDownloaded();
        }

        public static async Task HandleEpisode(EpisodeResult episodeResult, Page p)
        {
            int key = episodeResult.Id;

            DownloadHeader header = downloadHeaders[key];
            if (header.movieType == MovieType.AnimeMovie || header.movieType == MovieType.Movie) {
                var infoKey = downloadHelper[key].infoIds[0];
                await HandleEpisodeTapped(infoKey, p);
            }
            else {
                p.Navigation.PushModalAsync(new DownloadViewPage(key), false);
            }
        }

        public static void PlayDownloadedFile(DownloadInfo _info, bool? vlc = null)
        {
            PlayDownloadedFile(_info.info, vlc);
            //Download.PlayDownloadedFile(_info.info.fileUrl, _info.info.name, _info.info.episode, _info.info.season, _info.info.episodeIMDBId, _info.info.source, true, vlc ?? !Settings.UseVideoPlayer);
        }
        public static void PlayDownloadedFile(DownloadEpisodeInfo _info, bool? vlc = null)
        {
            Download.PlayDownloadedFile(_info.fileUrl, _info.name, _info.episode, _info.season, _info.episodeIMDBId, _info.source, true, vlc ?? !Settings.UseVideoPlayer);
        }

        public static void PlayDownloadedFile(string file, string name, int episode, int season, string episodeId, string headerId, bool startFromSaved = true, bool vlc = false)
        {
            long pos = startFromSaved ? GetViewPos(episodeId) : -1L;//App.GetKey(VIEW_TIME_POS, _episodeId, -1L);
            if (vlc) {
                //long dur = App.GetKey(VIEW_TIME_DUR, episodeId, -1L);
                print("MAINPOS: " + pos + "|" + episodeId);
                if (pos != -1L) {// && dur != -1L) {
                    RequestVlc(new List<string>() { file }, new List<string>() { name }, name, episodeId, startId: pos,overrideSelectVideo:false);
                }
                else {
                    RequestVlc(new List<string>() { file }, new List<string>() { name }, name, episodeId,overrideSelectVideo:false);
                }
                return;
            }


            Page p = new VideoPage(new VideoPage.PlayVideo() {
                descript = "",
                name = name,
                isSingleMirror = true,
                episode = episode,
                isDownloadFile = true,
                season = season,

                downloadFileUrl = file,
                //Subtitles = subtitlesEnabled ? new List<string>() { subtitleFull } : new List<string>(),
                //SubtitlesNames = subtitlesEnabled ? new List<string>() { "English" } : new List<string>(),
                startPos = pos,
                episodeId = episodeId,
                headerId = headerId,
            });//new List<string>() { "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4" }, new List<string>() { "Black" }, new List<string>() { });// { mainPoster = mainPoster };
            ((MainPage)CloudStreamCore.mainPage).Navigation.PushModalAsync(p, true);
        }

        public static void PlayVLCFile(string file, string name, string episodeId, bool startFromSaved = true)
        {
            long pos = startFromSaved ? GetViewPos(episodeId) : -1L;//App.GetKey(VIEW_TIME_POS, _episodeId, -1L);
                                                                    //long dur = App.GetKey(VIEW_TIME_DUR, episodeId, -1L);
            print("MAINPOS: " + pos + "|" + episodeId);
            if (pos != -1L) {// && dur != -1L) {
                RequestVlc(new List<string>() { file }, new List<string>() { name }, name, episodeId, startId: pos);
            }
            else {
                RequestVlc(new List<string>() { file }, new List<string>() { name }, name, episodeId);
            }

            //App.PlayVLCWithSingleUrl(file, name, overrideSelectVideo: false);
        }


        public static DownloadEpisodeInfo chromeDownload;
        public static async Task HandleEpisodeTapped(int key, Page p)
        {
            DownloadInfo info = downloads[key];
            List<string> actions = new List<string>() {
                 "Play in App","Delete File", "Open Source"
            };
            if(App.CanPlayExternalPlayer()) {
                actions.Insert(1, "Play External App");
            }
            /*
            if (!MainChrome.IsConnectedToChromeDevice && MainChrome.allChromeDevices.Count() > 0) {
                actions.Insert(0, "Connect to Chromecast");
            }
            else*/ if (MainChrome.IsConnectedToChromeDevice) {
               // actions.Insert(0, "Disconnect");
                actions.Insert(0, "Chromecast");
            }

            string action = await ActionPopup.DisplayActionSheet(info.info.name, actions.ToArray());//await p.DisplayActionSheet(info.info.name, "Cancel", null, "Play", "Delete File", "Open Source");

            if (action == "Connect to Chromecast") {
                List<string> names = MainChrome.GetChromeDevicesNames();
                //   if (MainChrome.IsConnectedToChromeDevice) { names.Add("Disconnect"); }
                string a = await ActionPopup.DisplayActionSheet("Cast to", names.ToArray());//await DisplayActionSheet("Cast to", "Cancel", MainChrome.IsConnectedToChromeDevice ? "Disconnect" : null, names.ToArray());
                if (a != "Cancel") {
                    MainChrome.ConnectToChromeDevice(a);
                }
            }
            else if (action == "Disconnect") {
                MainChrome.ConnectToChromeDevice("Disconnect");
            }
            else if (action == "Chromecast") {
                var header = downloadHeaders[info.info.downloadHeader];
                 
                await MainChrome.CastVideo(info.info.fileUrl, info.info.name, posterUrl: header.posterUrl, movieTitle: header.name, fromFile: true);
                chromeDownload = info.info;
                Page _p = ChromeCastPage.CreateChromePage(info.info); //{ episodeResult = new EpisodeResult() { }, chromeMovieResult = new Movie() { } };
                MainPage.mainPage.Navigation.PushModalAsync(_p, false);
            }
            else if (action == "Play External App") {
                PlayDownloadedFile(info, true);
            }
            else if (action == "Play in App") {
                PlayDownloadedFile(info, false);
                // PlayVLCFile(info.info.fileUrl, info.info.name, info.info.id.ToString());
                //App.PlayVLCWithSingleUrl(, overrideSelectVideo: false);
            }
            else if (action == "Delete File") {
                bool succ = App.DeleteFile(info.info.fileUrl);
                App.DeleteFile(info.info.fileUrl.Replace(".mp4", ".srt"));
                if (succ) {
                    App.ShowToast("Deleted File");
                    App.UpdateDownload(info.info.id, 2);
                    RemoveDownloadCookie(info.info.id);
                }
                else {
                    App.ShowToast("Failed to delete file");
                }
            }
            else if (action == "Open Source") {
                if (info.info.dtype == DownloadType.YouTube) {
                    App.OpenBrowser(info.info.source);
                }
                else {
                    var header = downloadHeaders[info.info.downloadHeader];
                    PushPageFromUrlAndName(info.info.source, header.name);
                }
            }

            // info.info.fileUrl
        }

        /*
        public static void PlayFile(string keyData, string title = "")
        {
            string moviePath = FindHTML(keyData, "_dpath=", "|||");
            App.PlayVLCWithSingleUrl(moviePath, title);
        }*/

        List<FFImageLoading.Forms.CachedImage> play_btts = new List<FFImageLoading.Forms.CachedImage>();
        private void Image_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {

            FFImageLoading.Forms.CachedImage image = ((FFImageLoading.Forms.CachedImage)sender);

            if (play_btts.Where(t => t.Id == image.Id).Count() == 0) {
                play_btts.Add(image);
                image.Source = image.Source = App.GetImageSource("nexflixPlayBtt.png");//ImageSource.FromResource("CloudStreamForms.Resource.playBtt.png", Assembly.GetExecutingAssembly());
                if (Device.RuntimePlatform == Device.Android) {
                    image.Scale = 0.5f;
                }
                else {
                    image.Scale = 0.3f;
                }
            }

        }

        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            HandleEpisodeAsync(episodeResult);
            //HandleEpisode(episodeResult, this);
            //PlayEpisode(episodeResult);
        }


        private void Grid_BindingContextChanged(object sender, EventArgs e)
        {
            var c = ((FFImageLoading.Forms.CachedImage)((Grid)sender).Children[1]);

            c.Transformations.Clear();
            var ep = ((EpisodeResult)c.BindingContext);
            c.Transformations = new List<FFImageLoading.Work.ITransformation>() { new FFImageLoading.Transformations.RoundedTransformation() { BorderHexColor = ep.ExtraColor, BorderSize = 0, Radius = 1, CropHeightRatio = 1.5 } }; //1.77

        }
    }

    public static class YouTube
    {
        public static async Task<Video> GetYTVideo(string url)
        {
            return await client.Videos.GetAsync(url);
        }

        public static readonly YoutubeClient client = new YoutubeClient();

        public static async Task<Channel> GetAuthorFromVideoAsync(Video videoId)
        {
            Channel t = null;
            for (int i = 0; i < 10; i++) {
                if (t == null) {
                    try {
                        t = await client.Channels.GetByVideoAsync(videoId.Id);
                        //await client.Videos.GetVideoAuthorChannelAsync(videoId);
                    }
                    catch (Exception _ex) {
                        print("CHANNELL ASYNC FAILED:" + _ex);
                    }
                }
            }
            if (t == null) throw new Exception("Channel Failed");

            return t;
        }

        public static async Task<MuxedStreamInfo> GetInfoAsync(Video v)
        {
            var manifest = await client.Videos.Streams.GetManifestAsync(v.Id);
            return manifest.GetMuxed().WithHighestVideoQuality() as MuxedStreamInfo;
        }

        public static async Task DownloadSubtitles(Video v, string path)
        {
            try {
                print("SUBFÅAJJ: " + v.Title);
                var manifest = await client.Videos.ClosedCaptions.GetManifestAsync(v.Id);
                print("S:S::S: ");
                var lan = manifest.TryGetByLanguage("English");
                if (lan != null) {
                    print("LAN: " + lan);
                    await client.Videos.ClosedCaptions.DownloadAsync(lan, path);
                }
                else {
                    print("ERORORLAN:");
                }

            }
            catch (Exception _ex) {
                print("MAIN EX IN DLOADSUBTITLE: " + _ex);
            }

            //client.Videos.ClosedCaptions.DownloadAsync()
        }

        public static string DownloadVideo(MuxedStreamInfo mediaStreamInfos, string name, MuxedStreamInfo info, Video v, int id, DownloadHeader header, bool isNotification = false)
        {
            string rootPath = App.GetDownloadPath("", "/YouTube");
            if (rootPath.EndsWith("\\")) {
                rootPath = rootPath.Substring(0, rootPath.Length - 1);
            }

            if (!File.Exists(rootPath)) {
                Directory.CreateDirectory(rootPath);
            }

            print("DLOADING " + mediaStreamInfos.Size);
            print("DURL::" + mediaStreamInfos.Url);

            ImageService.Instance.LoadUrl(v.Thumbnails.HighResUrl, TimeSpan.FromDays(30)); // CASHE IMAGE
            string extraPath = "/" + GetPathFromType(header);

            string fileUrl = platformDep.DownloadHandleIntent(id, new List<BasicMirrorInfo>() { new BasicMirrorInfo() { name = info.VideoQualityLabel,mirror = info.Url } }, v.Title + "." + info.Container.Name, name, true, extraPath, true, true, false, v.Thumbnails.HighResUrl, "{name}\n");//isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));
            return fileUrl;
        }
    }

    public class MainEpisodeView
    {
        private ObservableCollection<EpisodeResult> _MyEpisodeResultCollection;
        public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

        public event EventHandler Added;

        public MainEpisodeView()
        {
            MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
        }
    }
}