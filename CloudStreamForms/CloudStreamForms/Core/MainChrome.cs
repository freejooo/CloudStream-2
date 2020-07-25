using GoogleCast;
using GoogleCast.Channels;
using GoogleCast.Models.Media;
using SubtitlesParser.Classes;
using SubtitlesParser.Classes.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Core
{
    public static class MainChrome
    {
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static string SubData = "";
        public static bool isSubActive = false;

        public static string CreateSubListiner(string sub)
        {
            if (!sub.IsClean()) return "";
            string url = $"http://{GetLocalIPAddress()}:51337/sub.vtt/"; // 1234
            SubData = sub;
            if (isSubActive) return url;
            isSubActive = true;

            var thread = CloudStreamCore.mainCore.CreateThread(77);
            mainCore.StartThread("SubThread", () => {
                try {
                    using (var listener = new HttpListener()) {
                        listener.Prefixes.Add(url);

                        listener.Start();

                        while (true) {
                            if (!mainCore.GetThredActive(thread)) {
                                print("ABORT!!!!!!!!!!!!");
                                return;
                            }
                            print("Listening...");

                            HttpListenerContext context = listener.GetContext();
                            HttpListenerRequest request = context.Request;

                            using (HttpListenerResponse response = context.Response) {
                                response.ContentType = "text/vtt";
                                response.StatusCode = 200;
                                response.AppendHeader("access-control-expose-headers", "Content-Length, Date, Server, Transfer-Encoding, X-GUploader-UploadID, X-Google-Trace, origin, range");
                                response.AppendHeader("access-control-allow-origin", "*");
                                response.AppendHeader("accept-ranges", "bytes");
                                if (request.HttpMethod == "OPTIONS") {
                                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                                    response.AddHeader("Access-Control-Max-Age", "1728000");
                                }

                                string responseString = SubData;
                                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                                response.ContentLength64 = buffer.Length;
                                using (var output = response.OutputStream) {
                                    output.Write(buffer, 0, buffer.Length);
                                }
                            }
                        }
                    }
                }
                catch (Exception _ex) {
                    print("MAMAMMAMAMMAMAMMAMA: " + _ex);
                }
            });
            return url;
        }


        public static event EventHandler OnDisconnected;
        public static event EventHandler<bool> OnVideoCastingChanged;
        public static event EventHandler OnConnected;
        public static event EventHandler OnChromeDevicesFound;
        public static event EventHandler<string> OnChromeImageChanged;
        public static event EventHandler<bool> OnPauseChanged;

        private static ChromeNotification _notification = new ChromeNotification() { isCasting = false, title = "", body = "", isPaused = false, posterUrl = "" };
        public static ChromeNotification Notification {
            set {
                _notification = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, value);
                });
            }
            get { return _notification; }
        }

        public class ChromeNotification
        {
            public string title;
            public string body; // MIRRORNAME
            public string posterUrl;
            public bool isCasting;
            public bool isPlaying;
            public bool isPaused;
        }

        public static event EventHandler<ChromeNotification> OnNotificationChanged;


        public static bool IsChromeDevicesOnNetwork {
            get {
                if (allChromeDevices == null) { return false; }
                foreach (IReceiver r in allChromeDevices) {
                    return true;
                }
                return false;
            }
        }

        public static bool IsConnectedToChromeDevice { set; get; }
        public static bool IsPendingConnection { set; get; }
        public static bool IsCastingVideo {
            set {
                _isCastingVideo = value; Notification.isCasting = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnVideoCastingChanged?.Invoke(null, value); OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return _isCastingVideo; }
        }
        private static bool _isCastingVideo;
        static bool _IsPaused = false;



        //            Notification.isPaused = IsPaused;

        private static bool isPaused;
        private static bool isPlaying;
        public static bool IsPaused {
            set {
                isPaused = value; Notification.isPaused = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return isPaused; }
        }
        public static bool IsPlaying {
            set {
                isPlaying = value; Notification.isPlaying = value;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
            }
            get { return isPlaying; }
        }

        public static bool IsBuffering { set; get; }
        public static bool IsIdle { set; get; }

        public static double CurrentCastingDuration { get; set; }

        public static IEnumerable<IReceiver> allChromeDevices;
        public static IMediaChannel CurrentChannel;
        public static MediaStatus CurrentChromeMedia;
        public static Sender chromeSender;
        public static IReceiver chromeRecivever;
        static DateTime castUpdatedNow;
        static double castLastUpdate;

        private static float _Volume = 1;
        public static float Volume {
            set {
                SetVolumeAsync(value); _Volume = value;
            }
            get { return _Volume; }
        }

        public static double CurrentTime {
            get {
                try {
                    if (RequestNextTime != -1 && IsBuffering) {
                        return RequestNextTime;
                    }

                    if (IsPaused) {
                        return PausedTime;
                    }
                    else if (IsBuffering || IsIdle) {
                        return BufferTime;
                    }
                    else {
                        TimeSpan t = DateTime.Now.Subtract(castUpdatedNow);
                        double currentTime = castLastUpdate + t.TotalSeconds;
                        return currentTime;
                    }
                }
                catch (System.Exception) {
                    return CurrentCastingDuration; // CAST STOPPED FROM EXTERNAL
                }
            }
        }

        public static double PausedTime { set; get; } = 0;
        public static double BufferTime { set; get; } = 0;
        public static double RequestNextTime { set; get; } = 0;

        private static bool IsStopped {
            get {
                return CurrentChannel.Status == null || !string.IsNullOrEmpty(CurrentChannel.Status.FirstOrDefault()?.IdleReason);
            }
        }

        public static int CurrentImage = 0;

        public static string MultiplyString(string s, int times)
        {
            return String.Concat(Enumerable.Repeat(s, times));
        }

        public static string ConvertScoreToArcadeScore(object inp, int maxLetters = 8, string multiString = "0")
        {
            string inpS = inp.ToString();
            inpS = MultiplyString(multiString, maxLetters - inpS.Length) + inpS;
            return inpS;
        }

        public static string GetSourceFromInt(int inp = -1)
        {
            if (inp == -1) {
                inp = 4;
            }
            if (inp == 0) {
                return "round_cast_white_48dp2_0.png";
            }
            return "round_cast_white_48dp_blue_" + inp + ".png";//"ic_media_route_connected_dark_" + ConvertScoreToArcadeScore(inp, 2) + "_mtrl.png";
        }

        public static string CurrentImageSource { get { return GetSourceFromInt(CurrentImage); } }

        public static async Task StartImageChanger()
        {
            try {
                while (true) {
                    int lastImage = int.Parse(CurrentImage.ToString());
                    if (IsPendingConnection) {
                        CurrentImage++;
                        if (CurrentImage > 3) {
                            CurrentImage = 1;
                        }
                    }
                    else {
                        CurrentImage += IsConnectedToChromeDevice ? 1 : -1;
                    }
                    if (CurrentImage < 0) CurrentImage = 0;
                    if (CurrentImage > 4) CurrentImage = 4;
                    if (!IsChromeDevicesOnNetwork) {
                        CurrentImage = 0;
                    }
                    if (lastImage != CurrentImage) {
                        Device.BeginInvokeOnMainThread(() => {
                            OnChromeImageChanged?.Invoke(null, CurrentImageSource);
                        });
                    }
                    await Task.Delay(500);
                }


            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public static async void GetAllChromeDevices()
        {
            try {
                print("SCANNING");
                allChromeDevices = await new DeviceLocator().FindReceiversAsync();
                print("SCANNED");
                print("FOUND " + allChromeDevices.ToList().Count + " CHROME DEVICES");
                if (IsChromeDevicesOnNetwork) {
                    Device.BeginInvokeOnMainThread(() => {
                        try {
                            OnChromeDevicesFound?.Invoke(null, null);
                        }
                        catch (Exception _ex) {
                            error(_ex);
                        }
                    });
                }
            }
            catch (Exception _ex) {
                error("ERROR LOADING CHROME::" + _ex);
            }

        }

        public static List<string> GetChromeDevicesNames()
        {
            if (allChromeDevices == null) {
                return new List<string>();
            }
            List<string> allNames = new List<string>();
            foreach (IReceiver r in allChromeDevices) {
                allNames.Add(r.FriendlyName);
            }
            return allNames;
        }

        private static void ChromeChannel_StatusChanged(object sender, EventArgs e)
        {
            MediaStatus mm = CurrentChannel.Status.FirstOrDefault();

            IsPlaying = (mm.PlayerState == "PLAYING");
            IsBuffering = (mm.PlayerState == "BUFFERING");
            IsIdle = (mm.PlayerState == "IDLE");
            IsPaused = (mm.PlayerState == "PAUSED");

            BufferTime = CurrentTime;
            PausedTime = CurrentTime;

            if (IsPlaying && RequestNextTime != -1) {
                print("SET NEXT REQUEST TO -1");
                RequestNextTime = -1;
            }

            if (_IsPaused != IsPaused) {
                _IsPaused = IsPaused;
                Device.BeginInvokeOnMainThread(() => {
                    OnPauseChanged?.Invoke(null, IsPaused);
                });
            }


            print("STATE::" + mm.PlayerState);



            castUpdatedNow = DateTime.Now;
            castLastUpdate = mm.CurrentTime;
        }

        static Dictionary<string, string> subtitleParsed = new Dictionary<string, string>();
        static bool ContainsStartColor(string inp)
        {
            return inp.Contains("<font color=");
        }

        public static List<SubtitleItem> ParseSubtitles(string _inp)
        {
            var parser = GetSubtiteParser(_inp);
            byte[] byteArray = Encoding.UTF8.GetBytes(_inp);
            MemoryStream stream = new MemoryStream(byteArray);
            return parser.ParseStream(stream, Encoding.UTF8).Select(t => {

                // REMOVE BLOAT
                for (int i = 0; i < t.Lines.Count; i++) {
                    var inp = t.Lines[i];
                    while (ContainsStartColor(inp)) {
                        t.Lines[i] = inp.Replace($"<font color=\"{FindHTML(inp, "<font color=\"", "\"")}\">", "");
                    }
                }

                t.Lines = t.Lines.Select(_t => _t.Replace("<i>", "").Replace("{i}", "").Replace("<b>", "").Replace("{b}", "").Replace("<u>", "").Replace("{u}", "").Replace("</i>", "").Replace("{/i}", "").Replace("</b>", "").Replace("{/b}", "").Replace("</u>", "").Replace("{/u}", "").Replace("</font>", "")).ToList();
                return t;
            }).ToList();
        }

        public static ISubtitlesParser GetSubtiteParser(string _inp)
        {
            while (_inp.StartsWith(" ") || _inp.StartsWith("\n")) {
                _inp = _inp.Remove(0, 1);
            }

            if (_inp.StartsWith("[INFORMATION]")) {
                return new SubViewerParser();
            }
            else if (_inp.StartsWith("WEBVTT")) {
                return new VttParser();
            }
            else if (_inp.StartsWith("<?xml version=\"")) {
                return new TtmlParser();
            }
            else if (_inp.StartsWith("[Script Info]") || _inp.StartsWith("Title:")) {
                return new SsaParser();
            }
            else if (_inp.StartsWith("1")) {
                return new SrtParser();
            }
            else if (_inp.StartsWith("0:00")) {
                return new YtXmlFormatParser();
            }
            else {
                return new SrtParser();
            }
        }

        /// <summary>
        /// Generates a link for subtitles, If it is the first time it will create a thread on Id77 
        /// </summary>
        /// <param name="data">subtitledata in any major format</param>
        /// <param name="offset">offset im ms</param>
        /// <returns></returns>
        public static string GenerateSubUrl(string data, int offset = 0)
        {
            if (data == null) return "";

            string realSubText = "";
            if (data != "") {
                try {
                    var _sub = ParseSubtitles(data).ToArray();
                    print("SUBLENNN::: " + _sub);
                    realSubText += "WEBVTT\n\n";
                    string ToSubTime(int tick)
                    {
                        if (tick < 0) {
                            tick = 0;
                        }

                        var str = TimeSpan.FromMilliseconds(tick).ToString();
                        if (str.Length == 8) {
                            str += ".";
                        }
                        int reqLen = Math.Max(0, 12 - str.Length);
                        if (reqLen > 0) {
                            str += MultiplyString("0", reqLen);
                        }
                        return str.Substring(0, 12);
                    }

                    for (int i = 0; i < _sub.Length; i++) {
                        realSubText += (i + 1).ToString() + "\n";
                        realSubText += ToSubTime(_sub[i].StartTime + offset) + " --> " + ToSubTime(_sub[i].EndTime + offset) + "\n";
                        foreach (var line in _sub[i].Lines) {
                            realSubText += line + "\n";
                        }
                        realSubText += "\n";
                    }
                    print("DDADADAA");
                }
                catch (Exception _ex) {
                    print("MINNNNN EX: " + _ex);
                    return "";
                }
            }
            return CreateSubListiner(realSubText);
        }

        static string videoStreamPath;
        static bool videoFileThreadCreated = false;

        static void ListWriteFile(HttpListenerContext ctx, string path)
        {
            try {
                var p = ctx.Response;
                var req = ctx.Request;
                p.SendChunked = true;

                using (FileStream fs = new FileStream(videoStreamPath, FileMode.Open)) {
                    int startByte = -1;
                    int endByte = -1;
                    if (req.Headers.AllKeys.Contains("Range")) {
                        string rangeHeader = req.Headers["Range"].ToString().Replace("bytes=", "");
                        string[] range = rangeHeader.Split('-');
                        startByte = int.Parse(range[0]);
                        if (range[1].Trim().Length > 0) int.TryParse(range[1], out endByte);
                        if (endByte == -1) endByte = (int)fs.Length;//<startByte + 1024;//
                    }
                    else {
                        startByte = 0;
                        endByte = (int)fs.Length;
                    }
                    //  byte[] buffer = new byte[endByte - startByte];
                    fs.Position = startByte;
                    //fs.Seek(startByte, SeekOrigin.Begin);

                    p.StatusCode = 206;
                    p.StatusDescription = ("Partial Content");
                    int len = endByte - startByte;
                    p.ContentType = "video/mp4";
                    p.AddHeader("Accept-Ranges", "bytes");
                    int totalCount = startByte + len;
                    p.AddHeader("Content-Range", string.Format("bytes {0}-{1}/{2}", startByte, totalCount - 1, totalCount));
                    p.ContentLength64 = (len);
                    print("BUFFERSEND: " + startByte + "|" + endByte + "|" + len);
                    p.KeepAlive = true;

                    byte[] temp = new byte[1024];
                    int count = 0;
                    while ((count = fs.Read(temp, 0, 1024)) > 0) {
                        p.OutputStream.Write(temp, 0, count);
                    }

                    //int read = fs.Read(buffer, 0, endByte - startByte);
                    //  fs.Flush();
                    fs.Close();

                    // p.Close(buffer, true); 
                    p.OutputStream.Flush();
                }

            }
            catch (Exception _ex) {
                error("Socceterror: " + _ex);
            }
        }

        public static string GenerateVideoUrlFromFile(string filepath)
        {
            videoStreamPath = filepath;
            string url = $"http://{GetLocalIPAddress()}:51336/video.mp4/";
            print("VIDEOURL:: " + url);

            if (videoFileThreadCreated) {
                return url;
            }

            videoFileThreadCreated = true;
            var thread = CloudStreamCore.mainCore.CreateThread(76);
            mainCore.StartThread("VideoThread", () => {
                try {
                    using (var listener = new HttpListener()) {
                        listener.Prefixes.Add(url);

                        listener.Start();
                        // Task.Factory.StartNew(() =>
                        // {
                        while (true) {
                            try {

                                HttpListenerContext context = listener.GetContext();
                                Task.Factory.StartNew((ctx) => {
                                    ListWriteFile((HttpListenerContext)ctx, videoStreamPath);
                                }, context, TaskCreationOptions.LongRunning);

                            }
                            catch (Exception _ex) {
                                error(_ex);
                            }
                        }
                        // }, TaskCreationOptions.LongRunning);

                        /*
                        while (true) {
                            if (!mainCore.GetThredActive(thread)) {
                                print("ABORT!!!!!!!!!!!!");
                                return;
                            }
                            print("Listening VIDEO...");

                        

                            /*
                            var result = listener.BeginGetContext(ListenerCallback, listener);
                            result.AsyncWaitHandle.WaitOne(); 
                        }*/
                    }
                }
                catch (Exception _ex) {
                    print("MAMAMMAMAMMAMAMMAMA: " + _ex);
                }
            });
            return url;
        }

        /*
        public static async Task ToggleSubtitles(bool isEnabled, string lang = null)
        { 
        }*/

        public static async Task<bool> ChangeSubtitles(string subFile, string name, int delay = 0)
        {
            bool done = false;
            bool suc = false;
            Thread t = new Thread(async () => {
                suc = await CastVideo(currentUrl, currentMirrorName, -2, subFile, name, currentPosterUrl, currentMovieName, delay);
                done = true;
            });
            t.Start();
            while (!done) {
                await Task.Delay(100);
            }

            /*if (suc) {
                await ToggleSubtitles(true);
            }*/
            return suc;
        }

        static string currentUrl = "";
        static string currentMirrorName = "";
        static string currentPosterUrl = "";
        static string currentMovieName = "";

        // Subtitle Url https://static.movies123.pro/files/tracks/JhUzWRukqeuUdRrPCe0R3lUJ1SmknAVSv670Ep0cXipm1JfMgNWa379VKKAz8nvFMq2ksu7bN5tCY5tXXKS4Lrr1tLkkipdLJNArNzVSu2g.srt
        public static async Task<bool> CastVideo(string url, string mirrorName, double setTime = -1, string subtitleUrl = "", string subtitleName = "", string posterUrl = "", string movieTitle = "", int subtitleDelay = 0, bool fromFile = false)
        {
            try {
                if (setTime == -2) {
                    setTime = CurrentTime;
                }

                //CurrentChannel.;
                //CurrentChromeMedia.CurrentTime;
                //chromeSender.;
                //chromeRecivever.;

                if (fromFile) {
                    url = GenerateVideoUrlFromFile(url);
                }

                currentUrl = url;
                currentMirrorName = mirrorName;
                currentPosterUrl = posterUrl;
                currentMovieName = movieTitle;

                GenericMediaMetadata mediaMetadata = new GenericMediaMetadata();

                RequestNextTime = setTime;

                bool validSubtitle = false;
                var mediaInfo = new MediaInformation() { ContentId = url, Metadata = mediaMetadata };
                // mediaInfo.StreamType = fromFile ? StreamType.Live : StreamType.Buffered;

                print("REALLSLSLLS:: " + subtitleUrl);

                string realSub = GenerateSubUrl(subtitleUrl, subtitleDelay);
                //   subtitleUrl = "https://commondatastorage.googleapis.com/gtv-videos-bucket/CastVideos/tracks/DesigningForGoogleCast-en.vtt";


                // SUBTITLES
                if (realSub != "") {
                    validSubtitle = true;
                    mediaInfo.Tracks = new Track[]
                               {
                                 new Track() {  TrackId = 1, Language = "en-US" , Name = subtitleName, TrackContentId = realSub,SubType=TextTrackType.Subtitles,Type=TrackType.Text}
                               };
                    mediaInfo.TextTrackStyle = new TextTrackStyle() {
                        FontFamily = Settings.GlobalSubtitleFont,
                        BackgroundColor = System.Drawing.Color.Transparent,//.Color.Transparent,
                        EdgeColor = System.Drawing.Color.Black,
                        EdgeType = TextTrackEdgeType.Outline,
                        ForegroundColor = System.Drawing.Color.White,
                        //WindowColor = System.Drawing.Color.Red,
                        //  WindowType = TextTrackWindowType.RoundedCorners,
                        // WindowRoundedCornerRadius = 5,
                        FontScale = 1.05f,
                    };
                }

                mediaMetadata.Title = mirrorName;
                mediaMetadata.Images = new GoogleCast.Models.Image[] { new GoogleCast.Models.Image() { Url = posterUrl, Height = 200, Width = 200 } };

                if (validSubtitle) {
                    CurrentChromeMedia = await CurrentChannel.LoadAsync(mediaInfo, true, 1);
                }
                else {
                    CurrentChromeMedia = await CurrentChannel.LoadAsync(mediaInfo);
                }

                print("START!!");

                CurrentCastingDuration = (double)CurrentChromeMedia.Media.Duration;
                if (setTime != -1) {
                    SetChromeTime(setTime);
                }
                CurrentChannel.StatusChanged += ChromeChannel_StatusChanged;

                castUpdatedNow = DateTime.Now;
                castLastUpdate = 0;
                IsCastingVideo = true;
                Notification.body = mirrorName;
                Notification.title = movieTitle;
                Notification.posterUrl = posterUrl;
                Device.BeginInvokeOnMainThread(() => {
                    OnNotificationChanged?.Invoke(null, Notification);
                });
                print("!4");
                return true;
            }
            catch (System.Exception) {
                print("ERROR");
                await Task.CompletedTask;
                return false;
            }
        }

        public static EventHandler<double> OnForceUpdateTime;

        public static void SetChromeTime(double time)
        {
            print("Seek To: " + time);

            BufferTime = time;
            PausedTime = time;

            IsBuffering = true;
            IsPlaying = false;

            castUpdatedNow = DateTime.Now;
            castLastUpdate = time;

            OnForceUpdateTime?.Invoke(null, time);
            CurrentChannel.SeekAsync(time);
        }

        public static void SeekMedia(double sec)
        {
            SetChromeTime(CurrentTime + sec);
        }

        public static void PauseAndPlay(bool paused)
        {
            try {
                if (paused) {
                    PausedTime = CurrentTime;
                    CurrentChannel.PauseAsync();
                }
                else {
                    CurrentChannel.PlayAsync();
                }
            }
            catch (Exception _ex) {
                print("EX PAUSED::: " + _ex);
            }

        }

        public static async void JustStopVideo()
        {
            ShowLogo();

            /*
            try {
                await CurrentChannel.StopAsync();
            }
            catch (System.Exception) {
                await Task.CompletedTask;
            }
            try {
                await chromeSender.LaunchAsync(CurrentChannel);
            }
            catch (Exception) {
                await Task.CompletedTask;
            }*/


            IsCastingVideo = false;
            print("STOP CASTING! VIDEO");
        }

        public static async void StopCast()
        {
            try {
                await CurrentChannel.StopAsync();
            }
            catch (System.Exception) {
                await Task.CompletedTask;
            }
            try {
                CurrentChannel.Sender.Disconnect();
            }
            catch (System.Exception) {

            }
            Device.BeginInvokeOnMainThread(() => {
                OnDisconnected?.Invoke(null, null);
            });
            IsConnectedToChromeDevice = false;
            IsCastingVideo = false;
            print("STOP CASTING!");
        }

        public static async void ShowLogo()
        {
            CurrentChromeMedia = await CurrentChannel.LoadAsync(new MediaInformation() { ContentId = "https://cdn.discordapp.com/attachments/551382684560261121/730169809408622702/ChromecastLogo6.png", StreamType = StreamType.None, ContentType = "image/jpeg" });
        }

        public static async void ConnectToChromeDevice(string name)
        {
            if (name == "Disconnect") {
                StopCast();
                return;
            }

            // if (chromeRecivers.Count() > 0) {
            foreach (IReceiver r in allChromeDevices) {
                if (r.FriendlyName == name) {
                    chromeSender = new Sender();

                    // Connect to the Chromecast
                    try {
                        IsPendingConnection = true;
                        await chromeSender.ConnectAsync(r);
                        chromeRecivever = r;
                        Console.WriteLine("CONNECTED");
                        CurrentChannel = chromeSender.GetChannel<IMediaChannel>();
                        await chromeSender.LaunchAsync(CurrentChannel);

                        ShowLogo();

                        IsConnectedToChromeDevice = true;

                        Device.BeginInvokeOnMainThread(() => {
                            OnConnected?.Invoke(null, null);
                        });
                    }
                    catch (System.Exception) {
                        await Task.CompletedTask; // JUST IN CASE
                    }
                    IsPendingConnection = false;
                    return;
                }
            }
            //}
        }

        private static async Task SetVolumeAsync(float level) // 0 = 0%, 1 = 100%
        {
            if (IsCastingVideo) {
                await SendChannelCommandAsync<IReceiverChannel>(IsStopped, null, async c => await c.SetVolumeAsync(level));
            }
            else {
                await Task.CompletedTask;
            }
        }

        private static async Task SendChannelCommandAsync<TChannel>(bool condition, Func<TChannel, Task> action, Func<TChannel, Task> otherwise) where TChannel : IChannel => await InvokeAsync(condition ? action : otherwise);

        private static async Task InvokeAsync<TChannel>(Func<TChannel, Task> action) where TChannel : IChannel
        {
            if (action != null) {
                await action?.Invoke(chromeSender.GetChannel<TChannel>());
            }
        }


        static string GetUrlFromUploadSubtitles(string fullData, string ending = ".srt")
        {
            string boundary = Guid.NewGuid().ToString();

            WebRequest request = WebRequest.Create("https://uguu.se/api.php?d=upload-tool");
            request.Method = "POST";
            request.ContentType = string.Format("multipart/form-data; boundary={0}", boundary);
            //request.Headers.Add(HttpRequestHeader.Cookie, header);
            string ff = string.Format("--{0}", boundary) + "\n";

            string textMode = "plain";
            if (ending == ".vtt") {
                textMode = "vtt";
            }

            string _1 = $"{ff}Content-Disposition: form-data; name=\"MAX_FILE_SIZE\"\n\n150000000\n";
            string _2 = $"{ff}Content-Disposition: form-data; name=\"file\"; filename=\"subtitles{ending}\"\nContent-Type: text/{textMode}\n\n{fullData}\n\n";
            string _3 = $"{ff}Content-Disposition: form-data; name=\"name\"\n\n\n";
            string _4 = $"--{boundary}--";

            byte[] _data = Encoding.UTF8.GetBytes(_1 + _2 + _3 + _4);

            if (_data != null) {
                request.ContentLength = _data.Length;
            }

            Stream dataStream = request.GetRequestStream();

            if (_data != null && _data.Length > 0) {
                dataStream.Write(_data, 0, _data.Length);
            }

            dataStream.Close();

            WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream);

            string responseReader = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseReader;
        }
    }

}
