using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using LibVLCSharp.Forms.Shared;
using Plugin.LocalNotifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;
using static CloudStreamForms.App;
using static CloudStreamForms.CloudStreamCore;
using Application = Android.App.Application;

namespace CloudStreamForms.Droid
{
    [Activity(Label = "CloudStream 2", Icon = "@drawable/bicon", Theme = "@style/MainTheme.Splash", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation), IntentFilter(new[] { Intent.ActionView }, DataScheme = "cloudstreamforms", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        MainDroid mainDroid;
        public static Activity activity;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);
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



            LocalNotificationsImplementation.NotificationIconId = Resource.Drawable.bicon;
            MainDroid.NotificationIconId = Resource.Drawable.bicon;


            LoadApplication(new App());
            activity = this;

            mainDroid = new MainDroid();
            mainDroid.Awake();

            if (Intent.DataString != null) {
                if (Intent.DataString != "") {
                    MainPage.PushPageFromUrlAndName(Intent.DataString);
                }
            }
            RequestPermission(this);
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

        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public static int REQUEST_START = 112;
        public static int REQUEST_INSTALL = 113;
        public static int REQUEST_INSTALL2 = 113;
        public static int REQUEST_INSTALL3 = 114;
        private static void RequestPermission(Activity context)
        {

            List<string> requests = new List<string>() {
                Manifest.Permission.WriteExternalStorage, Manifest.Permission.RequestInstallPackages,Manifest.Permission.InstallPackages,Manifest.Permission.WriteSettings
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
    }




    public class MainDroid : App.IPlatformDep
    {
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
        static NotificationManager _manager => (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);

        /// <summary>
        /// Show a local notification
        /// </summary>
        /// <param name="title">Title of the notification</param>
        /// <param name="body">Body or description of the notification</param>
        /// <param name="id">Id of the notification</param>
        public void Show(string title, string body, int id = 0)
        {
            var builder = new Notification.Builder(Application.Context);
            builder.SetContentTitle(title);
            builder.SetContentText(body);
            builder.SetAutoCancel(true);

            if (NotificationIconId != 0) {
                builder.SetSmallIcon(NotificationIconId);
            }
            else {
                builder.SetSmallIcon(Resource.Drawable.plugin_lc_smallicon);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
                var channelId = $"{_packageName}.general";
                var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);

                _manager.CreateNotificationChannel(channel);

                builder.SetChannelId(channelId);

                var context = MainActivity.activity.ApplicationContext;
                var _resultIntent = new Intent(context, typeof(MainActivity));
                //_resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);

                var pending = PendingIntent.GetActivity(context, 0,
                    _resultIntent,
                   // PendingIntentFlags.CancelCurrent
                   PendingIntentFlags.UpdateCurrent
                    );

                // SET AFTER THO 
                RemoteViews remoteViews = new RemoteViews(Application.Context.PackageName, Resource.Xml.PausePlay);
                // remoteViews.SetImageViewResource(R.id.notifAddDriverIcon, R.drawable.my_trips_new);
                builder.SetCustomContentView(remoteViews);

                builder.SetShowWhen(false);

                //  builder.SetProgress(100, 51, false); // PROGRESSBAR
                //  builder.SetLargeIcon(Android.Graphics.Drawables.Icon.CreateWithResource(context, Resource.Drawable.bicon)); // POSTER
                builder.SetActions(new Notification.Action(Resource.Drawable.design_bottom_navigation_item_background, "Hello", pending)); // IDK TEXT PRESS
            }

            var resultIntent = GetLauncherActivity();
            resultIntent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            var stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(Application.Context);
            stackBuilder.AddNextIntent(resultIntent);
            var resultPendingIntent =
                stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);
            builder.SetContentIntent(resultPendingIntent);

            _manager.Notify(id, builder.Build());
        }
        public static Intent GetLauncherActivity()
        {
            var packageName = Application.Context.PackageName;
            return Application.Context.PackageManager.GetLaunchIntentForPackage(packageName);
        }









        static bool hidden = false;
        static int baseShow = 0;

        public void UpdateBackground(int color)
        {
            Window window = MainActivity.activity.Window;
            window.SetNavigationBarColor(Android.Graphics.Color.Rgb(color, color, color));
            /*
            Window window = MainActivity.activity.Window;
            int color = Settings.BlackColor - 5;
            if(color > 255) { color = 255; }
            if(color < 0) { color = 0; }
            window.SetNavigationBarColor(Android.Graphics.Color.Rgb(color, color, color));*/
        }

        public void UpdateStatusBar()
        {
            // Window window = MainActivity.activity.Window;
            ToggleFullscreen(!Settings.HasStatusBar);
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
            Window window = MainActivity.activity.Window;
            if (fullscreen) {
                window.AddFlags(WindowManagerFlags.Fullscreen); // REMOVES STATUS BAR
            }
            else {
                window.ClearFlags(WindowManagerFlags.Fullscreen);
            }
        }

        public void LandscapeOrientation()
        {
            MainActivity.activity.RequestedOrientation = ScreenOrientation.Landscape;

        }
        public void NormalOrientation()
        {
            MainActivity.activity.RequestedOrientation = ScreenOrientation.Unspecified;
        }


        public void ShowStatusBar()
        {
            if (!hidden) return;

            Window window = MainActivity.activity.Window;
            window.ClearFlags(WindowManagerFlags.TurnScreenOn);
            window.ClearFlags(WindowManagerFlags.KeepScreenOn);
            //ToggleFullscreen(!Settings.HasStatusBar);
            
            if (Settings.HasStatusBar) {
                window.ClearFlags(WindowManagerFlags.Fullscreen);
            }

            window.DecorView.SystemUiVisibility = (StatusBarVisibility)baseShow;
        }

        public void HideStatusBar()
        {

            if (hidden) return;
            hidden = true;

            Window window = MainActivity.activity.Window;
            window.AddFlags(WindowManagerFlags.TurnScreenOn);
            window.AddFlags(WindowManagerFlags.KeepScreenOn);
            
            if (Settings.HasStatusBar) {
                window.AddFlags(WindowManagerFlags.Fullscreen);
            }

            int uiOptions = (int)window.DecorView.SystemUiVisibility;
            baseShow = uiOptions;

            uiOptions |= (int)SystemUiFlags.LowProfile;
            // uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
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
        public StorageInfo GetStorageInformation(string path = "")
        {
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

        public static void OpenPathAsVideo(string path, string name, string subtitleLoc)
        {
            OpenPathsAsVideo(new List<string>() { path }, new List<string>() { name }, subtitleLoc);
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

            Show("Test", "test");
            print("HHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH");

        }

        static Java.Lang.Thread downloadThread;
        public static void DownloadFromLink(string url, string title, string toast = "", string ending = "", bool openFile = false, string descripts = "")
        {
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
        public static void OpenFile(string link)
        {
            //  Android.Net.Uri uri = Android.Net.Uri.Parse(link);//link);
            Java.IO.File file = new Java.IO.File(Java.Net.URI.Create(link));
            print("Path:" + file.Path);

            Android.Net.Uri photoURI = FileProvider.GetUriForFile(MainActivity.activity.ApplicationContext, (MainActivity.activity.ApplicationContext.PackageName + ".provider.FileProvider"), file);
            Intent promptInstall = new Intent(Intent.ActionView).SetDataAndType(photoURI, "application/vnd.android.package-archive"); //vnd.android.package-archive
            promptInstall.AddFlags(ActivityFlags.NewTask);
            promptInstall.AddFlags(ActivityFlags.GrantReadUriPermission);
            promptInstall.AddFlags(ActivityFlags.NoHistory);
            promptInstall.AddFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(promptInstall);
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
            Android.App.Application.Context.StartActivity(promptInstall);
        }

        public static Java.IO.File WriteFile(string name, string basePath, string write)
        {
            try {
                File.Delete(basePath + "/" + name);
            }
            catch (System.Exception) {

            }
            //name = Regex.Replace(name, @"[^A-Za-z0-9\.]+", String.Empty);
            //name.Replace(" ", "");
            //  name = name.ToLower();

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


        public static async Task OpenPathsAsVideo(List<string> path, List<string> name, string subtitleLoc)
        {
            string absolutePath = Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads;
            CloudStreamCore.print("AVS: " + absolutePath);

            bool subtitlesEnabled = subtitleLoc != "";
            string writeData = CloudStreamForms.App.ConvertPathAndNameToM3U8(path, name, subtitlesEnabled, "content://" + absolutePath + "/");
            Java.IO.File subFile = null;
            WriteFile(CloudStreamForms.App.baseM3u8Name, absolutePath, writeData);
            if (subtitlesEnabled) {
                subFile = WriteFile(CloudStreamForms.App.baseSubtitleName, absolutePath, subtitleLoc);
            }

            // await Task.Delay(5000);

            Device.BeginInvokeOnMainThread(() => {
                // OpenPathAsVideo(path.First(), name.First(), "");
                OpenVlcIntent(absolutePath + "/" + CloudStreamForms.App.baseM3u8Name, absolutePath + "/" + App.baseSubtitleName);
            });
        }




        public static void OpenVlcIntent(string path, string subfile = "") //Java.IO.File subFile)
        {
            Android.Net.Uri uri = Android.Net.Uri.Parse(path);

            Intent intent = new Intent(Intent.ActionView).SetDataAndType(uri, "video/*");
            //intent.SetPackage("org.videolan.vlc");
            // Main.print("Da_ " + Android.Net.Uri.Parse(subfile));

            if (subfile != "") {
                var sfile = Android.Net.Uri.FromFile(new Java.IO.File(subfile));  //"content://" + Android.Net.Uri.Parse(subfile);
                                                                                  //  print(sfile.Path);
                intent.PutExtra("subtitles_location", sfile);//Android.Net.Uri.FromFile(subFile));
                                                             // intent.PutExtra("subtitles_location", );//Android.Net.Uri.FromFile(subFile));
            }

            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
            intent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
            intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

            intent.AddFlags(ActivityFlags.NewTask);


            // Android.App.Application.Context.ApplicationContext.start
            //Android.App.Application.Context.StartService(intent);
            Android.App.Application.Context.StartActivity(intent);
        }

        public void PlayVlc(string url, string name, string subtitleLoc)
        {
            try {
                MainDroid.OpenPathAsVideo(url, name, subtitleLoc);
            }
            catch (Exception) {
                CloudStreamForms.App.OpenBrowser(url);
            }
        }
        public void PlayVlc(List<string> url, List<string> name, string subtitleLoc)
        {
            try {
                MainDroid.OpenPathsAsVideo(url, name, subtitleLoc);
            }
            catch (Exception) {
                CloudStreamForms.App.OpenBrowser(url.First());
            }

        }
        public void Awake()
        {
            App.platformDep = this;
        }

        public void ShowToast(string message, double duration)
        {
            Device.BeginInvokeOnMainThread(() => {
                ToastLength toastLength = ToastLength.Short;
                if (duration >= 3) {
                    toastLength = ToastLength.Long;
                }
                Toast.MakeText(Android.App.Application.Context, message, toastLength).Show();
            });

        }

        public static string GetPath(bool mainPath, string extraPath)
        {
            return (mainPath ? (Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads) : (Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads + "/Extra")) + extraPath;
        }

        public string DownloadFile(string file, string fileName, bool mainPath, string extraPath)
        {
            return WriteFile(CensorFilename(fileName), GetPath(mainPath, extraPath), file).Path;
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



        static string CensorFilename(string name, bool toLower = true)
        {
            name = Regex.Replace(name, @"[^A-Za-z0-9\.]+", String.Empty);
            name.Replace(" ", "");
            if (toLower) {
                name = name.ToLower();
            }
            return name;
        }

        public string GetBuildNumber()
        {
            var context = Android.App.Application.Context;
            var VersionNumber = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.MetaData).VersionName;
            var BuildNumber = context.PackageManager.GetPackageInfo(context.PackageName, PackageInfoFlags.MetaData).VersionCode.ToString();
            return BuildNumber + " " + VersionNumber;
        }

        public void DownloadUpdate(string update)
        {
            string downloadLink = "https://github.com/LagradOst/CloudStream-2/releases/download/" + update + "/com.CloudStreamForms.CloudStreamForms.apk";
            App.ShowToast("Download started!");
            //  DownloadUrl(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", true, "", "Download complete!");
            DownloadFromLink(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", "Download complete!", "", false, "");

        }

        public string GetDownloadPath(string path, string extraFolder)
        {
            return GetPath(true, extraFolder + "/" + CensorFilename(path, false));
        }

        public string GetExternalStoragePath()
        {
            return Android.OS.Environment.ExternalStorageDirectory.Path;
        }

        public int ConvertDPtoPx(int dp)
        {
            return (int)(dp * MainActivity.activity.ApplicationContext.Resources.DisplayMetrics.Density);
        }
    }
}
