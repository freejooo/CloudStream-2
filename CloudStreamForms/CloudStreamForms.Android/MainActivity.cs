using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Text;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using CloudStreamForms.Core;
using Java.IO;
using Java.Net;
using Javax.Net.Ssl;
using LibVLCSharp.Forms.Shared;
using Newtonsoft.Json;
using Plugin.LocalNotifications;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Droid.LocalNot;
using static CloudStreamForms.Droid.MainActivity;
using static CloudStreamForms.Droid.MainHelper;
using Application = Android.App.Application;
using AudioTrack = Android.Media.AudioTrack;

namespace CloudStreamForms.Droid
{
    // THIS IS USED TO SAVE ALL PROGRESS WHEN THE APP IS KILLED
    [Service]
    public class OnKilledService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnTaskRemoved(Intent rootIntent)
        {
            MainActivity.activity.Killed();
            StopSelf();
            // base.OnTaskRemoved(rootIntent);
        }
    }

    [Service]
    public class DemoIntentService : IntentService
    {
        public DemoIntentService() : base("DemoIntentService")
        {
        }

        protected override void OnHandleIntent(Android.Content.Intent intent)
        {
            print("perform some long running work");
            System.Console.WriteLine("work complete");

            print("HANDLE" + intent.Extras.GetString("data"));
        }
    }

    [Service]
    public class MainIntentService : IntentService
    {
        public MainIntentService() : base("MainIntentService")
        {

        }

        protected override void OnHandleIntent(Android.Content.Intent intent)
        {
            string data = intent.Extras.GetString("data");

            if (data.StartsWith("handleDownload")) {
                int id = int.Parse(FindHTML(data, $"{nameof(id)}=", "|||")); //intent.Extras.GetInt("downloadId");
                int dType = int.Parse(FindHTML(data, $"{nameof(dType)}=", "|||"));
                var manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
                manager.Cancel(id);
                DownloadHandle.isPaused[id] = dType;
                DownloadHandle.changedPause?.Invoke(null, id);
            }
        }
    }


    [Service]
    public class ChromeCastIntentService : IntentService
    {
        public ChromeCastIntentService() : base("ChromeCastIntentService")
        {

        }

        protected override void OnHandleIntent(Android.Content.Intent intent)
        {
            string data = intent.Extras.GetString("data");
            //play" : "pause", "goforward", "stop
            print("HANDLE [" + data + "]");

            switch (data) {
                case "play":
                    MainChrome.PauseAndPlay(false);
                    break;
                case "pause":
                    MainChrome.PauseAndPlay(true);
                    break;
                case "goforward":
                    MainChrome.SeekMedia(30);
                    break;
                case "goback":
                    MainChrome.SeekMedia(-30);
                    break;
                case "stop":
                    //  MainChrome.StopCast();
                    MainChrome.JustStopVideo();
                    break;
                default:
                    break;
            }

        }
    }

    [System.Serializable]
    public class LocalAction
    {
        public string action;
        public string name;
        public int sprite;
    }

    [System.Serializable]
    public class LocalNot
    {
        public string title;
        public string body;
        public int id;
        public bool autoCancel = true;
        public bool showWhen = true;
        public int smallIcon = PublicNot;
        public string bigIcon = "";
        public bool mediaStyle = true;
        public string data = "";
        public bool onGoing = false;
        public int progress = -1;
        public DateTime? when = null;
        public List<LocalAction> actions = new List<LocalAction>();

        public int notificationImportance = (int)NotificationImportance.Default;


        public static NotificationManager _manager => (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
        public static Dictionary<string, Bitmap> cachedBitmaps = new Dictionary<string, Bitmap>(); // TO ADD PREFORMACE WHEN ADDING NOTIFICATION W SAME IMAGE

        public static async Task<Bitmap> GetImageBitmapFromUrl(string url)
        {
            if (cachedBitmaps.ContainsKey(url)) {
                return cachedBitmaps[url];
            }

            try {
                Bitmap imageBitmap = null;

                using (var webClient = new WebClient()) {
                    var imageBytes = await webClient.DownloadDataTaskAsync(url);
                    if (imageBytes != null && imageBytes.Length > 0) {
                        imageBitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    }
                }
                cachedBitmaps.Add(url, imageBitmap);
                return imageBitmap;
            }
            catch (Exception) {
                return null;
            }

        }

        public static long CurrentTimeMillis(DateTime time)
        {
            return (long)(time - Jan1st1970).TotalMilliseconds;
        }

        private static readonly DateTime Jan1st1970 = new DateTime
    (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static PendingIntent GetCurrentPending(string data = "")
        {
            Intent _resultIntent = new Intent(Application.Context, typeof(MainActivity));
            _resultIntent.SetAction(Intent.ActionMain);
            _resultIntent.AddCategory(Intent.CategoryLauncher);
            if (data != "") {
                _resultIntent.PutExtra("data", data);
            }
            PendingIntent pendingIntent = PendingIntent.GetActivity(activity, 0,
       _resultIntent, 0);
            return pendingIntent;
        }

        public static async void ShowLocalNot(LocalNot not, Context context = null)
        {
            var cc = context ?? Application.Context;
            var builder = new Notification.Builder(cc);
            builder.SetContentTitle(not.title);

            bool containsMultiLine = not.body.Contains("\n");

            if (Build.VERSION.SdkInt < BuildVersionCodes.O || !containsMultiLine) {
                builder.SetContentText(not.body);
            }
            builder.SetSmallIcon(not.smallIcon);
            builder.SetAutoCancel(not.autoCancel);
            builder.SetOngoing(not.onGoing);

            if (not.progress != -1) {
                builder.SetProgress(100, not.progress, false);
            }

            builder.SetVisibility(NotificationVisibility.Public);
            builder.SetOnlyAlertOnce(true);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                var channelId = $"{cc.PackageName}.general";
                var channel = new NotificationChannel(channelId, "General", (NotificationImportance)not.notificationImportance);
                _manager.CreateNotificationChannel(channel);

                builder.SetChannelId(channelId);

                if (not.bigIcon != null) {
                    if (not.bigIcon != "") {
                        var bitmap = await GetImageBitmapFromUrl(not.bigIcon);
                        if (bitmap != null) {
                            builder.SetLargeIcon(bitmap);
                            if (not.mediaStyle) {
                                builder.SetStyle(new Notification.MediaStyle()); // NICER IMAGE
                            }
                        }
                    }
                }

                if (containsMultiLine) {
                    var b = new Notification.BigTextStyle();
                    b.BigText(not.body);
                    builder.SetStyle(b); // Text
                                         // builder.SetContentText(not.body);
                }

                if (not.actions.Count > 0) {
                    List<Notification.Action> actions = new List<Notification.Action>();

                    for (int i = 0; i < not.actions.Count; i++) {
                        var _resultIntent = new Intent(context, typeof(MainIntentService));
                        _resultIntent.PutExtra("data", not.actions[i].action);
                        var pending = PendingIntent.GetService(context, 3337 + i + not.id,
                         _resultIntent,
                        PendingIntentFlags.UpdateCurrent
                         );

                        actions.Add(new Notification.Action(not.actions[i].sprite, not.actions[i].name, pending));
                    }

                    builder.SetActions(actions.ToArray());
                }
            }

            builder.SetShowWhen(not.showWhen);
            if (not.when != null) {
                builder.SetWhen(CurrentTimeMillis((DateTime)not.when));
            }
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(cc);

            var resultIntent = GetLauncherActivity(cc);
            if (not.data != "") {

                resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
                var _data = Android.Net.Uri.Parse(not.data);//"cloudstreamforms:tt0371746Name=Iron man=EndAll");
                resultIntent.SetData(_data);
                stackBuilder.AddNextIntent(resultIntent);
                var resultPendingIntent =
              stackBuilder.GetPendingIntent(not.id, (int)PendingIntentFlags.UpdateCurrent);
                builder.SetContentIntent(resultPendingIntent);
            }
            else {
                //Intent resultIntent = new Intent(context, typeof(MainActivity));
                //  stackBuilder.AddParentStack(activity.Class);
                // resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ResetTaskIfNeeded );
                /* resultIntent.SetAction(Intent.ActionMain);
                 resultIntent.AddCategory(Intent.CategoryLauncher);*/
                stackBuilder.AddNextIntent(resultIntent);

                builder.SetContentIntent(GetCurrentPending());
                /*
                var _resultIntent = new Intent(context, typeof(TrampolineActivity));
              //  _resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

                var pending = PendingIntent.GetActivity(context, 42,
                      _resultIntent,
                     PendingIntentFlags.UpdateCurrent
                      );

                stackBuilder.AddNextIntent(_resultIntent);
                builder.SetContentIntent(pending);*/



                // resultIntent.SetFlags(ActivityFlags.task);
            }

            //   print("NOTIFY::: " + not.id);
            try {
                _manager.Notify(not.id, builder.Build());
            }
            catch (Exception _ex) {
                print("ED MANGAER::: " + _ex);
            }

        }
        public static Intent GetLauncherActivity(Context context = null)
        {
            var cc = context ?? Application.Context;
            var packageName = cc.PackageName;
            return cc.PackageManager.GetLaunchIntentForPackage(packageName).SetPackage(null);
        }
    }

    [Service]
    public class NotifyAtTime : IntentService
    {
        public NotifyAtTime() : base("NotifyAtTime")
        {
        }

        protected override void OnHandleIntent(Android.Content.Intent intent)
        {
            ToastLength toastLength = ToastLength.Short;

            Toast.MakeText(Android.App.Application.Context, "Hello world", toastLength).Show();

        }
    }

    [BroadcastReceiver]
    public class AlertReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            ToastLength toastLength = ToastLength.Short;
            LocalNot localNot = new LocalNot();
            foreach (var prop in typeof(LocalNot).GetFields()) {
                if (prop.FieldType == typeof(string)) {
                    prop.SetValue(localNot, intent.Extras.GetString(prop.Name));
                }
                if (prop.FieldType == typeof(int)) {
                    prop.SetValue(localNot, intent.Extras.GetInt(prop.Name));
                }
                if (prop.FieldType == typeof(float)) {
                    prop.SetValue(localNot, intent.Extras.GetFloat(prop.Name));
                }
                if (prop.FieldType == typeof(DateTime)) {
                    prop.SetValue(localNot, DateTime.Parse(intent.Extras.GetString(prop.Name)));
                }
                if (prop.FieldType == typeof(bool)) {
                    prop.SetValue(localNot, intent.Extras.GetBoolean(prop.Name));
                }
            }

            //  Toast.MakeText(Android.App.Application.Context, "da:" + localNot.title, toastLength).Show();
            ShowLocalNot(localNot);

            /*
            try {
              //  print("GOT DATATATA::::::::::::::::::::::::::::::::::::.!!");
                string data = intent.GetStringExtra("data");
                var not = App.ConvertToObject<LocalNot>(data, null);
                if (not != null) {
                    MainDroid.ShowLocalNot(not);
                }
            }
            catch (Exception) {

            }*/
        }
    }



    public static class DownloadHandle
    {

        static Dictionary<int, long> progressDownloads = new Dictionary<int, long>();
        const string DOWNLOAD_KEY = "DownloadProgress";
        const string DOWNLOAD_KEY_INTENT = "DownloadProgressIntent";

        public static void OnKilled()
        {
            try {
                /*
                foreach (var path in App.GetKeysPath(DOWNLOAD_KEY_INTENT)) {
                    //  App.GetKey<long>(DOWNLOAD_KEY, path, 0);
                    string data = App.GetKey<string>(path, null);
                    int id = int.Parse(FindHTML(data, $"{nameof(id)}=", "|||"));
                    App.CancelNotifaction(id);
                }*/

                foreach (var id in DownloadHandle.activeIds) {
                    var manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
                    manager.Cancel(id);
                    //  App.CancelNotifaction(id);
                }
                foreach (var key in outputStreams.Keys) {
                    var outp = outputStreams[key];
                    var inpp = inputStreams[key];
                    outp.Flush();
                    outp.Close();
                    inpp.Close();
                }
                foreach (var key in progressDownloads.Keys) {
                    print("SAVED KEY:" + key);
                    App.SetKey(DOWNLOAD_KEY, key.ToString(), progressDownloads[key]);
                }
            }
            catch (Exception _ex) {
                print("EXEPTION WHEN DESTROYED: " + _ex);
            }
        }

        public static void ResumeIntents()
        {
            try {
                int downloadResumes = 0;
                foreach (var path in App.GetKeysPath(DOWNLOAD_KEY_INTENT)) {
                    //  App.GetKey<long>(DOWNLOAD_KEY, path, 0);
                    downloadResumes++;
                    try {
                        string data = App.GetKey<string>(path, null);
                        HandleIntent(data);
                    }
                    catch (Exception) {

                    }
                }

                if (downloadResumes == 1) {
                    App.ShowToast("Resumed Download");
                }
                else if (downloadResumes != 0) {
                    App.ShowToast($"Resumed {downloadResumes} downloads");
                }
            }
            catch (Exception _ex) {
                App.ShowToast("Error resuming download");
            }
        }

        static Dictionary<int, OutputStream> outputStreams = new Dictionary<int, OutputStream>();
        static Dictionary<int, InputStream> inputStreams = new Dictionary<int, InputStream>();
        public static List<int> activeIds = new List<int>();
        /// <summary>
        /// 0 = download, 1 = Pause, 2 = remove
        /// </summary>
        public static Dictionary<int, int> isPaused = new Dictionary<int, int>();

        public static Dictionary<int, bool> isStartProgress = new Dictionary<int, bool>();

        public static EventHandler<int> changedPause;

        [System.Serializable]
        public struct DownloadHandleNot
        {
            public int id;
            public List<BasicMirrorInfo> mirrors;
            public int mirror;
            public string title;
            public string path;
            public string poster;
            public string fileName;
            public string beforeTxt;
            public bool openWhenDone;
            public bool showNotificaion;
            public bool showDoneNotificaion;
            public bool showDoneAsToast;
        }

        public static void HandleIntent(string data)
        {
            try {
                DownloadHandleNot d = JsonConvert.DeserializeObject<DownloadHandleNot>(data);
                HandleIntent(d.id, d.mirrors, d.mirror, d.title, d.path, d.poster, d.fileName, d.beforeTxt, d.openWhenDone, d.showNotificaion, d.showDoneNotificaion, d.showDoneAsToast, true);

            }
            catch (Exception) {

            }
        }
        public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
        {
            try {
                Task task = Task.Factory.StartNew(() => codeBlock());
                task.Wait(timeSpan);
                return task.IsCompleted;
            }
            catch (AggregateException ae) {
                throw ae.InnerExceptions[0];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mirrorNames"></param>
        /// <param name="mirrorUrls"></param>
        /// <param name="mirror"></param>
        /// <param name="title"></param>
        /// <param name="path">NOT FULL PATH, just subpath, filname will handle the rest</param>
        /// <param name="poster"></param>
        /// <param name="fileName"></param>
        /// <param name="beforeTxt"></param>
        /// <param name="openWhenDone"></param>
        /// <param name="showNotificaion"></param>
        /// <param name="showDoneNotificaion"></param>
        /// <param name="showDoneAsToast"></param>
        /// <param name="resumeIntent"></param>
        public static void HandleIntent(int id, List<BasicMirrorInfo> mirrors, int mirror, string title, string path, string poster, string fileName, string beforeTxt, bool openWhenDone, bool showNotificaion, bool showDoneNotificaion, bool showDoneAsToast, bool resumeIntent)
        {
            const int UPDATE_TIME = 1;

            try {
                fileName = CensorFilename(fileName);

                isStartProgress[id] = true;
                print("START DLOAD::: " + id);
                if (isPaused.ContainsKey(id)) {
                    //if (isPaused[id] == 2) {
                    //  isPaused.Remove(id);
                    //    print("YEET DELETED KEEEE");
                    return;
                    //  }
                }
                var context = Application.Context;

                //$"{nameof(id)}={id}|||{nameof(title)}={title}|||{nameof(path)}={path}|||{nameof(poster)}={poster}|||{nameof(fileName)}={fileName}|||{nameof(beforeTxt)}={beforeTxt}|||{nameof(openWhenDone)}={openWhenDone}|||{nameof(showDoneNotificaion)}={showDoneNotificaion}|||{nameof(showDoneAsToast)}={showDoneAsToast}|||");

                int progress = 0;

                int bytesPerSec = 0;

                void UpdateDloadNot(string progressTxt)
                {
                    //poster != ""
                    if (!isPaused.ContainsKey(id)) {
                        isPaused[id] = 0;
                    }
                    try {
                        int isPause = isPaused[id];
                        bool canPause = isPause == 0;
                        if (isPause != 2) {
                            ShowLocalNot(new LocalNot() { actions = new List<LocalAction>() { new LocalAction() { action = $"handleDownload|||id={id}|||dType={(canPause ? 1 : 0)}|||", name = canPause ? "Pause" : "Resume" }, new LocalAction() { action = $"handleDownload|||id={id}|||dType=2|||", name = "Stop" } }, mediaStyle = false, bigIcon = poster, title = $"{title} - {ConvertBytesToAny(bytesPerSec / UPDATE_TIME, 2, 2)} MB/s", autoCancel = false, showWhen = false, onGoing = canPause, id = id, smallIcon = PublicNot, progress = progress, body = progressTxt }, context); //canPause
                        }
                    }
                    catch (Exception _ex) {
                        print("ERRORLOADING PROGRESS:::" + _ex);
                    }

                }

                async void ShowDone(bool succ, string? overrideText = null)
                {
                    print("DAAAAAAAAAASHOW DONE" + succ);
                    if (showDoneNotificaion) {
                        print("DAAAAAAAAAASHOW DONE!!!!");
                        Device.BeginInvokeOnMainThread(() => {
                            try {
                                print("DAAAAAAAAAASHOW DONE222");
                                ShowLocalNot(new LocalNot() { mediaStyle = poster != "", bigIcon = poster, title = title, autoCancel = true, onGoing = false, id = id, smallIcon = PublicNot, body = overrideText ?? (succ ? "Download done!" : "Download Failed") }, context); // ((e.Cancelled || e.Error != null) ? "Download Failed!"
                            }
                            catch (Exception _ex) {
                                print("SUPERFATALEX: " + _ex);
                            }
                        });
                        //await Task.Delay(1000); // 100% sure that it is downloaded
                        OnSomeDownloadFinished?.Invoke(null, EventArgs.Empty);
                    }
                    else {
                        print("DONT SHOW WHEN DONE");
                    }
                    // Toast.MakeText(context, "PG DONE!!!", ToastLength.Long).Show(); 
                }



                print("START DLOADING");
                void StartT()
                {
                    isStartProgress[id] = true;
                    if (isPaused.ContainsKey(id)) {
                        //if (isPaused[id] == 2) {
                        //    isPaused.Remove(id);
                        return;
                        //  }
                    }

                    Thread t = new Thread(() => {
                        print("START:::");
                        string json = JsonConvert.SerializeObject(new DownloadHandleNot() { id = id, mirrors = mirrors, fileName = fileName, showDoneAsToast = showDoneAsToast, openWhenDone = openWhenDone, showDoneNotificaion = showDoneNotificaion, beforeTxt = beforeTxt, mirror = mirror, path = path, poster = poster, showNotificaion = showNotificaion, title = title });

                        App.SetKey(DOWNLOAD_KEY_INTENT, id.ToString(), json);

                        var mirr = mirrors[mirror];
                        string url = mirr.mirror;
                        string urlName = mirr.name;
                        string referer = mirr.referer ?? "";

                        if ((int)Android.OS.Build.VERSION.SdkInt > 9) {
                            StrictMode.ThreadPolicy policy = new
                            StrictMode.ThreadPolicy.Builder().PermitAll().Build();
                            StrictMode.SetThreadPolicy(policy);
                        }
                        long total = 0;
                        int fileLength = 0;
                        print("START:::2");

                        void UpdateProgress()
                        {
                            UpdateDloadNot($"{beforeTxt.Replace("{name}", urlName)}{progress} % ({ConvertBytesToAny(total, 1, 2)} MB/{ConvertBytesToAny(fileLength, 1, 2)} MB)");
                        }

                        void UpdateFromId(object sender, int _id)
                        {
                            if (_id == id) {
                                UpdateProgress();
                            }
                        }

                        print("START:::3" + path);
                        bool removeKeys = true;
                        var rFile = new Java.IO.File(path, fileName);
                        print("START:::4");

                        try {
                            // CREATED DIRECTORY IF NEEDED
                            try {
                                Java.IO.File __file = new Java.IO.File(path);
                                __file.Mkdirs();
                            }
                            catch (Exception _ex) {
                                print("FAILED:::" + _ex);
                            }
                            print("START:::5");

                            //  fileName = CensorFilename(fileName);
                            // string rpath = path + "/" + fileName;
                            //   print("PATH=====" + rpath + "|" + fileName);

                            print("OPEN URL:L::" + url);
                            URL _url = new URL(url);

                            URLConnection connection = _url.OpenConnection();

                            print("SET CONNECT ::");
                            if (!rFile.Exists()) {
                                print("FILE DOSENT EXITS");
                                rFile.CreateNewFile();
                            }
                            else {
                                if (resumeIntent) {
                                    total = rFile.Length();
                                    connection.SetRequestProperty("Range", "bytes=" + rFile.Length() + "-");
                                }
                                else {
                                    rFile.Delete();
                                    rFile.CreateNewFile();
                                }
                            }
                            print("SET CONNECT ::2");
                            connection.SetRequestProperty("Accept-Encoding", "identity");
                            if (referer != "") {
                                connection.SetRequestProperty("Referer", referer);
                            }
                            int clen = 0;

                            bool Completed = ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(10000), () => {
                                connection.Connect();
                                clen = connection.ContentLength;
                                if (clen < 5000000 && !path.Contains("/YouTube/")) { // min of 5 MB 
                                    clen = 0;
                                }
                                //
                                // Write your time bounded code here
                                // 
                            });
                            if (!Completed) {
                                print("FAILED MASS");
                                clen = 0;
                            }
                            print("SET CONNECT ::3");


                            print("TOTALLLLL::: " + clen);

                            if (clen == 0) {
                                if (isStartProgress.ContainsKey(id)) {
                                    isStartProgress.Remove(id);
                                }
                                if (mirror < mirrors.Count - 1 && progress < 2 && rFile.Length() < 1000000) { // HAVE MIRRORS LEFT
                                    mirror++;
                                    removeKeys = false;
                                    resumeIntent = false;
                                    rFile.Delete();

                                    print("DELETE;;;");
                                }
                                else {
                                    ShowDone(false);
                                }
                            }
                            else {
                                fileLength = clen + (int)total;
                                print("FILELEN:::: " + fileLength);
                                App.SetKey("dlength", "id" + id, fileLength);
                                String fileExtension = MimeTypeMap.GetFileExtensionFromUrl(url);
                                InputStream input = new BufferedInputStream(connection.InputStream);

                                //long skip = App.GetKey<long>(DOWNLOAD_KEY, id.ToString(), 0);

                                OutputStream output = new FileOutputStream(rFile, true);

                                outputStreams[id] = output;
                                inputStreams[id] = input;

                                if (isPaused.ContainsKey(id)) {
                                    //if (isPaused[id] == 2) {
                                    //    isPaused.Remove(id);
                                    return;
                                    //  }
                                }

                                isPaused[id] = 0;
                                activeIds.Add(id);

                                int cProgress()
                                {
                                    return (int)(total * 100 / fileLength);
                                }
                                progress = cProgress();

                                byte[] data = new byte[1024];
                                // skip;
                                int count;
                                int previousProgress = 0;
                                UpdateDloadNot(total == 0 ? "Download starting" : "Download resuming");

                                System.DateTime lastUpdateTime = System.DateTime.Now;
                                long lastTotal = total;

                                changedPause += UpdateFromId;

                                if (isStartProgress.ContainsKey(id)) {
                                    isStartProgress.Remove(id);
                                }

                                bool showDone = true;
                                while ((count = input.Read(data)) != -1) {
                                    total += count;
                                    bytesPerSec += count;

                                    output.Write(data, 0, count);
                                    progressDownloads[id] = total;
                                    progress = cProgress();


                                    if (isPaused[id] == 1) {
                                        print("PAUSEDOWNLOAD");
                                        UpdateProgress();
                                        while (isPaused[id] == 1) {
                                            Thread.Sleep(100);
                                        }
                                        if (isPaused[id] != 2) {
                                            UpdateProgress();
                                        }
                                    }
                                    if (isPaused[id] == 2) { // DELETE FILE
                                        print("DOWNLOAD STOPPED");
                                        ShowDone(false, "Download Stopped");
                                        //  Thread.Sleep(100);
                                        output.Flush();
                                        output.Close();
                                        input.Close();
                                        outputStreams.Remove(id);
                                        inputStreams.Remove(id);
                                        isPaused.Remove(id);
                                        // Thread.Sleep(100);
                                        rFile.Delete();
                                        App.RemoveKey(DOWNLOAD_KEY, id.ToString());
                                        App.RemoveKey(DOWNLOAD_KEY_INTENT, id.ToString());
                                        App.RemoveKey(App.hasDownloadedFolder, id.ToString());
                                        App.RemoveKey("dlength", "id" + id);
                                        App.RemoveKey("DownloadIds", id.ToString());
                                        changedPause -= UpdateFromId;
                                        activeIds.Remove(id);
                                        removeKeys = true;
                                        OnSomeDownloadFailed?.Invoke(null, EventArgs.Empty);
                                        Thread.Sleep(100);
                                        return;
                                    }

                                    if (DateTime.Now.Subtract(lastUpdateTime).TotalSeconds > UPDATE_TIME) {
                                        lastUpdateTime = DateTime.Now;
                                        long diff = total - lastTotal;
                                        //  UpdateDloadNot($"{ConvertBytesToAny(diff/UPDATE_TIME, 2,2)}MB/s | {progress}%");
                                        //{ConvertBytesToAny(diff / UPDATE_TIME, 2, 2)}MB/s | 
                                        if (progress >= 100) {
                                            print("DLOADPG DONE!");
                                            ShowDone(true);
                                        }
                                        else {
                                            UpdateProgress();
                                            // UpdateDloadNot(progress + "%");
                                        }
                                        bytesPerSec = 0;

                                        lastTotal = total;
                                    }

                                    if (progress >= 100 || progress > previousProgress) {
                                        // Only post progress event if we've made progress.
                                        previousProgress = progress;
                                        if (progress >= 100) {
                                            print("DLOADPG DONE!");
                                            ShowDone(true);
                                            showDone = false;
                                        }
                                        else {
                                            // UpdateProgress();
                                            // UpdateDloadNot(progress + "%");
                                        }
                                    }
                                }

                                if (showDone) {
                                    ShowDone(true);
                                }
                                output.Flush();
                                output.Close();
                                input.Close();
                                outputStreams.Remove(id);
                                inputStreams.Remove(id);
                                activeIds.Remove(id);
                            }
                        }
                        catch (Exception _ex) {
                            print("DOWNLOADURL: " + url);
                            print("DOWNLOAD FAILED BC: " + _ex);
                            if (mirror < mirrors.Count - 1 && progress < 2) { // HAVE MIRRORS LEFT
                                mirror++;
                                removeKeys = false;
                                resumeIntent = false;
                                rFile.Delete();
                            }
                            else {
                                ShowDone(false);
                            }
                        }
                        finally {
                            changedPause -= UpdateFromId;
                            isPaused.Remove(id);
                            if (isStartProgress.ContainsKey(id)) {
                                isStartProgress.Remove(id);
                            }
                            if (removeKeys) {
                                App.RemoveKey(DOWNLOAD_KEY, id.ToString());
                                App.RemoveKey(DOWNLOAD_KEY_INTENT, id.ToString());
                            }
                            else {
                                StartT();
                            }
                        }
                    });
                    t.Start();
                }
                StartT();


            }
            catch (Exception) {

                throw;
            }
        }
    }



    [Activity(Label = "CloudStream 2", Icon = "@drawable/bicon9", Theme = "@style/MainTheme.Splash", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation), IntentFilter(new[] { Intent.ActionView }, DataScheme = "cloudstreamforms", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static MainDroid mainDroid;
        public static MainActivity activity;

        public const int REQUEST_CODE = 42;
        public const string EXTRA_POSITION_OUT = "extra_position";
        public const string EXTRA_DURATION_OUT = "extra_duration";

        public static string lastId = "";

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (REQUEST_CODE == requestCode) {
                if (resultCode == Result.Ok) {
                    long pos = data.GetLongExtra(EXTRA_POSITION_OUT, -1);//Last position in media when player exited
                    if (pos > -1) {
                        App.SetViewPos(lastId, pos);
                        print("ViewHistoryTimePos SET TO: " + lastId + "|" + pos);
                    }
                    long dur = data.GetLongExtra(EXTRA_DURATION_OUT, -1);//	long	Total duration of the media
                    if (dur > -1) {
                        App.SetViewDur(lastId, dur);
                        print("ViewHistoryTimeDur SET TO: " + lastId + "|" + dur);
                    }
                }
            }
            App.ForceUpdateVideo?.Invoke(null, EventArgs.Empty);
            base.OnActivityResult(requestCode, resultCode, data);
        }

        protected override void OnNewIntent(Intent intent)
        {
            if (Settings.IS_TEST_BUILD) {
                return;
            }

            //App.ShowToast("ON NEW INTENT");
            //print("DA:::.2132131");
            if (intent.DataString != null) {
                print("INTENTNADADA:::" + intent.DataString);
                print("GOT NON NULL DATA");
                if (Intent.DataString != "" && Intent.DataString.Contains("cloudstreamforms:")) {
                    print("INTENTDATA::::" + Intent.DataString);
                    MainPage.PushPageFromUrlAndName(Intent.DataString);
                }
            }
            Bundle extras = intent.Extras;
            if (extras != null) {
                if (extras.ContainsKey("data")) {
                    // extract the extra-data in the Notification
                    string msg = extras.GetString("data");

                    if (msg == "openchrome") {
                        MovieResult.OpenChrome();
                    }

                    print("DADADA:D:A:D:AD:A:D:A:D:A" + msg);
                }
            }

            base.OnNewIntent(intent);
        }

        public static int PublicNot;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            print("ON CREATED:::::!!!!!!!!!");

            SetTheme(Resource.Style.MainTheme_NonSplash);

            PublicNot = Resource.Drawable.bicon;

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            System.AppDomain.CurrentDomain.UnhandledException += MainPage.UnhandledExceptionTrapper;


            string data = Intent?.Data?.EncodedAuthority;

            try {
                MainPage.intentData = data;
            }
            catch (Exception) { }

            // int intHeight = (int)(Resources.DisplayMetrics.HeightPixels / Resources.DisplayMetrics.Density);
            //int intWidth = (int)(Resources.DisplayMetrics.WidthPixels / Resources.DisplayMetrics.Density);


            // ======================================= INIT =======================================

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);
            Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
            UserDialogs.Init(this);
            LibVLCSharpFormsRenderer.Init();
            XamEffects.Droid.Effects.Init();

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LocalNotificationsImplementation.NotificationIconId = PublicNot;
            MainDroid.NotificationIconId = PublicNot;

            trustEveryone();
            LoadApplication(new App());
            if (Settings.IS_TEST_BUILD) {
                platformDep = new NullPlatfrom();
                return;
            }
            try {
                activity = this;

                mainDroid = new MainDroid();
                mainDroid.Awake();

                //Typeface.CreateFromAsset(Application.Context.Assets, "Times-New-Roman.ttf");


                if (Intent.DataString != null) {
                    print("GOT NON NULL DATA");
                    if (Intent.DataString != "") {
                        print("INTENTDATA::::" + Intent.DataString);
                        MainPage.PushPageFromUrlAndName(Intent.DataString);
                    }
                }
                RequestPermission(this);

                //App.ShowToast("ON CREATE");

                //mainDroid.Test();
                /*
                MessagingCenter.Subscribe<VideoPage>(this, "allowLandScapePortrait", sender =>
                {
                    RequestedOrientation = ScreenOrientation.Unspecified;
                });
                MessagingCenter.Subscribe<VideoPage>(this, "preventLandScape", sender =>
                {
                    RequestedOrientation = ScreenOrientation.Portrait;
                });*/
                // Window.DecorView.SetBackgroundResource(Resource.Drawable.splash_background_remove);//Resources.GetDrawable(Resource.Drawable.splash_background_remove);


                /*
                var alarm = Application.Context.GetSystemService(Context.AlarmService) as AlarmManager;
                var context = ApplicationContext;
                var _testIntent = new Intent(context, typeof(AlertReceiver));

                var pending = PendingIntent.GetBroadcast(context, 1337, _testIntent, 0);

                alarm.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, MainDroid.CurrentTimeMillis(DateTime.UtcNow.AddSeconds(5)), pending);*/

                MainChrome.OnDisconnected += (o, e) => {
                    MainDroid.CancelChromecast();
                };

                MainChrome.OnNotificationChanged += (o, e) => {
                    try {
                        print("CHROMECAST CHANGED::: ");
                        print("ID=====================" + e.isCasting + "|" + e.isPlaying + "|" + e.isPaused);
                        if (!e.isCasting) {// || !e.isPlaying) {
                            MainDroid.CancelChromecast();
                        }
                        else {
                            MainDroid.UpdateChromecastNotification(e.title, e.body, e.isPaused, e.posterUrl);
                        }
                    }
                    catch (Exception _ex) {
                        print("EX NOT CHANGED::: " + _ex);
                    }
                };

                ResumeIntentData();
                StartService(new Intent(BaseContext, typeof(OnKilledService)));

                Window.SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
            }
            catch (Exception _ex) {
                error(_ex);
            }

            // TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
            //  Android.Renderscripts.ta
            // var bar = new Xamarin.Forms.Platform.Android.TabbedRenderer();//.Platform.Android.

            //ShowBlackToast("Yeet", 3);
            // DownloadHandle.ResumeIntents();
            //   ShowLocalNot(new LocalNot() { mediaStyle = false, title = "yeet", data = "", progress = -1, showWhen = false, autoCancel = true, onGoing = false, id = 1234, smallIcon = Resource.Drawable.bicon, body = "Download ddddd" }, Application.Context);

            // ShowLocalNot(new LocalNot() { mediaStyle = false, title = "yeet", autoCancel = true, onGoing = false, id = 123545, smallIcon = Resource.Drawable.bicon, body = "Download Failed!",showWhen=false }); // ((e.Cancelled || e.Error != null) ? "Download Failed!"
        }



        private static void trustEveryone()
        {
            /*
            HttpsURLConnection.DefaultHostnameVerifier =
                    new Org.Apache.Http.Conn.Ssl.AllowAllHostnameVerifier();
            */
        }

        async void ResumeIntentData()
        {
            await Task.Delay(1000);
            print("STARTINTENT");
            DownloadHandle.ResumeIntents();
        }

        /*
        private static void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            var newExc = new Exception("TaskSchedulerOnUnobservedTaskException", unobservedTaskExceptionEventArgs.Exception);
            App.ShowNotification("Error", newExc.Message);
        }*/

        public void Killed()
        {
            // App.ShowToast("KILLED");
            //ShowNotification("finish", "Yeet");
#if DEBUG
            EndDebugging();
#endif
            MainDroid.CancelChromecast(); // TO REMOVE IT, CANT INTERACT WITHOUT THE CORE
            DownloadHandle.OnKilled();
            App.OnAppKilled?.Invoke(null, EventArgs.Empty);
        }

        protected override void OnDestroy()
        {
            Killed();
            base.OnDestroy();
        }

        protected override void OnStop()
        {
            App.OnAppNotInForground?.Invoke(null, EventArgs.Empty);
            base.OnStop();
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public static int REQUEST_START = 112;
        private static void RequestPermission(Activity context)
        {
            try {
                List<string> requests = new List<string>() {
                Manifest.Permission.WriteExternalStorage, Manifest.Permission.RequestInstallPackages,Manifest.Permission.InstallPackages,Manifest.Permission.WriteSettings, //Manifest.Permission.Bluetooth
            };

                for (int i = 0; i < requests.Count; i++) {
                    bool hasPermission = (ContextCompat.CheckSelfPermission(context, requests[i]) == Permission.Granted);
                    if (!hasPermission) {
                        ActivityCompat.RequestPermissions(context,
                           new string[] { requests[i] },
                         REQUEST_START + i);
                    }
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }
    }


    public static class MainHelper
    {
        public static AlarmManager GetAlarmManager()
        {
            var alarmManager = Application.Context.GetSystemService(Context.AlarmService) as AlarmManager;
            return alarmManager;
        }

        public static string GetPath(bool mainPath, string extraPath)
        {
            return (mainPath ? (Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads) : (Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads + "/Extra")) + extraPath;
        }

        public static void ShowBlackToast(string msg, double duration)
        {
            Device.BeginInvokeOnMainThread(() => {
                ToastLength toastLength = ToastLength.Short;
                if (duration >= 3) {
                    toastLength = ToastLength.Long;
                }
                Toast toast = Toast.MakeText(Application.Context, Html.FromHtml("<font color='#ffffff' >" + msg + "</font>"), ToastLength.Short);
                toast.SetGravity(GravityFlags.CenterHorizontal | GravityFlags.Top, 0, 0);
                var view = toast.View;
                //Gets the actual oval background of the Toast then sets the colour filter
                view.SetBackgroundColor(new Android.Graphics.Color(0, 0, 0, 10));//.SetColorFilter(Resource.dtra, PorterDuff.Mode.SRC_IN);
                toast.Show();
            });
        }
    }


    public class BluetoothServiceListener : Java.Lang.Object
        , IBluetoothProfileServiceListener
    {
        public BluetoothHeadset btHeadset;
        public void Dispose()
        {
            print("TRYIGNT O DISPOSE");
            //  throw new NotImplementedException();
        }

        public void OnServiceConnected(ProfileType profile, IBluetoothProfile proxy)
        {
            print("ON CONNECTED");
            if (profile == ProfileType.Headset) {
                btHeadset = (BluetoothHeadset)proxy;
            }
        }

        public void OnServiceDisconnected(ProfileType profile)
        {
            print("ON DISSSS:S::S:S:");
            if (profile == ProfileType.Headset) {
                btHeadset = null;
            }
        }
    }

    public class MainDroid : App.IPlatformDep
    {
        /*
        BluetoothAdapter bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
        BluetoothServiceListener bluetoothServiceListener = new BluetoothServiceListener();

        public BluetoothDeviceID[] GetBluetoothDevices()
        {
            if (bluetoothServiceListener == null) return null;
            if (bluetoothServiceListener.btHeadset == null) return null;
            if (bluetoothServiceListener.btHeadset.ConnectedDevices == null) return null;
            if (bluetoothServiceListener.btHeadset.ConnectedDevices.Count == 0) return null;
            return bluetoothServiceListener.btHeadset.ConnectedDevices.Select(t => new BluetoothDeviceID() { name = t.Name, id = t.Address }).ToArray();
        }
        public void SearchBluetoothDevices()
        {
            // true == headset connected && connected headset is support hands free

            var state = bluetoothAdapter.GetProfileConnectionState(ProfileType.Headset);
            if (state != ProfileState.Connected)
                return;
            try {
                bluetoothAdapter.GetProfileProxy(MainActivity.activity.ApplicationContext, bluetoothServiceListener, ProfileType.Headset);
            }
            catch (Exception e) {
                print("MAIN EX IN >>>>>><" + nameof(SearchBluetoothDevices) + "<<<<");
            }
        }*/


        public void UpdateDownload(int id, int state)
        {
            if (state == -1) {
                if (DownloadHandle.isPaused.ContainsKey(id)) {
                    DownloadHandle.isPaused.Remove(id);
                }
            }
            else {
                try {
                    DownloadHandle.isPaused[id] = state;
                }
                catch (Exception _ex) {
                    error(_ex);
                }
            }
        }
        /**
  * The audio latency has not been estimated yet
  */
        private static long AUDIO_LATENCY_NOT_ESTIMATED = long.MinValue + 1;

        /**
         * The audio latency default value if we cannot estimate it
         */
        private static long DEFAULT_AUDIO_LATENCY = 100L * 1000L * 1000L; // 100ms

        private static long _framesToNanoSeconds(long frames)
        {
            return frames * 1000000000L / 16000;
        }
        private static long nanoTime()
        {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

        // Source: https://stackoverflow.com/a/52559996/497368
        private long getDelay()
        {
            long estimatedAudioLatency = AUDIO_LATENCY_NOT_ESTIMATED;
            long audioFramesWritten = 0;
            var outputBufferSize = AudioTrack.GetMinBufferSize(16000, ChannelOut.Stereo, Android.Media.Encoding.Pcm16bit);

            AudioTrack track = new AudioTrack(Android.Media.Stream.Music, 16000, ChannelOut.Mono, Android.Media.Encoding.Pcm16bit, outputBufferSize, AudioTrackMode.Stream);//AudioManager.USE_DEFAULT_STREAM_TYPE, 16000, AudioFormat.CHANNEL_OUT_MONO, AudioFormat.ENCODING_PCM_16BIT, outputBufferSize, AudioTrack.MODE_STREAM);

            // First method. SDK >= 19.
            if ((int)Build.VERSION.SdkInt >= 19 && track != null) {

                AudioTimestamp audioTimestamp = new AudioTimestamp();
                if (track.GetTimestamp(audioTimestamp)) {

                    // Calculate the number of frames between our known frame and the write index
                    long frameIndexDelta = audioFramesWritten - audioTimestamp.FramePosition;

                    // Calculate the time which the next frame will be presented
                    long frameTimeDelta = _framesToNanoSeconds(frameIndexDelta);
                    long nextFramePresentationTime = audioTimestamp.NanoTime + frameTimeDelta;

                    // Assume that the next frame will be written at the current time
                    long nextFrameWriteTime = nanoTime();

                    // Calculate the latency
                    estimatedAudioLatency = nextFramePresentationTime - nextFrameWriteTime;
                }
            }

            // Second method. SDK >= 18.
            if (estimatedAudioLatency == AUDIO_LATENCY_NOT_ESTIMATED && (int)Build.VERSION.SdkInt >= 18) {
                System.Reflection.MethodInfo getLatencyMethod;
                try {
                    getLatencyMethod = typeof(AudioTrack).GetMethod("getLatency");
                    estimatedAudioLatency = (int)getLatencyMethod.Invoke(track, (Object[])null) * 1000000L;
                }
                catch (Exception ignored) {
                    print("IGNORED:::2222::: " + ignored);
                }
            }

            // If no method has successfully gave us a value, let's try a third method
            if (estimatedAudioLatency == AUDIO_LATENCY_NOT_ESTIMATED) {
                AudioManager audioManager = Application.Context.GetSystemService(Context.AudioService) as AudioManager;
                try {
                    System.Reflection.MethodInfo getOutputLatencyMethod = typeof(AudioManager).GetMethod("getOutputLatency");
                    estimatedAudioLatency = (int)getOutputLatencyMethod.Invoke(audioManager, new object[] { AudioContentType.Music }) * 1000000L;
                }
                catch (Exception ignored) {
                    print("IGNORED::: " + ignored);
                }
            }

            // No method gave us a value. Let's use a default value. Better than nothing.
            if (estimatedAudioLatency == AUDIO_LATENCY_NOT_ESTIMATED) {
                print("DEF LATENCY");
                estimatedAudioLatency = DEFAULT_AUDIO_LATENCY;
            }

            return estimatedAudioLatency;
        }

        public DownloadProgressInfo GetDownloadProgressInfo(int id, string fileUrl)
        {
            //  return new DownloadProgressInfo() { bytesDownloaded = 10, totalBytes = 100, state = DownloadState.Downloading };
            //  Stopwatch s = new Stopwatch();
            try {
                DownloadProgressInfo progressInfo = new DownloadProgressInfo();

                bool downloadingOrPaused = DownloadHandle.isPaused.ContainsKey(id);

                var file = new Java.IO.File(fileUrl);

                //  s.Start();
                bool exists = file.Exists();

                if (downloadingOrPaused) {
                    int paused = DownloadHandle.isPaused[id];
                    progressInfo.state = paused == 1 ? DownloadState.Paused : DownloadState.Downloading;
                }
                else {
                    //file.Length()
                    progressInfo.state = exists ? DownloadState.Downloaded : DownloadState.NotDownloaded;
                }

                if (progressInfo.bytesDownloaded < progressInfo.totalBytes - 10 && progressInfo.state == DownloadState.Downloaded) {
                    progressInfo.state = DownloadState.NotDownloaded;
                }
                print("CONTAINS ::>>" + DownloadHandle.isStartProgress.ContainsKey(id));
                progressInfo.bytesDownloaded = (exists ? (file.Length()) : 0) + (DownloadHandle.isStartProgress.ContainsKey(id) ? 1 : 0);

                //                        App.SetKey("dlength", "id" + id, fileLength);

                progressInfo.totalBytes = exists ? App.GetKey<int>("dlength", "id" + id, 0) : 0;
                print("STATE:::::==" + progressInfo.totalBytes + "|" + progressInfo.bytesDownloaded);

                //  s.Stop();
                //  print("STIME: " + s.ElapsedMilliseconds);

                if (!exists) {
                    return progressInfo;
                }

                if (progressInfo.bytesDownloaded >= progressInfo.totalBytes - 10) {
                    progressInfo.state = DownloadState.Downloaded;
                }
                else if (progressInfo.state == DownloadState.Downloaded) {
                    progressInfo.state = DownloadState.NotDownloaded;
                }

                if (progressInfo.bytesDownloaded < 0 || progressInfo.totalBytes < 0) {
                    progressInfo.state = DownloadState.NotDownloaded;
                    progressInfo.totalBytes = 0;
                }

                return progressInfo;


            }
            catch (Exception _ex) {
                error(_ex);
                return new DownloadProgressInfo();
            }
        }

        public void SetBrightness(double opacity)
        {
            Android.Provider.Settings.System.PutInt(MainActivity.activity.ContentResolver, Android.Provider.Settings.System.ScreenBrightness, (int)(opacity * 255));
        }

        public double GetBrightness()
        {
            return Android.Provider.Settings.System.GetInt(MainActivity.activity.ContentResolver, Android.Provider.Settings.System.ScreenBrightness) / 255.0;
        }


        // FROM https://github.com/edsnider/localnotificationsplugin/blob/master/src/Plugin.LocalNotifications.Android/LocalNotificationsImplementation.cs


        /// <summary>
        /// Get or Set Resource Icon to display
        /// </summary>
        public static int NotificationIconId { get; set; }
        static string _packageName => Application.Context.PackageName;



        int LocalNotificationIconId {
            get {
                if (NotificationIconId != 0) {
                    return NotificationIconId;
                }
                else {
                    return Resource.Drawable.plugin_lc_smallicon;
                }
            }
        }






        public static void CancelFutureNotification(int id)
        {
            var context = MainActivity.activity.ApplicationContext;

            var alarmManager = GetAlarmManager();
            var _resultIntent = new Intent(context, typeof(AlertReceiver));
            var pending = PendingIntent.GetBroadcast(context, id,
                    _resultIntent,
                   PendingIntentFlags.CancelCurrent
                    );
            alarmManager.Cancel(pending);
        }



        public async void ShowNotIntentAsync(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "")
        {

            var localNot = new LocalNot() { title = title, body = body, id = id, data = titleId == "-1" ? ("cloudstreamforms:" + titleId + "Name=" + titleName + "=EndAll") : null, bigIcon = bigIconUrl, autoCancel = true, mediaStyle = true, notificationImportance = (int)NotificationImportance.Default, showWhen = true, when = time, smallIcon = PublicNot };


            if (time == null) {
                ShowLocalNot(localNot);
            }
            else {
                print("SHOWS NOTIFICATION== " + body + " in " + ((DateTime)time).Subtract(DateTime.UtcNow).TotalSeconds);
                var context = MainActivity.activity.ApplicationContext;

                var _resultIntent = new Intent(context, typeof(AlertReceiver));
                //  _resultIntent.PutExtra("data", App.ConvertToString(localNot));


                // IF NOT BITSERALIZER IS AVALIBLE
                foreach (var prop in typeof(LocalNot).GetFields()) {
                    if (prop.FieldType == typeof(int)) {
                        _resultIntent.PutExtra(prop.Name, (int)prop.GetValue(localNot));//(int)prop.GetValue(localNot));
                    }
                    if (prop.FieldType == typeof(float)) {
                        _resultIntent.PutExtra(prop.Name, (float)prop.GetValue(localNot));//(int)prop.GetValue(localNot));
                    }
                    if (prop.FieldType == typeof(bool)) {
                        _resultIntent.PutExtra(prop.Name, (bool)prop.GetValue(localNot));//(int)prop.GetValue(localNot));
                    }
                    if (prop.FieldType == typeof(string)) {
                        _resultIntent.PutExtra(prop.Name, (string)prop.GetValue(localNot));//(int)prop.GetValue(localNot));
                    }
                    if (prop.FieldType == typeof(DateTime)) {
                        _resultIntent.PutExtra(prop.Name, ((DateTime)prop.GetValue(localNot)).ToLongDateString());//(int)prop.GetValue(localNot));
                    }
                    if (prop.FieldType.IsEnum) {
                        _resultIntent.PutExtra(prop.Name, (int)prop.GetValue(localNot));//(int)prop.GetValue(localNot));
                    }
                }


                _resultIntent.PutExtra("title", localNot.title);

                var pending = PendingIntent.GetBroadcast(context, id,
                     _resultIntent,
                    PendingIntentFlags.CancelCurrent
                     );




                var triggerTime = CurrentTimeMillis(((DateTime)time).Add(DateTime.UtcNow.Subtract(DateTime.Now)));// NotifyTimeInMilliseconds((DateTime)time);
                var alarmManager = GetAlarmManager();

                alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerTime, pending);

                /*
                var alarm = Application.Context.GetSystemService(Context.AlarmService) as AlarmManager;
                var _testIntent = new Intent(context, typeof(AlertReceiver));

                var pending = PendingIntent.GetBroadcast(context, 1337, _testIntent, 0);

                alarm.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, MainDroid.CurrentTimeMillis(DateTime.UtcNow.AddSeconds(5)), pending);*/

            }

            /*var builder = new Notification.Builder(Application.Context);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetAutoCancel(true);
            builder.SetShowWhen(true);
            builder.SetSmallIcon(LocalNotificationIconId);


            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                var channelId = $"{_packageName}.general";
                var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);

                _manager.CreateNotificationChannel(channel);

                builder.SetChannelId(channelId);

                var context = MainActivity.activity.ApplicationContext;
                if (bigIconUrl != "") {
                    print("BIGBIG::" + bigIconUrl);
                    var bitmap = await GetImageBitmapFromUrl(bigIconUrl);
                    if (bitmap != null) {
                        builder.SetLargeIcon(bitmap);
                        builder.SetStyle(new Notification.MediaStyle()); // NICER IMAGE
                    }
                }
            }


            var resultIntent = GetLauncherActivity();
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            //"cloudstreamforms:tt0371746Name=Iron man=EndAll"
            string data = $"cloudstreamforms:{titleId}Name={titleName}=EndAll";

            var _data = Android.Net.Uri.Parse(data);
            resultIntent.SetData(_data);

            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(Application.Context);
            stackBuilder.AddNextIntent(resultIntent);
            var resultPendingIntent =
                stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);


            builder.SetContentIntent(resultPendingIntent);




            if (time != null) {


                //var serializedNotification = SerializeNotification(localNotification);
                //intent.PutExtra(ScheduledAlarmHandler.LocalNotificationKey, serializedNotification);
                var context = MainActivity.activity.ApplicationContext;

                var _resultIntent = new Intent(context, typeof(NotifyAtTime));
                _resultIntent.PutExtra("title", builder.)

                var pending = PendingIntent.GetService(context, 0,
                 _resultIntent,
                //PendingIntentFlags.CancelCurrent
                PendingIntentFlags.UpdateCurrent
                 );




                // var pendingIntent = PendingIntent.GetService()//GetBroadcast(Application.Context, 0, intent, PendingIntentFlags.CancelCurrent);
                var triggerTime = NotifyTimeInMilliseconds((DateTime)time);
                var alarmManager = GetAlarmManager();

                alarmManager.Set(AlarmType.RtcWakeup, triggerTime, pending);

                // builder.SetWhen(CurrentTimeMillis((DateTime)time));
            }
            else {

                _manager.Notify(id, builder.Build());
            }*/
        }


        public void ShowNotIntent(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "")
        {
            ShowNotIntentAsync(title, body, id, titleId, titleName, time, bigIconUrl);
        }

        public const int CHROME_CAST_NOTIFICATION_ID = 1337;

        public static void CancelChromecast()
        {
            _manager.Cancel(CHROME_CAST_NOTIFICATION_ID);
        }

        static MediaSession mediaSession = new MediaSession(Application.Context, "Chromecast");

        public static async void UpdateChromecastNotification(string title, string body, bool isPaused, string poster)
        {
            try {
                var builder = new Notification.Builder(Application.Context);
                builder.SetContentTitle(title);
                builder.SetContentText(body);
                builder.SetAutoCancel(false);

                builder.SetSmallIcon(Resource.Drawable.biconWhite2);//LocalNotificationIconId);
                builder.SetOngoing(true);


                var context = MainActivity.activity.ApplicationContext;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                    var channelId = $"{_packageName}.general";
                    var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);

                    _manager.CreateNotificationChannel(channel);

                    builder.SetChannelId(channelId);
                    //https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_UX182_CR0,0,182,268_AL_.jpg
                    var bitmap = await GetImageBitmapFromUrl(poster);//"https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_UX182_CR0,0,182,268_AL_.jpg");
                    if (bitmap != null) {
                        builder.SetLargeIcon(bitmap);
                    }

                    builder.SetStyle(new Notification.MediaStyle().SetMediaSession(mediaSession.SessionToken).SetShowActionsInCompactView(0, 1, 2)); // NICER IMAGE

                    List<string> actionNames = new List<string>() { "-30s", isPaused ? "Play" : "Pause", "+30s", "Stop" };
                    List<int> sprites = new List<int>() { Resource.Drawable.netflixGoBack128, isPaused ? Resource.Drawable.netflixPlay128v2 : Resource.Drawable.netflixPause128v2, Resource.Drawable.netflixGoForward128, Resource.Drawable.netflixStop128v2 };
                    List<string> actionIntent = new List<string>() { "goback", isPaused ? "play" : "pause", "goforward", "stop" }; // next

                    List<Notification.Action> actions = new List<Notification.Action>();

                    for (int i = 0; i < sprites.Count; i++) {
                        var _resultIntent = new Intent(context, typeof(ChromeCastIntentService));
                        _resultIntent.PutExtra("data", actionIntent[i]);
                        var _pending = PendingIntent.GetService(context, 2337 + i,
                         _resultIntent,
                        PendingIntentFlags.UpdateCurrent
                         );

                        actions.Add(new Notification.Action(sprites[i], actionNames[i], _pending));
                    }
                    builder.SetActions(actions.ToArray());
                }

                builder.SetContentIntent(GetCurrentPending("openchrome"));
                try {
                    _manager.Notify(CHROME_CAST_NOTIFICATION_ID, builder.Build());

                }
                catch (Exception _ex) {
                    print("EX NOTTIFY;; " + _ex);
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }


        /// <summary>
        /// Show a local notification
        /// </summary>
        /// <param name="title">Title of the notification</param>
        /// <param name="body">Body or description of the notification</param>
        /// <param name="id">Id of the notification</param>
        public async void Show(string title, string body, int id = 0)
        {
            return;
            var builder = new Notification.Builder(Application.Context);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetAutoCancel(true);


            builder.SetSmallIcon(LocalNotificationIconId);



            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                var channelId = $"{_packageName}.general";
                var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);

                _manager.CreateNotificationChannel(channel);

                builder.SetChannelId(channelId);
                //https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_UX182_CR0,0,182,268_AL_.jpg
                var bitmap = await GetImageBitmapFromUrl("https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_UX182_CR0,0,182,268_AL_.jpg");
                if (bitmap != null) {
                    builder.SetLargeIcon(bitmap);
                }
                var context = MainActivity.activity.ApplicationContext;


                MediaSession mediaSession = new MediaSession(context, "tag");

                builder.SetStyle(new Notification.MediaStyle().SetMediaSession(mediaSession.SessionToken).SetShowActionsInCompactView(0, 1, 2)); // NICER IMAGE


                // mediaSession.SetPlaybackState(PlaybackState.)

                bool isPaused = true;

                List<string> actionNames = new List<string>() { "-30s", isPaused ? "Play" : "Pause", "+30s", "Stop" };
                List<int> sprites = new List<int>() { Resource.Drawable.netflixGoBack128, isPaused ? Resource.Drawable.netflixPlay128v2 : Resource.Drawable.netflixPause128v2, Resource.Drawable.netflixGoForward128, Resource.Drawable.netflixStop128v2 };
                List<string> actionIntent = new List<string>() { "goback", isPaused ? "play" : "pause", "goforward", "stop" }; // next

                List<Notification.Action> actions = new List<Notification.Action>();

                for (int i = 0; i < sprites.Count; i++) {
                    var _resultIntent = new Intent(context, typeof(DemoIntentService));
                    // _resultIntent.SetAction("com.CloudStreamForms.CloudStreamForms.pause");
                    // _resultIntent.AddFlags(ActivityFlags.IncludeStoppedPackages);
                    _resultIntent.PutExtra("data", actionIntent[i]);
                    // _resultIntent.AddFlags(ActivityFlags.ReceiverForeground);

                    //PendingIntent.GetActivity
                    //GetBroadcast
                    //GetService
                    var pending = PendingIntent.GetService(context, 1337 + i,
                     _resultIntent,
                    //PendingIntentFlags.CancelCurrent
                    PendingIntentFlags.UpdateCurrent
                     );

                    actions.Add(new Notification.Action(sprites[i], actionNames[i], pending));
                }
                builder.SetActions(actions.ToArray());

                //builder.SetColorized(true);
                //  builder.SetColor(Resource.Color.colorPrimary);
                /*
                var context = MainActivity.activity.ApplicationContext;
                var _resultIntent = new Intent(context, typeof(MainActivity));
                //_resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

                var da = Android.Net.Uri.Parse("cloudstreamforms:tt0371746Name=Iron man=EndAll");
                _resultIntent.SetData(da);
                _resultIntent.PutExtra("data", da);
                _resultIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

                print("PDATA::::" + _resultIntent.DataString);
                var pending = PendingIntent.GetActivity(context, 0,
                    _resultIntent,
                   //PendingIntentFlags.CancelCurrent
                   PendingIntentFlags.UpdateCurrent
                    );

                // SET AFTER THO 
                //RemoteViews remoteViews = new RemoteViews(Application.Context.PackageName, Resource.Xml.PausePlay);
                // remoteViews.SetImageViewResource(R.id.notifAddDriverIcon, R.drawable.my_trips_new);
                // builder.SetCustomContentView(remoteViews);

                builder.SetShowWhen(false);
                builder.SetContentIntent(pending);
                builder.SetFullScreenIntent(pending, true);
                */

                /*
                Intent notificationIntent = new Intent(context, typeof(MainActivity));
                notificationIntent.PutExtra("NotificationMessage", "YEET");
                notificationIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
                PendingIntent pendingNotificationIntent = PendingIntent.GetActivity(context, 1337, notificationIntent,PendingIntentFlags.UpdateCurrent);

                notification.setLatestEventInfo(getApplicationContext(), notificationTitle, notificationMessage, pendingNotificationIntent);*/
                //  builder.SetProgress(100, 51, false); // PROGRESSBAR
                //  builder.SetLargeIcon(Android.Graphics.Drawables.Icon.CreateWithResource(context, Resource.Drawable.bicon)); // POSTER
                // builder.SetActions(new Notification.Action(Resource.Drawable.design_bottom_navigation_item_background, "Hello", pending)); // IDK TEXT PRESS
            }

            var resultIntent = GetLauncherActivity();
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

            var _da = Android.Net.Uri.Parse("cloudstreamforms:tt0371746Name=Iron man=EndAll");
            resultIntent.SetData(_da);

            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(Application.Context);
            stackBuilder.AddNextIntent(resultIntent);
            var resultPendingIntent =
                stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);


            builder.SetContentIntent(resultPendingIntent);

            _manager.Notify(id, builder.Build());
        }
        public static Intent GetLauncherActivity(string pgName = null)
        {
            var packageName = pgName ?? Application.Context.PackageName;
            return Application.Context.PackageManager.GetLaunchIntentForPackage(packageName);
        }

        private long NotifyTimeInMilliseconds(DateTime notifyTime)
        {
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(notifyTime);
            var epochDifference = (new DateTime(1970, 1, 1) - DateTime.MinValue).TotalSeconds;

            var utcAlarmTimeInMillis = utcTime.AddSeconds(-epochDifference).Ticks / 10000;
            return utcAlarmTimeInMillis;
        }

        static bool hidden = false;
        // static int baseShow = 0;

        public void UpdateBackground(int color)
        {
            print("SET NON TRANSPARENT!");
            try {
                Window window = MainActivity.activity.Window;

                int uiOptions = (int)window.DecorView.SystemUiVisibility;
                uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
                window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
                // window.NavigationBarDividerColor
                window.SetNavigationBarColor(Android.Graphics.Color.Rgb(color, color, color));

            }
            catch (Exception _ex) {
                error(_ex);
            }
            /*
            Window window = MainActivity.activity.Window;
            int color = Settings.BlackColor - 5;
            if(color > 255) { color = 255; }
            if(color < 0) { color = 0; }
            window.SetNavigationBarColor(Android.Graphics.Color.Rgb(color, color, color));*/
        }

        public void UpdateBackground()
        {
            try {
                Window window = MainActivity.activity.Window;
                print("SET TRANSPARENT!");

                int uiOptions = (int)window.DecorView.SystemUiVisibility;
                uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
                window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;

                window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public void UpdateStatusBar()
        {
            // Window window = MainActivity.activity.Window;
            try {
                ToggleFullscreen(!Settings.HasStatusBar);
                if (Settings.HasStatusBar) {
                    ShowStatusBar();
                }
                else {
                    HideStatusBar();
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }

            /*
            if (!Settings.HasStatusBar) {
                print("REMOVE STATUS BAR::::");
                window.AddFlags(WindowManagerFlags.Fullscreen); // REMOVES STATUS BAR
            }
            else {
                window.ClearFlags(WindowManagerFlags.Fullscreen); // ADD STATUS BAR
            }*/
        }

        public void ToggleFullscreen(bool fullscreen)
        {
            try {
                Window window = MainActivity.activity.Window;

                if (fullscreen) {
                    window.AddFlags(WindowManagerFlags.Fullscreen); // REMOVES STATUS BAR
                }
                else {
                    window.ClearFlags(WindowManagerFlags.Fullscreen);
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }

        }

        public void ToggleRealFullScreen(bool fullscreen)
        {
            try {
                Window window = MainActivity.activity.Window;
                print("TOGGLE" + fullscreen);

                var uiOptions = (int)window.DecorView.SystemUiVisibility;
                // uiOptions |= (int)SystemUiFlags.LowProfile;
                // uiOptions |= (int)SystemUiFlags.Fullscreen;


                //var attrs = window.Attributes;

                if (fullscreen) {
                    uiOptions |= (int)SystemUiFlags.HideNavigation;
                    //uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
                    uiOptions |= (int)SystemUiFlags.Fullscreen;
                    //uiOptions |= (int)SystemUiFlags.LayoutStable;
                    //uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
                    //uiOptions |= (int)SystemUiFlags.LayoutFullscreen;
                    //    uiOptions |= (int)SystemUiFlags.LowProfile;

                    window.AddFlags(WindowManagerFlags.TurnScreenOn);
                    window.AddFlags(WindowManagerFlags.KeepScreenOn);
                    window.AddFlags(WindowManagerFlags.Fullscreen); // REMOVES STATUS BAR

                    //   attrs.Flags |= Android.Views.WindowManagerFlags.Fullscreen;

                    //   window.AddFlags(WindowManagerFlags.Fullscreen);
                    // window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
                }
                else {
                    uiOptions &= ~(int)SystemUiFlags.HideNavigation;
                    //     uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
                    uiOptions &= ~(int)SystemUiFlags.Fullscreen;
                    //   uiOptions &= ~(int)SystemUiFlags.LayoutStable;
                    //   uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;
                    //  uiOptions &= ~(int)SystemUiFlags.LayoutFullscreen;
                    //   uiOptions &= ~(int)SystemUiFlags.LowProfile;

                    window.ClearFlags(WindowManagerFlags.TurnScreenOn);
                    window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                    window.ClearFlags(WindowManagerFlags.Fullscreen);

                    // window.AddFlags(WindowManagerFlags.ForceNotFullscreen);
                    // window.ClearFlags(WindowManagerFlags.Fullscreen);

                    //  attrs.Flags &= ~Android.Views.WindowManagerFlags.Fullscreen;
                }

                //   window.Attributes = attrs;

                window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;

                if (!fullscreen) {
                    UpdateStatusBar();
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public void LandscapeOrientation()
        {
            try {
                MainActivity.activity.RequestedOrientation = ScreenOrientation.Landscape;
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public void NormalOrientation()
        {
            try {
                MainActivity.activity.RequestedOrientation = ScreenOrientation.Unspecified;
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public void ShowStatusBar()
        {
            //  if (!hidden) return;
            try {
                Window window = MainActivity.activity.Window;
                // window.ClearFlags(WindowManagerFlags.TurnScreenOn);
                //window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                //ToggleFullscreen(!Settings.HasStatusBar);

                //if (Settings.HasStatusBar) {
                window.ClearFlags(WindowManagerFlags.Fullscreen);
                //}

                int uiOptions = (int)window.DecorView.SystemUiVisibility;
                //  baseShow = uiOptions;

                //  uiOptions &= ~(int)SystemUiFlags.LowProfile;
                uiOptions &= ~(int)SystemUiFlags.Fullscreen;
                //   uiOptions &= ~(int)SystemUiFlags.HideNavigation;
                uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;

                window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public void HideStatusBar()
        {
            try {
                //if (hidden) return;
                hidden = true;

                Window window = MainActivity.activity.Window;
                //  window.AddFlags(WindowManagerFlags.TurnScreenOn);
                // window.AddFlags(WindowManagerFlags.KeepScreenOn);

                if (Settings.HasStatusBar) {
                    window.AddFlags(WindowManagerFlags.Fullscreen);
                }

                int uiOptions = (int)window.DecorView.SystemUiVisibility;
                //  baseShow = uiOptions;

                // uiOptions |= (int)SystemUiFlags.LowProfile;
                uiOptions |= (int)SystemUiFlags.Fullscreen;
                // uiOptions |= (int)SystemUiFlags.HideNavigation;
                uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

                window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
                /*
                var activity = (Activity)Forms.Context;
                var window = activity.Window;
                var attrs = window.Attributes;
                attrs.Flags |= Android.Views.WindowManagerFlags.Fullscreen;
                window.Attributes = attrs;

                window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
                window.AddFlags(WindowManagerFlags.Fullscreen);

                var decorView = window.DecorView;

                var uiOptions =
                    (int)Android.Views.SystemUiFlags.LayoutStable |
                    (int)Android.Views.SystemUiFlags.LayoutHideNavigation |
                    (int)Android.Views.SystemUiFlags.LayoutFullscreen |
                    (int)Android.Views.SystemUiFlags.HideNavigation |
                    (int)Android.Views.SystemUiFlags.Fullscreen |
                    (int)Android.Views.SystemUiFlags.Immersive;

                decorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)uiOptions;

                window.DecorView.SystemUiVisibility = StatusBarVisibility.Hidden;*/


            }
            catch (Exception _ex) {
                error(_ex);
            }
        }
        public StorageInfo GetStorageInformation(string path = "")
        {
            try {
                StorageInfo storageInfo = new StorageInfo();

                long totalSpaceBytes = 0;
                long freeSpaceBytes = 0;
                long availableSpaceBytes = 0;

                /*
                  We have to do the check for the Android version, because the OS calls being made have been deprecated for older versions. 
                  The ‘old style’, pre Android level 18 didn’t use the Long suffixes, so if you try and call use those on 
                  anything below Android 4.3, it’ll crash on you, telling you that that those methods are unavailable. 
                  http://blog.wislon.io/posts/2014/09/28/xamarin-and-android-how-to-use-your-external-removable-sd-card/
                 */
                if (path == "") {
                    totalSpaceBytes = Android.OS.Environment.ExternalStorageDirectory.TotalSpace;
                    freeSpaceBytes = Android.OS.Environment.ExternalStorageDirectory.FreeSpace;
                    availableSpaceBytes = Android.OS.Environment.ExternalStorageDirectory.UsableSpace;
                }
                else {
                    StatFs stat = new StatFs(path); //"/storage/sdcard1"

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2) {


                        long blockSize = stat.BlockSizeLong;
                        totalSpaceBytes = stat.BlockCountLong * stat.BlockSizeLong;
                        availableSpaceBytes = stat.AvailableBlocksLong * stat.BlockSizeLong;
                        freeSpaceBytes = stat.FreeBlocksLong * stat.BlockSizeLong;

                    }
                    else {
                        totalSpaceBytes = (long)stat.BlockCount * (long)stat.BlockSize;
                        availableSpaceBytes = (long)stat.AvailableBlocks * (long)stat.BlockSize;
                        freeSpaceBytes = (long)stat.FreeBlocks * (long)stat.BlockSize;
                    }
                }

                storageInfo.TotalSpace = totalSpaceBytes;
                storageInfo.AvailableSpace = availableSpaceBytes;
                storageInfo.FreeSpace = freeSpaceBytes;
                return storageInfo;
            }
            catch (Exception _ex) {
                error(_ex);
                return new StorageInfo() {
                    AvailableSpace = 0,
                    FreeSpace = 0,
                    TotalSpace = 0,
                };
            }
        }


        public bool DeleteFile(string path)
        {
            //Context context = Android.App.Application.Context;
            try {
                Java.IO.File file = new Java.IO.File(path);
                if (file.Exists()) {
                    file.Delete();
                }
                return true;
            }
            catch (Exception) {
                return false;
            }
            /*

            string where = MediaStore.MediaColumns.Data + "=?";
            string[] selectionArgs = new string[] { file.AbsolutePath };
            ContentResolver contentResolver = context.ContentResolver;
            Android.Net.Uri filesUri = MediaStore.Files.GetContentUri("external");

            if (file.Exists()) {
                contentResolver.Delete(filesUri, where, selectionArgs);
            }*/
        }

        static readonly int NOTIFICATION_ID = 1000;
        static readonly string CHANNEL_ID = "location_notification";
        internal static readonly string COUNT_KEY = "count";
        public void Test()
        {
            return;
            Show("Test", "test");
            print("HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH");

        }

        static Java.Lang.Thread downloadThread;
        public static void DownloadFromLink(string url, string title, string toast = "", string ending = "", bool openFile = false, string descripts = "")
        {
            try {
                print("DOWNLOADING: " + url);

                DownloadManager.Request request = new DownloadManager.Request(Android.Net.Uri.Parse(url));
                request.SetTitle(title);
                request.SetDescription(descripts);
                string mainPath = Android.OS.Environment.DirectoryDownloads;
                string subPath = title + ending;
                string fullPath = mainPath + "/" + subPath;

                print("PATH: " + fullPath);

                request.SetDestinationInExternalPublicDir(mainPath, subPath);
                request.SetVisibleInDownloadsUi(true);
                request.SetNotificationVisibility(DownloadVisibility.VisibleNotifyCompleted);

                DownloadManager manager;
                manager = (DownloadManager)MainActivity.activity.GetSystemService(Context.DownloadService);

                long downloadId = manager.Enqueue(request);




                // AUTO OPENS FILE WHEN DONE DOWNLOADING
                if (openFile || toast != "") {
                    downloadThread = new Java.Lang.Thread(() => {
                        try {
                            bool exists = false;
                            while (!exists) {
                                try {
                                    string p = manager.GetUriForDownloadedFile(downloadId).Path;
                                    exists = true;
                                }
                                catch (System.Exception) {
                                    Java.Lang.Thread.Sleep(100);
                                }

                            }
                            Java.Lang.Thread.Sleep(1000);
                            if (toast != "") {
                                App.ShowToast(toast);
                            }
                            if (openFile) {

                                print("OPEN FILE");
                                //            
                                string truePath = ("file://" + Android.OS.Environment.ExternalStorageDirectory + "/" + fullPath);

                                OpenFile(truePath);
                            }
                        }
                        finally {
                            downloadThread.Join();
                        }
                    });
                    downloadThread.Start();
                }
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }
        public static void OpenFile(string link)
        {
            //  Android.Net.Uri uri = Android.Net.Uri.Parse(link);//link);
            try {
                Java.IO.File file = new Java.IO.File(Java.Net.URI.Create(link));
                print("Path:" + file.Path);

                Android.Net.Uri photoURI = FileProvider.GetUriForFile(MainActivity.activity.ApplicationContext, (MainActivity.activity.ApplicationContext.PackageName + ".provider.FileProvider"), file);
                Intent promptInstall = new Intent(Intent.ActionView).SetDataAndType(photoURI, "application/vnd.android.package-archive"); //vnd.android.package-archive
                promptInstall.AddFlags(ActivityFlags.NewTask);
                promptInstall.AddFlags(ActivityFlags.GrantReadUriPermission);
                promptInstall.AddFlags(ActivityFlags.NoHistory);
                promptInstall.AddFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(promptInstall);
            }
            catch (Exception _ex) {
                error(_ex);
            }
            /*
            Intent promptInstall = new Intent(Intent.ActionView).SetData(uri);//.SetDataAndType(uri, "application/vnd.android.package-archive");
            //   promptInstall.AddFlags(ActivityFlags.NewTask);
            promptInstall.AddFlags(ActivityFlags.GrantReadUriPermission);
            promptInstall.AddFlags(ActivityFlags.GrantWriteUriPermission);
            promptInstall.AddFlags(ActivityFlags.GrantPrefixUriPermission);
            promptInstall.AddFlags(ActivityFlags.GrantPersistableUriPermission);

            promptInstall.AddFlags(ActivityFlags.NewTask);*/


            // Android.App.Application.Context.ApplicationContext.start
            //Android.App.Application.Context.StartService(intent);
            // Android.App.Application.Context.StartActivity(promptInstall);
        }


        public static Java.IO.File WriteFile(string name, string basePath, string write)
        {
            try {
                System.IO.File.Delete(basePath + "/" + name);
            }
            catch (System.Exception) {

            }
            //name = Regex.Replace(name, @"[^A-Za-z0-9\.]+", String.Empty);
            //name.Replace(" ", "");
            //  name = name.ToLower();

            try {
                Java.IO.File file = new Java.IO.File(basePath, name);
                Java.IO.File _file = new Java.IO.File(basePath);
                CloudStreamCore.print("PATH: " + basePath + "/" + name);
                _file.Mkdirs();
                file.CreateNewFile();
                Java.IO.FileWriter writer = new Java.IO.FileWriter(file);
                // Writes the content to the file
                writer.Write(write);
                writer.Flush();
                writer.Close();
                return file;

            }
            catch (Exception _ex) {
                error("MAIN EX IN WriteFile: " + _ex);
                return null;
            }
        }

        static void OpenStore(string applicationPackageName)
        {
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + applicationPackageName));
            intent.AddFlags(ActivityFlags.NewTask);

            activity.ApplicationContext.StartActivity(intent);
        }

        public const string VLC_PACKAGE = "org.videolan.vlc";
        public const string VLC_INTENT_ACTION_RESULT = "org.videolan.vlc.player.result";
        public static ComponentName VLC_COMPONENT = new ComponentName(VLC_PACKAGE, "org.videolan.vlc.gui.video.VideoPlayerActivity");


        public static bool IsAppInstalled(string packageName)
        {
            try {
                PackageManager pm = activity.PackageManager;
                pm.GetPackageInfo(packageName, PackageInfoFlags.Activities);
                return true;
            }
            catch (PackageManager.NameNotFoundException e) {
                return false;
            }
        }

        public static void openVlc(Activity _activity, int requestId, Android.Net.Uri uri, long time, String title, string subfile = "")
        {
            Intent vlcIntent = new Intent(VLC_INTENT_ACTION_RESULT);

            vlcIntent.SetPackage(VLC_PACKAGE);
            vlcIntent.SetDataAndTypeAndNormalize(uri, "video/*");

            long position = time;
            if (time == FROM_START) {
                position = 1;
            }
            else if (time == FROM_PROGRESS) {
                position = 0;
            }

            vlcIntent.PutExtra("position", position);
            vlcIntent.PutExtra("title", title);
            vlcIntent.SetComponent(VLC_COMPONENT);
            subfile = "/sdcard/Download/subtitles.srt";
            if (subfile != "") {
                var sfile = Android.Net.Uri.FromFile(new Java.IO.File(subfile));  //"content://" + Android.Net.Uri.Parse(subfile);
                print("SUBFILE::::" + subfile + "|" + sfile.Path);
                //  print(sfile.Path);
                vlcIntent.PutExtra("subtitles_location", subfile);//"/sdcard/Download/subtitles.srt");//sfile);//Android.Net.Uri.FromFile(subFile));
                                                                  // intent.PutExtra("subtitles_location", );//Android.Net.Uri.FromFile(subFile));
            }
            _activity.StartActivityForResult(vlcIntent, requestId);

        }

        public void RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = -2, string subtitleFull = "", VideoPlayer preferedPlayer = VideoPlayer.VLC)
        {
            if (preferedPlayer == VideoPlayer.None) { App.ShowToast("No videoplayer installed"); };
            try {
                string absolutePath = Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads;
                subtitleFull = subtitleFull ?? "";
                bool subtitlesEnabled = subtitleFull != "";
                string writeData = CloudStreamForms.App.ConvertPathAndNameToM3U8(urls, names, subtitlesEnabled, "content://" + absolutePath + "/");
                WriteFile(CloudStreamForms.App.baseM3u8Name, absolutePath, writeData);

                Java.IO.File subFile = null;
                if (subtitlesEnabled) {
                    print("WRITING SUBFILE: " + absolutePath + "|" + baseSubtitleName + "|" + subtitleFull + "|" + (subtitleFull == "") + "|" + (subtitleFull == null));
                    subFile = WriteFile(CloudStreamForms.App.baseSubtitleName, absolutePath, subtitleFull);
                }

                string _bpath = absolutePath + "/" + CloudStreamForms.App.baseM3u8Name;

                if (!GetPlayerInstalled(preferedPlayer)) {
                    App.ShowToast("Videoplayer not installed");
                    return;
                }

                string package = GetVideoPlayerPackage(preferedPlayer);
                Android.Net.Uri uri = Android.Net.Uri.Parse(urls.Count == 1 ? urls[0] : _bpath);

                switch (preferedPlayer) {
                    case VideoPlayer.VLC:

                        Intent vlcIntent = new Intent(VLC_INTENT_ACTION_RESULT);

                        vlcIntent.SetPackage(VLC_PACKAGE);
                        vlcIntent.SetDataAndTypeAndNormalize(uri, "video/*");

                        long position = startId;
                        if (startId == FROM_START) {
                            position = 1;
                        }
                        else if (startId == FROM_PROGRESS) {
                            position = 0;
                        }

                        vlcIntent.PutExtra("position", position);
                        vlcIntent.PutExtra("title", episodeName);

                        if (subFile != null) {
                            var sfile = Android.Net.Uri.FromFile(subFile);
                            vlcIntent.PutExtra("subtitles_location", sfile);
                        }

                        vlcIntent.SetComponent(VLC_COMPONENT);

                        lastId = episodeId;
                        activity.StartActivityForResult(vlcIntent, REQUEST_CODE);
                        break;
                    /*  case VideoPlayer.MPV:
                          Intent mpvIntent = new Intent().SetPackage(package).SetAction(Intent.ActionView);// GetLauncherActivity(package);// new Intent();
                       //   mpvIntent.SetPackage(package);
                          mpvIntent.SetDataAndTypeAndNormalize(uri, "video/m3u8");

                          mpvIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                          mpvIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                          mpvIntent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
                          mpvIntent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                          mpvIntent.AddFlags(ActivityFlags.NewTask);

                          activity.StartActivity(mpvIntent);
                          break;*/
                    case VideoPlayer.MXPlayer:
                        Intent MXIntent = new Intent();
                        MXIntent.SetPackage(package);
                        MXIntent.SetDataAndTypeAndNormalize(uri, "video/m3u8");

                        MXIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
                        MXIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                        MXIntent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
                        MXIntent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                        MXIntent.AddFlags(ActivityFlags.NewTask);
                        Android.App.Application.Context.StartActivity(MXIntent);
                        break;
                    default:
                        break;
                }

                /*
                    Intent intent = new Intent(Intent.ActionView);
                    intent.SetDataAndTypeAndNormalize(Android.Net.Uri.Parse(_bpath), "video/*");
                    intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                    intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                    intent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
                    intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

                    intent.AddFlags(ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                 */

            }
            catch (Exception _ex) {
                error("MAIN EX IN REQUEST VLC: " + _ex);
            }
        }

        public static Random rng = new Random();

        public EventHandler<bool> OnAudioFocusChanged { set; get; }

        public void Awake()
        {
            try {
                System.Net.ServicePointManager.DefaultConnectionLimit = 999;

                App.platformDep = this;
                myAudioFocusListener = new MyAudioFocusListener();
                myAudioFocusListener.FocusChanged += ((sender, b) => {
                    OnAudioFocusChanged?.Invoke(this, b);
                    if (b) {
                        // play stuff
                    }
                    else {
                        // stop playing stuff
                    }
                });
                var playbackAttributes = new AudioAttributes.Builder()
                      .SetUsage(AudioUsageKind.Media)
                      .SetContentType(AudioContentType.Movie)
                      .Build();

                CurrentAudioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                    .SetAudioAttributes(playbackAttributes)
                    .SetAcceptsDelayedFocusGain(true)
                    .SetOnAudioFocusChangeListener(myAudioFocusListener, handler)
                    .Build();
            }
            catch (Exception _ex) {
                error(_ex);
            }
            mainS.Start();
            //  MainDelayTest();
            // long delay = getDelay();

            //  print("MAIN DELAYYYY::: " + delay);


        }

        static Stopwatch mainS = new Stopwatch();
        async Task MainDelayTest()
        {
            await Task.Delay(250);
            long f1 = mainS.ElapsedMilliseconds;
            Device.BeginInvokeOnMainThread(() => {
                long f2 = mainS.ElapsedMilliseconds;
                App.ShowToast("SHOW: " + (f2 - f1));
            });
            MainDelayTest();
        }


        MyAudioFocusListener myAudioFocusListener;

        public class MyAudioFocusListener
        : Java.Lang.Object
        , AudioManager.IOnAudioFocusChangeListener
        {
            public event EventHandler<bool> FocusChanged;

            public void OnAudioFocusChange(AudioFocus focusChange)
            {
                print("AUDIOFOCUS CHANGED:::: " + focusChange.ToString() + "|" + (int)focusChange);
                switch (focusChange) {
                    case AudioFocus.GainTransient:
                        if (FocusChanged != null)
                            FocusChanged(this, true);
                        break;
                    case AudioFocus.LossTransient:
                        if (FocusChanged != null)
                            FocusChanged(this, false);
                        break;
                    case AudioFocus.Loss:
                        if (FocusChanged != null)
                            FocusChanged(this, false);
                        break;
                    case AudioFocus.GainTransientExclusive:
                        if (FocusChanged != null)
                            FocusChanged(this, true);
                        break;
                    case AudioFocus.Gain:
                        if (FocusChanged != null)
                            FocusChanged(this, true);
                        break;
                }
            }
        }
        private Handler handler = new Handler();

        AudioManager AudioManager => Application.Context.GetSystemService(Context.AudioService) as AudioManager;


        AudioFocusRequestClass CurrentAudioFocusRequest;
        public bool GainAudioFocus()
        {
            // AudioManager.IOnAudioFocusChangeListener afChangeListener;
            // AudioFocusRequestClass.Builder audioBuilder = new AudioFocusRequestClass.Builder();
            try {
                AudioFocusRequest audio = AudioManager.RequestAudioFocus(CurrentAudioFocusRequest);
                return audio == AudioFocusRequest.Granted;
            }
            catch (Exception) {
                return false;
            }
        }


        public void ReleaseAudioFocus()
        {
            try {
                AudioFocusRequest audio = AudioManager.AbandonAudioFocusRequest(CurrentAudioFocusRequest);
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }


        public void ShowToast(string message, double duration)
        {
            Device.BeginInvokeOnMainThread(() => {
                try {
                    ToastLength toastLength = ToastLength.Short;
                    if (duration >= 3) {
                        toastLength = ToastLength.Long;
                    }
                    Toast.MakeText(Android.App.Application.Context, message, toastLength).Show();
                }
                catch (Exception _ex) {
                    error(_ex);
                }
            });
        }

        public string DownloadFile(string file, string fileName, bool mainPath, string extraPath)
        {
            try {
                return WriteFile(CensorFilename(fileName), GetPath(mainPath, extraPath), file).Path;
            }
            catch (Exception _ex) {
                error(_ex);
                return "";
            }
        }

        public string DownloadHandleIntent(int id, List<BasicMirrorInfo> mirrors, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "")//, int mirror, string title, string path, string poster, string fileName, string beforeTxt, bool openWhenDone, bool showNotificaion, bool showDoneNotificaion, bool showDoneAsToast, bool resumeIntent)
        {
            try {
                string path = GetPath(mainPath, extraPath);
                string full = path + "/" + CensorFilename(fileName);
                DownloadHandle.HandleIntent(id, mirrors, 0, titleName, path, poster, fileName, beforeTxt, openWhenDone, showNotification, showNotification, false, false);
                return full;
            }
            catch (Exception _ex) {
                error(_ex);
                return "";
            }
        }

        public string DownloadUrl(string url, string fileName, bool mainPath, string extraPath, string toast = "", bool isNotification = false, string body = "")
        {
            try {

                string basePath = GetPath(mainPath, extraPath);
                CloudStreamCore.print(basePath);
                Java.IO.File _file = new Java.IO.File(basePath);

                _file.Mkdirs();
                basePath += "/" + CensorFilename(fileName);
                CloudStreamCore.print(basePath);
                //webClient.DownloadFile(url, basePath);
                using (WebClient wc = new WebClient()) {
                    wc.DownloadProgressChanged += (o, e) => {

                        App.OnDownloadProgressChanged(basePath, e);

                        /*
                        if (e.ProgressPercentage == 100) {
                            App.ShowToast("Download Successful");
                            //OpenFile(basePath);
                        }*/
                        // print(e.ProgressPercentage + "|" + basePath);
                    };
                    wc.DownloadFileCompleted += (o, e) => {
                        if (toast != "") {
                            if (isNotification) {
                                App.ShowNotification(toast, body);
                            }
                            else {
                                App.ShowToast(toast);
                            }
                        }
                    };
                    wc.DownloadFileAsync(
                        // Param1 = Link of file
                        new System.Uri(url),
                        // Param2 = Path to save
                        basePath
                    );
                }

            }
            catch (Exception) {
                App.ShowToast("Download Failed");
                return "";
            }

            return GetPath(mainPath, extraPath) + "/" + CensorFilename(fileName);
        }

        public void DownloadUpdate(string update)
        {
            try {
                string downloadLink = "https://github.com/LagradOst/CloudStream-2/releases/download/" + update + "/com.CloudStreamForms.CloudStreamForms.apk";
                App.ShowToast("Download started!");
                //  DownloadUrl(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", true, "", "Download complete!");
                DownloadFromLink(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", "Download complete!", "", false, "");
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public string GetDownloadPath(string path, string extraFolder)
        {
            try {
                return GetPath(true, extraFolder + "/" + CensorFilename(path, false));
            }
            catch (Exception _ex) {
                error(_ex);
                return "";
            }
        }

        public string GetExternalStoragePath()
        {
            try {

                return Android.OS.Environment.ExternalStorageDirectory.Path;
            }
            catch (Exception _ex) {
                return "";
                error(_ex);
            }
        }

        public int ConvertDPtoPx(int dp)
        {
            try {
                return (int)(dp * MainActivity.activity.ApplicationContext.Resources.DisplayMetrics.Density);

            }
            catch (Exception _ex) {
                error(_ex);
                return 1;
            }
        }

        public void CancelNot(int id)
        {
            try {
                CancelFutureNotification(id);
            }
            catch (Exception _ex) {
                error(_ex);
            }
        }

        public string ReadFile(string fileName, bool mainPath, string extraPath)
        {
            try {
                string basePath = GetPath(mainPath, extraPath);
                Java.IO.File _file = new Java.IO.File(basePath, fileName);
                Java.IO.FileReader reader = new Java.IO.FileReader(_file);
                char[] data = new char[1024];
                int count = 0;
                StringBuilder builder = new StringBuilder();
                while ((count = reader.Read(data, 0, data.Length)) != -1) {
                    for (int i = 0; i < count; i++) {
                        builder.Append(data[i]);
                    }
                }

                return builder.ToString();
            }
            catch (Exception) {
                return "";
            }
        }

        static string GetVideoPlayerPackage(VideoPlayer player)
        {
            return player switch
            {
                // VideoPlayer.MPV => "is.xyz.mpv",
                VideoPlayer.MXPlayer => "com.mxtech.videoplayer.ad",
                VideoPlayer.VLC => "org.videolan.vlc",
                _ => "",
            };
        }

        public bool GetPlayerInstalled(VideoPlayer player)
        {
            if (player == VideoPlayer.None) {
                return false;
            }
            return IsAppInstalled(GetVideoPlayerPackage(player));
        }
    }

    public class NullPlatfrom : App.IPlatformDep
    {
        public EventHandler<bool> OnAudioFocusChanged { get; set; }

        public void CancelNot(int id)
        {
        }

        public int ConvertDPtoPx(int dp)
        {
            return 1;
        }

        public bool DeleteFile(string path)
        {
            return true;
        }

        public string DownloadFile(string file, string fileName, bool mainPath, string extraPath)
        {
            return "";
        }
        public string DownloadHandleIntent(int id, List<BasicMirrorInfo> mirrors, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "")
        {
            return "";
        }

        public void DownloadUpdate(string update)
        {
        }

        public string DownloadUrl(string url, string fileName, bool mainPath, string extraPath, string toast = "", bool isNotification = false, string body = "")
        {
            return "";
        }

        public bool GainAudioFocus()
        {
            return true;
        }

        /*
        public BluetoothDeviceID[] GetBluetoothDevices()
        {
            return null;
        }*/

        public double GetBrightness()
        {
            return 1;
        }

        public string GetDownloadPath(string path, string extraFolder)
        {
            return "";
        }

        public DownloadProgressInfo GetDownloadProgressInfo(int id, string fileUrl)
        {
            return null;
        }

        public string GetExternalStoragePath()
        {
            return "";
        }

        public bool GetPlayerInstalled(VideoPlayer player)
        {
            return false;
        }

        public StorageInfo GetStorageInformation(string path = "")
        {
            return null;
        }

        public void HideStatusBar()
        {
        }

        public void LandscapeOrientation()
        {
        }

        public void NormalOrientation()
        {
        }

        public void PlayExternalApp(VideoPlayer player)
        {
            throw new NotImplementedException();
        }


        public string ReadFile(string fileName, bool mainPath, string extraPath)
        {
            throw new NotImplementedException();
        }

        public void ReleaseAudioFocus()
        {
        }

        public void RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = -2, string subtitleFull = "", VideoPlayer preferedPlayer = VideoPlayer.VLC)
        {
        }

        public void SearchBluetoothDevices()
        {
        }

        public void SetBrightness(double opacity)
        {
        }

        public void ShowNotIntent(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "")
        {
        }

        public void ShowStatusBar()
        {
        }

        public void ShowToast(string message, double duration)
        {
        }

        public void Test()
        {
        }

        public void ToggleFullscreen(bool fullscreen)
        {
        }

        public void ToggleRealFullScreen(bool fullscreen)
        {
        }

        public void UpdateBackground(int color)
        {
        }

        public void UpdateBackground()
        {
        }

        public void UpdateDownload(int id, int state)
        {
        }

        public void UpdateStatusBar()
        {
        }
    }
}
