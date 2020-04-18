using Acr.UserDialogs;
using CloudStreamForms.Models;
using FFImageLoading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using YoutubeExplode;
using YoutubeExplode.Models;
using YoutubeExplode.Models.MediaStreams;
using static CloudStreamForms.App;
using static CloudStreamForms.CloudStreamCore;
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
        public bool IsRefreshing
        {
            get { return _isRefreshing; }
            set {
                _isRefreshing = value;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }

        public ICommand RefreshCommand
        {
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

                UserDialogs.Instance.Prompt(new PromptConfig() {
                    Title = "YouTube Download",
                    CancelText = "Cancel",
                    OkText = "Download",
                    InputType = InputType.Url,
                    Text = txt,
                    Placeholder = "YouTube Link",
                    OnAction = new Action<PromptResult>(async t => {
                        if (t.Ok) {
                            await Device.InvokeOnMainThreadAsync(async () => {
                                string ytUrl = t.Text;
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
                                        var author = await YouTube.GetAuthorFromVideoAsync(v.Id);
                                        var data = await YouTube.GetInfoAsync(v.GetUrl());

                                        double mb = App.ConvertBytesToAny(data.Size, 5, 2);
                                        // (episodeResult.mirrosUrls[i], episodeResult.Title + ".mp4", true, "/" + GetPathFromType());
                                        //  string ppath = App.DownloadUrl(episodeResult.PosterUrl, "epP" + episodeResult.Title + ".jpg", false, "/Posters");
                                        // string mppath = App.DownloadUrl(currentMovie.title.hdPosterUrl, "hdP" + episodeResult.Title + ".jpg", false, "/TitlePosters");
                                        string mppath = v.Thumbnails.HighResUrl;
                                        string ppath = v.Thumbnails.HighResUrl;

                                        DownloadHeader header = new DownloadHeader() { movieType = MovieType.YouTube, id = author.Id, name = author.Title, hdPosterUrl = author.LogoUrl, posterUrl = author.LogoUrl, ogName = author.Title }; //description = v.Description,hdPosterUrl=v.Thumbnails.HighResUrl, };//ConvertTitleToHeader(title);
                                        int id = ConvertStringToInt(v.Id);

                                        string filePath = YouTube.DownloadVideo(data, v.Title, data, v, id, header, true);
                                        App.ShowToast("YouTube download started");
                                        App.SetKey(nameof(DownloadHeader), "id" + header.RealId, header);
                                        App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.YouTube, source = v.GetShortUrl(), description = v.Description, downloadHeader = header.RealId, episode = -1, season = -1, fileUrl = filePath, id = id, name = v.Title, hdPosterUrl = v.Thumbnails.HighResUrl });
                                        App.SetKey("DownloadIds", id.ToString(), id);

                                        /*
                                        string key = "_dpath=" + dpath + "|||_ppath=" + ppath + "|||_mppath=" + mppath + "|||_descript=" + v.Description + "|||_maindescript=" + v.Description + "|||_epCounter=" + "-1" + "|||_epId=" + v.Id + "|||_movieId=" + v.GetUrl() + "|||_title=" + v.Title + "|||_movieTitle=" + v.Title + "|||isYouTube=" + true + "|||UploadData=" + v.UploadDate.ToString() + "|||Author=" + v.Author + "|||Duration=" + v.Duration.TotalSeconds + "|||=EndAll";
                                        print("DKEY: " + key);
                                        App.SetKey("Download", v.Id, key);
                                        App.ShowToast("Download Started - " + Math.Round(mb, 1) + "MB");
                                        App.SetKey("DownloadSize", v.Id, Math.Round(mb, 2));*/
                                        // YouTube.DownloadVideo(data, v.Title);

                                    }
                                    catch (Exception _ex) {
                                        print("MAINERROR:: " + _ex);
                                        App.ShowToast(errorTxt);
                                    }
                                }
                            });
                        }
                    })
                });
            };

            MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
            BindingContext = this;
            BackgroundColor = Settings.BlackRBGColor;
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
            object clock = new object();

            Thread mThread = new Thread(() => {
                Thread.Sleep(100);

                List<string> keys = App.GetKeys<string>("DownloadIds");
                List<string> keysPaths = App.GetKeysPath("DownloadIds");

                downloads.Clear();
                downloadHeaders.Clear();
                downloadHelper.Clear();

                List<int> headerRemovers = new List<int>();
                Dictionary<int, bool> validHeaders = new Dictionary<int, bool>();

                Parallel.For(0, keys.Count, (i) => {
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

                    print(info.info.name);
                    print(info.info.season);
                    print(info.state.state.ToString() + info.state.bytesDownloaded + "|" + info.state.totalBytes + "|" + info.state.ProcentageDownloaded + "%");
                    // App.GetDownloadHeaderInfo()
                    print("ID???????==" + id);
                    //  }           
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

                Device.BeginInvokeOnMainThread(() => {
                    // episodeView.Opacity = 0;
                    // episodeView.FadeTo(1, 200);
                    SetHeight();
                });
            });
            //mThread.SetApartmentState(ApartmentState.STA);
            mThread.Start();

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

        void PlayEpisode(EpisodeResult episodeResult)
        {
            App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
            episodeView.SelectedItem = null;
        }

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

        private void Grid_PlayVid(object sender, EventArgs e)
        {
            //print("DA");
            var s = ((Grid)sender);
            Commands.SetTap(s, new Command((o) => {
                var episodeResult = ((EpisodeResult)o);
                PlayEpisode(episodeResult);
                //   PlayEpisodeRes(episodeResult);
                //do something
            }));
            Commands.SetTapParameter(s, (EpisodeResult)s.BindingContext);
            //s.BindingContext = this;
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

        public static void PlayVLCFile(string file, string name)
        {
            App.PlayVLCWithSingleUrl(file, name, overrideSelectVideo: false);
        }

        public static async Task HandleEpisodeTapped(int key, Page p)
        {
            DownloadInfo info = downloads[key];
            string action = await p.DisplayActionSheet(info.info.name, "Cancel", null, "Play", "Delete File", "Open Source");

            if (action == "Play") {
                PlayVLCFile(info.info.fileUrl, info.info.name);
                //App.PlayVLCWithSingleUrl(, overrideSelectVideo: false);
            }
            else if (action == "Delete File") {
                bool succ = App.DeleteFile(info.info.fileUrl);
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


        public static void PlayFile(string keyData, string title = "")
        {
            string moviePath = FindHTML(keyData, "_dpath=", "|||");
            App.PlayVLCWithSingleUrl(moviePath, title);
        }

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
            // YoutubeClient client = new YoutubeClient();
            print("URL||||" + url);

            var id = YoutubeClient.ParseVideoId(url); // "bnsUkE8i0tU"
            print("ID::::" + id);

            return await client.GetVideoAsync(id);
        }

        public static string GetYTPath(string name)
        {
            return App.GetDownloadPath(name, "/YouTube") + ".mp4";
        }

        static readonly YoutubeClient client = new YoutubeClient();

        public static async Task<Channel> GetAuthorAsync(string id)
        {
            var _id = await client.GetChannelIdAsync(id);
            return await client.GetChannelAsync(_id);
        }
        public static async Task<Channel> GetAuthorFromVideoAsync(string videoId)
        {
            print("VideoID === " + videoId);

            Channel t = null;
            for (int i = 0; i < 10; i++) {
                if (t == null) {
                    try {
                        t = await client.GetVideoAuthorChannelAsync(videoId);
                    }
                    catch (Exception _ex) {
                        print("CHANNELL ASYNC FAILED:" + _ex);
                    }
                }
            }
            if (t == null) throw new Exception("Channel Failed");

            return t;
        }

        public static async Task<MuxedStreamInfo> GetInfoAsync(string url)
        {
            /*
            print("URL2||||" + url);


            YoutubeClient client = new YoutubeClient();

            var id = YoutubeClient.ParseVideoId(url);
            print("ID2::::" + id);

            var mediaStreamInfoSet = await client.GetVideoMediaStreamInfosAsync(id);
            
            // Select audio stream
            var audioStreamInfo = mediaStreamInfoSet.Audio.WithHighestBitrate();

            // Select video stream
            var videoStreamInfo = mediaStreamInfoSet.Video.OrderBy(t => (t.Resolution.Height * t.Resolution.Width * t.Framerate)).First();//.FirstOrDefault(s => s.VideoQualityLabel == "1080p60");

            // Combine them into a collection
            var mediaStreamInfos = new MediaStreamInfo[] { audioStreamInfo, videoStreamInfo };
            print("LEN:" + mediaStreamInfos.Length);
            return mediaStreamInfos;*/
            var id = YoutubeClient.ParseVideoId(url);

            // Get metadata for all streams in this video
            var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(id);

            // Select one of the streams, e.g. highest quality muxed stream
            var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();

            // ...or highest bitrate audio stream
            // var streamInfo = streamInfoSet.Audio.WithHighestBitrate();

            // ...or highest quality & highest framerate MP4 video stream
            // var streamInfo = streamInfoSet.Video
            //    .Where(s => s.Container == Container.Mp4)
            //    .OrderByDescending(s => s.VideoQuality)
            //    .ThenByDescending(s => s.Framerate)
            //    .First();

            // Get file extension based on stream's container
            // var ext = streamInfo.Container.GetFileExtension();
            //  print("EXTENTION:" + ext);
            return streamInfo;
        }

        public static string DownloadVideo(MediaStreamInfo mediaStreamInfos, string name, MuxedStreamInfo info, Video v, int id, DownloadHeader header, bool isNotification = false)
        {

            // YoutubeConverter converter = new YoutubeConverter();
            //  YoutubeClient client = new YoutubeClient();

            // Download and process them into one file
            /*
            string basePath = App.GetDownloadPath("", "/YouTube");
            string path = App.GetDownloadPath(name, "/YouTube").Replace(basePath, "");
            if (basePath.EndsWith("\\")) {
                basePath = basePath.Substring(0, basePath.Length - 1);
            }
            print("BasePath: " + basePath);
            print("ExtraPath " + path);*/
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
            string fileUrl = platformDep.DownloadHandleIntent(id, new List<string>() { info.Resolution.Height + "p" }, new List<string>() { info.Url }, v.Title + "." + info.Container.GetFileExtension(), name, true, extraPath, true, true, false, v.Thumbnails.HighResUrl, "{name}\n");//isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));
            return fileUrl;
            //App.SetKey("dlength", "id" + id, mediaStreamInfos.Size);
            /*
            try {
                await client.DownloadMediaStreamAsync(mediaStreamInfos, App.GetDownloadPath(name, "/YouTube") + ".mp4");
            }
            catch (Exception _ex) {
                print("EXDLOADYT:::: " + _ex);
            }
            if (isNotification) {
                ShowNotIntent(name, "Download done!", ConvertStringToInt(v.Id), "-1", "-1");
            }*/

            /*
            string extraPath = "/" + GetPathFromType(header);
            print("HEADERID::: " + header.RealId);
            App.SetKey(nameof(DownloadHeader), "id" + header.RealId, header);
            bool isMovie = header.movieType == MovieType.AnimeMovie || header.movieType == MovieType.Movie;

            string fileUrl = platformDep.DownloadHandleIntent(id, mirrorNames, mirrorUrls, downloadTitle, name, true, extraPath, true, true, false, poster, isMovie ? "{name}\n" : ($"S{season}:E{episode} - " + "{name}\n"));
            App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.Normal, source = header.id, description = description, downloadHeader = header.RealId, episode = episode, season = season, fileUrl = fileUrl, id = id, name = name });
            */


            /* if (toast != "") {
                 if (isNotification) {
                     App.ShowNotification(toast, body);
                 }
                 else {
                     App.ShowToast(toast);
                 }
             }*/
            //  await converter.DownloadAndProcessMediaStreamsAsync(mediaStreamInfos, basePath + "/" + path + ".mp4",".mp4");
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