using Acr.UserDialogs;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
        public MainEpisodeView epView;



        public Download()
        {
            InitializeComponent();
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
                                    App.SetKey(nameof(DownloadEpisodeInfo), "id" + id, new DownloadEpisodeInfo() { dtype = DownloadType.YouTube, source = v.GetShortUrl(), description = v.Description, downloadHeader = header.RealId, episode = -1, season = -1, fileUrl = filePath, id = id, name = v.Title });
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
                        }
                    })
                });
            };


            epView = new MainEpisodeView();
            BindingContext = epView;

            BackgroundColor = Settings.BlackRBGColor;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            BackgroundColor = Settings.BlackRBGColor;
            UpdateDownloads();


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
        List<DownloadPoster> downloadposter = new List<DownloadPoster>();
        [Serializable]
        struct DownloadPoster
        {
            public Button button;
            public int id;
            public string moviePath;
            public string name;
        }

        void AddEpisode(EpisodeResult episodeResult)
        {
            epView.MyEpisodeResultCollection.Add(episodeResult);
            SetHeight();
        }
        void SetHeight()
        {
            Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = epView.MyEpisodeResultCollection.Count * episodeView.RowHeight + 20);
        }

        void UpdateDload()
        {
            List<string> keys = App.GetKeys<string>("DownloadIds");
            List<string> keysPaths = App.GetKeysPath("DownloadIds");

            downloads.Clear();
            downloadHeaders.Clear();
            downloadHelper.Clear();

            for (int i = 0; i < keys.Count; i++) {
                int id = App.GetKey<int>(keysPaths[i], 0);
                var info = App.GetDownloadInfo(id);

                if (!downloads.ContainsKey(id)) {
                    downloads[id] = info;
                }

                int headerId = info.info.downloadHeader;
                if (!downloadHeaders.ContainsKey(headerId)) {
                    var header = App.GetDownloadHeaderInfo(headerId);
                    downloadHeaders[headerId] = header;
                }

                if (!downloadHelper.ContainsKey(headerId)) {
                    downloadHelper[headerId] = new DownloadHeaderHelper() { infoIds = new List<int>() { id }, bytesUsed = new List<long>() { info.state.bytesDownloaded }, totalBytesUsed = new List<long>() { info.state.totalBytes } };
                }
                else {
                    var helper = downloadHelper[headerId];
                    helper.infoIds.Add(id);
                    helper.totalBytesUsed.Add(info.state.totalBytes);
                    helper.bytesUsed.Add(info.state.bytesDownloaded);
                    downloadHelper.Remove(headerId);
                    downloadHelper.Add(headerId, helper);
                }

                print(info.info.name);
                print(info.info.season);
                print(info.state.state.ToString() + info.state.bytesDownloaded + "|" + info.state.totalBytes + "|" + info.state.ProcentageDownloaded + "%");
                // App.GetDownloadHeaderInfo()
                print("ID???????==" + id);
            }

            // ========================== SET VALUES ==========================

            epView.MyEpisodeResultCollection.Clear();

            List<EpisodeResult> eps = new List<EpisodeResult>();
            foreach (var key in downloadHeaders.Keys) {

                var val = downloadHeaders[key];
                var helper = downloadHelper[key];

                EpisodeResult ep = new EpisodeResult() { Title = val.name, PosterUrl = val.hdPosterUrl, Description = App.ConvertBytesToAny(helper.TotalBytes, 0, 2) + " MB" };

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
                    string extraString = "Downloading";
                    switch (info.state.state) {
                        case App.DownloadState.Downloading:
                            break;
                        case App.DownloadState.Downloaded:
                            extraString = "Downloaded";
                            break;
                        case App.DownloadState.NotDownloaded: // SHOULD NEVER HAPPEND; SHOULD BE REMOVED BEFOREHAND
                            extraString = "Error";
                            break;
                        case App.DownloadState.Paused:
                            extraString = "Paused";
                            break;
                        default:
                            break;
                    }

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
                AddEpisode(ep);
                // epView.MyEpisodeResultCollection.Add(ep);
                // eps.Add(ep);
            }

            foreach (var dload in downloads.Values) {
                if (dload.info.dtype == App.DownloadType.YouTube) {
                    // ADD YOUTUBE EPISODE
                }
                else {

                }
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


        void UpdateDownloads()
        {
            UpdateDload();
            return;
            List<string> keys = App.GetKeys<string>("Download");
            List<string> keysPaths = App.GetKeysPath("Download");
            foreach (var item in keysPaths) {
                print("KEYPATH:" + item);
            }
            List<string> data = new List<string>();
            downloadposter = new List<DownloadPoster>();
            // Downloads.Children.Clear();
            epView.MyEpisodeResultCollection.Clear();

            for (int i = 0; i < keys.Count; i++) {
                string __key = App.ConvertToObject<string>(keys[i], "");
                if (__key == "") {
                    try {
                        App.RemoveKey(keysPaths[i]);
                    }
                    catch (Exception) {

                    }
                    continue;
                }
                string moviePath = FindHTML(__key, "_dpath=", "|||");
                string posterUrl = FindHTML(__key, "_ppath=", "|||");
                string movieUrl = FindHTML(__key, "_mppath=", "|||");
                string episodeDescript = FindHTML(__key, "_descript=", "|||");
                string movieDescript = FindHTML(__key, "_maindescript=", "|||");
                string id = FindHTML(__key, "_epId=", "|||");
                string movieId = FindHTML(__key, "_movieId=", "|||");
                string episodeTitle = FindHTML(__key, "_title=", "|||");
                string movieTitle = FindHTML(__key, "_movieTitle=", "|||");
                string epCounter = FindHTML(__key, "_epCounter=", "|||");
                print("KEY:" + __key);
                //  const double height = 80;
                //  const double width = 126;
                if (moviePath != "") {

                    bool stuckDownload = false;
                    long currentBytes = GetFileBytesOnSystem(moviePath);
                    if (App.GetKey<long>("DownloadSizeBytes", id, -1) == currentBytes) {
                        stuckDownload = true;
                    }
                    else {
                        App.SetKey("DownloadSizeBytes", id, currentBytes);
                    }

                    double currentProgress = GetFileSizeOnSystem(moviePath);
                    double maxProgress = App.GetKey("DownloadSize", id, -1.0);
                    double dprogress = currentProgress / maxProgress;
                    if (currentProgress == -1 || maxProgress == -1) {
                        dprogress = 1;
                    }
                    string extra = "";
                    bool downloadDone = false;
                    if (dprogress != -1) {
                        downloadDone = dprogress > 0.98;
                        if (!downloadDone) {

                            extra = " | " + currentProgress + " MB - " + maxProgress + " MB";
                        }
                        else {
                            extra = " | " + maxProgress + " MB";
                        }
                    }
                    bool isYouTube = __key.Contains("isYouTube=" + true);

                    if (downloadDone) { stuckDownload = false; }

                    AddEpisode(new EpisodeResult() {
                        Description = episodeDescript,
                        PosterUrl = posterUrl,
                        Id = i,
                        Title = episodeTitle + extra,
                        ExtraProgress = dprogress,
                        MainTextColor = stuckDownload ? "#D10E3C" : "#e7e7e7",
                        ExtraColor = stuckDownload ? "#D10E3C" : "#303F9F",
                        MainDarkTextColor = stuckDownload ? "#7D0824" : "#808080",

                        DownloadNotDone = !downloadDone,
                        Mirros = new List<string>() { "Download" },
                        mirrosUrls = new List<string>() { moviePath },
                        extraInfo = "KeyPath=" + keysPaths[i] + "|||_mppath=" + movieUrl + "|||_dpath=" + moviePath + "|||_ppath=" + posterUrl + "|||_movieId=" + movieId + "|||_movieTitle=" + movieTitle + "|||isYouTube=" + isYouTube + "|||=EndAll"
                    });
                    /*
                    Grid stackLayout = new Grid();
                    Button imageButton = new Button() { HeightRequest = height, WidthRequest = width, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
                    var ff = new FFImageLoading.Forms.CachedImage {
                        Source = posterUrl,
                        HeightRequest = height,
                        WidthRequest = width,
                        BackgroundColor = Color.Transparent,
                        VerticalOptions = LayoutOptions.Start,
                        Transformations = {
                                new FFImageLoading.Transformations.RoundedTransformation(10,2.5,1,10,"#303F9F")
                            },
                        InputTransparent = true,
                    };
                    var epTit = new Label() { Text = episodeTitle };
                    var epDesc = new Label() { Text = episodeDescript };
                    //Source = p.posterUrl

                    stackLayout.Children.Add(ff);
                    stackLayout.Children.Add(imageButton);
                    stackLayout.Children.Add(epTit);
                //    stackLayout.Children.Add(epDesc);
                    //stackLayout.WidthRequest = 0;
                    var c = new ColumnDefinition(); c.Width = new GridLength(1, GridUnitType.Auto);
                    stackLayout.ColumnDefinitions = new ColumnDefinitionCollection() { c, c, c };


                    Grid.SetColumn(epTit, 1);
                   // Grid.SetColumn(epDesc, 1);
                    downloadposter.Add(new DownloadPoster() { button = imageButton, id = i, moviePath = moviePath, name = episodeTitle });
                 //   Grid.SetRow(stackLayout, Downloads.Children.Count);
                   // Downloads.Children.Add(stackLayout);

                    // --- RECOMMENDATIONS CLICKED -----
                    imageButton.Clicked += (o, _e) => {
                        for (int z = 0; z < downloadposter.Count; z++) {
                            if (((Button)o).Id == downloadposter[z].button.Id) {
                                App.PlayVLCWithSingleUrl(downloadposter[z].moviePath, downloadposter[z].name);
                                // PushPageFromUrlAndName(bookmarkPosters[z].id, bookmarkPosters[z].name);
                            }
                        }
                    };
                }*/

                }
            }
        }

        private void episodeView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            //EpisodeResult episodeResult = ((EpisodeResult)((ListView)sender).BindingContext);
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
            EpsodeShow(episodeResult);
            //EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
            //App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
            //episodeView.SelectedItem = null;
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


        async void EpsodeShow(EpisodeResult episodeResult)
        {
            string action = await DisplayActionSheet(episodeResult.Title, "Cancel", null, "Play", "Delete File", "Open Source");
            if (action == "Play") {
                PlayEpisode(episodeResult);
            }
            if (action == "Delete File") {
                string moviePath = FindHTML(episodeResult.extraInfo, "_dpath=", "|||");
                string keyPath = FindHTML(episodeResult.extraInfo, "KeyPath=", "|||");
                //string posterUrl = FindHTML(episodeResult.extraInfo, "_ppath=", "|||");
                // string movieUrl = FindHTML(episodeResult.extraInfo, "_mppath=", "|||");
                //App.DeleteFile(movieUrl);
                //App.DeleteFile(posterUrl);
                DeleteFile(moviePath, keyPath);
            }
            if (action == "Open Source") {
                if (episodeResult.extraInfo.Contains("isYouTube=" + true)) {
                    App.OpenBrowser(FindHTML(episodeResult.extraInfo, "_movieId=", "|||"));
                }
                else {

                    string title = FindHTML(episodeResult.extraInfo, "_movieTitle=", "|||");
                    string movieId = FindHTML(episodeResult.extraInfo, "_movieId=", "|||");
                    PushPageFromUrlAndName(movieId, title);
                }
            }
            UpdateDownloads();
        }

        public static void DeleteFile(string keyPath)
        {
            string keyData = App.GetKey(keyPath, "");
            string moviePath = FindHTML(keyData, "_dpath=", "|||");
            DeleteFile(moviePath, keyPath);
        }

        public static void DeleteFileFromFolder(string keyData, string keyFolder, string keyId)
        {
            string moviePath = FindHTML(keyData, "_dpath=", "|||");
            DeleteFile(moviePath, keyFolder, keyId);
        }

        public static void DeleteFile(string moviePath, string keyPath)
        {
            if (App.DeleteFile(moviePath)) {
                App.RemoveKey(keyPath);
                App.ShowToast("Deleted File");
            }
        }

        public static void DeleteFile(string moviePath, string keyFolder, string keyId)
        {
            if (App.DeleteFile(moviePath)) {
                App.RemoveKey(keyFolder, keyId);
                App.ShowToast("Deleted File");
            }
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
            PlayEpisode(episodeResult);
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
            return await client.GetVideoAuthorChannelAsync(videoId);
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