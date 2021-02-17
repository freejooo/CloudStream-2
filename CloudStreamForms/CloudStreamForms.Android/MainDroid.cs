using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Support.V4.Content.PM;
using Android.Support.V4.Graphics.Drawable;
using Android.Views;
using Android.Widget;
using CloudStreamForms.Core;
using CloudStreamForms.Droid.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Droid.LocalNot;
using static CloudStreamForms.Droid.MainActivity;
using static CloudStreamForms.Droid.MainHelper;
using Application = Android.App.Application;

namespace CloudStreamForms.Droid
{
	public class MainDroid : App.IPlatformDep
	{
		//ShortcutManager ShortManager => Application.Context.GetSystemService(Context.ShortcutService) as ShortcutManager;
		//https://github.com/severnake/AndroSpy/blob/9a46b61e0adcd416e3a7b27ec24d22bf6f55dfd5/Client/MainActivity.cs#L440
		public async void AddShortcut(string name, string imdbId, string url)
		{
			var bitmap = await GetImageBitmapFromUrl(url);
			var uri = Android.Net.Uri.Parse($"cloudstreamforms:{imdbId}Name={name}=EndAll");
			var intent_ = new Intent(Intent.ActionView, uri);
			if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
				if (ShortcutManagerCompat.IsRequestPinShortcutSupported(MainActivity.activity)) {
					ShortcutInfoCompat shortcutInfo = new ShortcutInfoCompat.Builder(MainActivity.activity, imdbId)
					 .SetIntent(intent_)
					 .SetShortLabel(name)
					 .SetIcon(IconCompat.CreateWithBitmap(bitmap))
					 .Build();
					ShortcutManagerCompat.RequestPinShortcut(MainActivity.activity, shortcutInfo, null);
				}
			}
			else {
				Intent installer = new Intent();
				installer.PutExtra("android.intent.extra.shortcut.INTENT", intent_);
				installer.PutExtra("android.intent.extra.shortcut.NAME", name);
				installer.PutExtra("android.intent.extra.shortcut.ICON", bitmap);
				installer.SetAction("com.android.launcher.action.INSTALL_SHORTCUT");
				MainActivity.activity.SendBroadcast(installer);
			}
		}

		public void PictureInPicture()
		{
			MainActivity.activity.ShowPictureInPicture();
		}

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

		
		

		public DownloadProgressInfo GetDownloadProgressInfo(int id, string fileUrl)
		{
			try {
				DownloadProgressInfo progressInfo = new DownloadProgressInfo();

				bool downloadingOrPaused = DownloadHandle.isPaused.ContainsKey(id);

				var file = new Java.IO.File(fileUrl);

				bool exists = file.Exists();

				if (downloadingOrPaused) {
					int paused = DownloadHandle.isPaused[id];
					progressInfo.state = paused == 1 ? DownloadState.Paused : DownloadState.Downloading;
				}
				else {
					progressInfo.state = exists ? DownloadState.Downloaded : DownloadState.NotDownloaded;
				}

				if (progressInfo.bytesDownloaded < progressInfo.totalBytes - 10 && progressInfo.state == DownloadState.Downloaded) {
					progressInfo.state = DownloadState.NotDownloaded;
				}
				print("CONTAINS ::>>" + DownloadHandle.isStartProgress.ContainsKey(id));
				progressInfo.bytesDownloaded = (exists ? (file.Length()) : 0) + (DownloadHandle.isStartProgress.ContainsKey(id) ? 1 : 0);

				progressInfo.totalBytes = exists ? App.GetKey<long>("dlength", "id" + id, 0) : 0;
				print("STATE:::::==" + progressInfo.totalBytes + "|" + progressInfo.bytesDownloaded);

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
				return null;
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
		static string PkgName => Application.Context.PackageName;

		public const int CHROME_CAST_NOTIFICATION_ID = 1337;

		public static void CancelChromecast()
		{
			NotManager.Cancel(CHROME_CAST_NOTIFICATION_ID);
		}

		readonly static MediaSession mediaSession = new MediaSession(Application.Context, "Chromecast");

		public static async void UpdateChromecastNotification(string title, string body, bool isPaused, string poster)
		{
			try {
				var builder = new Notification.Builder(Application.Context);
				builder.SetContentTitle(title);
				builder.SetContentText(body);
				builder.SetAutoCancel(false);

				builder.SetSmallIcon(Resource.Drawable.round_cast_white_48dp2_4);
				builder.SetOngoing(true);

				var context = MainActivity.activity.ApplicationContext;
				if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
					var channelId = $"{PkgName}.general";
					var channel = new NotificationChannel(channelId, "General", NotificationImportance.Default);

					NotManager.CreateNotificationChannel(channel);

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
					NotManager.Notify(CHROME_CAST_NOTIFICATION_ID, builder.Build());
				}
				catch (Exception _ex) {
					print("EX NOTTIFY;; " + _ex);
				}
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}

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

        
		public void UpdateIcon(int icon)
		{
			ComponentName adaptive = new Android.Content.ComponentName(Application.Context, "com.CloudStreamForms.CloudStreamForms.adaptive");
			ComponentName hexagon = new Android.Content.ComponentName(Application.Context, "com.CloudStreamForms.CloudStreamForms.hexagon");

			ComponentEnabledState[] states = new ComponentEnabledState[2];

			switch (icon)
			{
				case 0:
					states[0] = ComponentEnabledState.Enabled;
					states[1] = ComponentEnabledState.Disabled;
					break;
				case 1:
					states[0] = ComponentEnabledState.Disabled;
					states[1] = ComponentEnabledState.Enabled;
					break;
			}

			Forms.Context.PackageManager.SetComponentEnabledSetting(
					adaptive,
					states[0],
					ComponentEnableOption.DontKillApp);
			Forms.Context.PackageManager.SetComponentEnabledSetting(
					hexagon,
					states[1],
					ComponentEnableOption.DontKillApp);
		}

		public void UpdateStatusBar()
		{
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

				if (fullscreen) {
					uiOptions |= (int)SystemUiFlags.HideNavigation;
					uiOptions |= (int)SystemUiFlags.Fullscreen;
					uiOptions |= (int)SystemUiFlags.LowProfile;
					uiOptions |= (int)SystemUiFlags.LayoutStable;
					uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
					uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;

					window.AddFlags(WindowManagerFlags.TurnScreenOn);
					window.AddFlags(WindowManagerFlags.KeepScreenOn);
					window.AddFlags(WindowManagerFlags.Fullscreen); // REMOVES STATUS BAR
				}
				else {
					uiOptions &= ~(int)SystemUiFlags.HideNavigation;
					uiOptions &= ~(int)SystemUiFlags.Fullscreen;
					uiOptions &= ~(int)SystemUiFlags.LowProfile;
					uiOptions &= ~(int)SystemUiFlags.LayoutStable;
					uiOptions &= ~(int)SystemUiFlags.ImmersiveSticky;
					uiOptions &= ~(int)SystemUiFlags.LayoutHideNavigation;

					window.ClearFlags(WindowManagerFlags.TurnScreenOn);
					window.ClearFlags(WindowManagerFlags.KeepScreenOn);
					window.ClearFlags(WindowManagerFlags.Fullscreen);
				}

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
				MainActivity.activity.RequestedOrientation = Settings.VideoplayerLockLandscape ? ScreenOrientation.Landscape : ScreenOrientation.SensorLandscape;
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
			try {
				Window window = MainActivity.activity.Window;

				window.ClearFlags(WindowManagerFlags.Fullscreen);
				int uiOptions = (int)window.DecorView.SystemUiVisibility;
				uiOptions &= ~(int)SystemUiFlags.Fullscreen;
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
				Window window = MainActivity.activity.Window;
				if (Settings.HasStatusBar) {
					window.AddFlags(WindowManagerFlags.Fullscreen);
				}

				int uiOptions = (int)window.DecorView.SystemUiVisibility;
				uiOptions |= (int)SystemUiFlags.Fullscreen;
				uiOptions |= (int)SystemUiFlags.ImmersiveSticky;

				window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
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
#pragma warning disable CS0618 // Type or member is obsolete
						totalSpaceBytes = (long)stat.BlockCount * (long)stat.BlockSize;
						availableSpaceBytes = (long)stat.AvailableBlocks * (long)stat.BlockSize;
						freeSpaceBytes = (long)stat.FreeBlocks * (long)stat.BlockSize;
#pragma warning restore CS0618 // Type or member is obsolete
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
		}

		public void Test()
		{
		}

		// static Java.Lang.Thread downloadThread;
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
					Java.Lang.Thread downloadThread = new Java.Lang.Thread(() => {
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
								string truePath = (Android.OS.Environment.ExternalStorageDirectory + "/" + fullPath);
								OpenFile(truePath);
							}
						}
						catch { }
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
			var file = new Java.IO.File(link);
			var promptInstall = new Intent(Intent.ActionView).AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
			if ((int)Build.VERSION.SdkInt >= (int)Android.OS.BuildVersionCodes.N) {
				promptInstall.AddFlags(ActivityFlags.GrantReadUriPermission);
				var _uri = GenericFileProvider.GetUriForFile(activity.ApplicationContext, activity.ApplicationContext.ApplicationInfo.PackageName + ".provider.storage", file);
				promptInstall.SetDataAndType(_uri, "application/vnd.android.package-archive");
			}
			else {
				promptInstall.SetDataAndType(Android.Net.Uri.FromFile(file), "application/vnd.android.package-archive");
			}

			activity.ApplicationContext.StartActivity(promptInstall);
		}

		public static Java.IO.File WriteFile(string path, string write)
		{
			var split = path.Split('/');
			string ending = split[^1];
			string start = split[0..^1].Aggregate((i, j) => i + "/" + j);
			return WriteFile(ending, start, write);
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
		/*
		static void OpenStore(string applicationPackageName)
		{
			Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + applicationPackageName));
			intent.AddFlags(ActivityFlags.NewTask);

			activity.ApplicationContext.StartActivity(intent);
		}*/

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
			catch {
				return false;
			}
		}

		public void RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = -2, string subtitleFull = "", VideoPlayer preferedPlayer = VideoPlayer.VLC, bool generateM3u8 = true)
		{
			if (preferedPlayer == VideoPlayer.None) { App.ShowToast("No videoplayer installed"); };
			try {
				string _bpath = "";
				Java.IO.File subFile = null;

				if (generateM3u8) {
					string absolutePath = Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads;
					subtitleFull ??= "";
					bool subtitlesEnabled = subtitleFull != "";
					string writeData = ConvertPathAndNameToM3U8(urls, names, subtitlesEnabled, "content://" + absolutePath + "/");
					WriteFile(baseM3u8Name, absolutePath, writeData);

					if (subtitlesEnabled) {
						print("WRITING SUBFILE: " + absolutePath + "|" + baseSubtitleName + "|" + subtitleFull + "|" + (subtitleFull == "") + "|" + (subtitleFull == null));
						subFile = WriteFile(baseSubtitleName, absolutePath, subtitleFull);
					}

					_bpath = absolutePath + "/" + baseM3u8Name;
				}
				else {
					_bpath = urls[0];
				}

				if (!GetPlayerInstalled(preferedPlayer)) {
					App.ShowToast("Videoplayer not installed");
					return;
				}

				string package = GetVideoPlayerPackage(preferedPlayer);
				Android.Net.Uri uri = Android.Net.Uri.Parse(urls.Count == 1 ? urls[0] : _bpath);

				switch (preferedPlayer) {
					case VideoPlayer.Auto:
						Intent intent = new Intent(Intent.ActionView);
						intent.SetDataAndTypeAndNormalize(Android.Net.Uri.Parse(_bpath), "video/*");
						intent.AddFlags(ActivityFlags.GrantReadUriPermission);
						intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
						intent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
						intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

						intent.AddFlags(ActivityFlags.NewTask);
						Application.Context.StartActivity(intent);
						break;
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
							vlcIntent.PutExtra("subtitles_location", sfile.Path);
							//vlcIntent.PutExtra("sub_mrl", "file://" sfile.Path);
							//vlcIntent.PutExtra("subtitles_location", "file:///storage/emulated/0/Download/mirrorlist.srt");
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
					case VideoPlayer.MXPlayerPro:
						Intent MXProIntent = new Intent();
						MXProIntent.SetPackage(package);
						MXProIntent.SetDataAndTypeAndNormalize(uri, "video/m3u8");

						MXProIntent.AddFlags(ActivityFlags.GrantReadUriPermission);
						MXProIntent.AddFlags(ActivityFlags.GrantWriteUriPermission);
						MXProIntent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
						MXProIntent.AddFlags(ActivityFlags.GrantPersistableUriPermission);

						MXProIntent.AddFlags(ActivityFlags.NewTask);
						Android.App.Application.Context.StartActivity(MXProIntent);
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
				App.ShowToast("Error Loading Videoplayer");
				error("MAIN EX IN REQUEST VLC: " + _ex);
			}
		}

		public static Random rng = new Random();

		public EventHandler<bool> OnAudioFocusChanged { set; get; }

		public void Awake()
		{
			try {
				System.Net.ServicePointManager.DefaultConnectionLimit = 999;

				App.FullPictureInPictureSupport = activity.CanShowPictureInPicture();
				if (App.FullPictureInPictureSupport) {
					activity.pictureInPictureParamsBuilder = new PictureInPictureParams.Builder();
				}

				App.PlatformDep = this;

				myAudioFocusListener = new MyAudioFocusListener();
				myAudioFocusListener.FocusChanged += ((sender, b) => {
					OnAudioFocusChanged?.Invoke(this, b);
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
		}

#if DEBUG
		ActivityManager ActManager => Application.Context.GetSystemService(Context.ActivityService) as ActivityManager;
#endif
		readonly static Stopwatch mainS = new Stopwatch();
#if DEBUG
		async void MainDelayTest()
		{
			await Task.Delay(5000);
			ActivityManager.MemoryInfo mi = new ActivityManager.MemoryInfo();
			ActManager.GetMemoryInfo(mi);
			double availableMegs = mi.AvailMem / 0x100000L;

			//Percentage can be calculated for API 16+
			double percentAvail = mi.AvailMem / (double)mi.TotalMem * 100.0;
			App.ShowToast("GG: " + (int)percentAvail + "MEGS: " + availableMegs + " | ");
			/*
			Device.BeginInvokeOnMainThread(() => {

				long f2 = mainS.ElapsedMilliseconds;
				App.ShowToast("SHOW: " + (f2 - f1));
			});*/
			//MainDelayTest();
		}
#endif

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
						FocusChanged?.Invoke(this, true);
						break;
					case AudioFocus.LossTransient:
						FocusChanged?.Invoke(this, false);
						break;
					case AudioFocus.Loss:
						FocusChanged?.Invoke(this, false);
						break;
					case AudioFocus.GainTransientExclusive:
						FocusChanged?.Invoke(this, true);
						break;
					case AudioFocus.Gain:
						FocusChanged?.Invoke(this, true);
						break;
				}
			}
		}
		private readonly Handler handler = new Handler();

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

		public string DownloadFile(string path, string data)
		{
			try {
				return WriteFile(GetPath(false, path), data).Path;
			}
			catch (Exception _ex) {
				error(_ex);
				return "";
			}
		}

		public string DownloadHandleIntent(int id, List<BasicMirrorInfo> mirrors, string fileName, string titleName, bool mainPath, string extraPath, bool showNotification = true, bool showNotificationWhenDone = true, bool openWhenDone = false, string poster = "", string beforeTxt = "")//, int mirror, string title, string path, string poster, string fileName, string beforeTxt, bool openWhenDone, bool showNotificaion, bool showDoneNotificaion, bool showDoneAsToast, bool resumeIntent)
		{
			try {
				string full = "";
				if (mainPath) {
					string path = GetPath(mainPath, extraPath);
					full = path + "/" + CensorFilename(fileName);
				}
				else {
					full = GetPath(mainPath, extraPath);
				}
				DownloadHandle.HandleIntent(id, mirrors, 0, titleName, full, poster, beforeTxt, openWhenDone, showNotification, showNotification, false, false);
				return full;
			}
			catch (Exception _ex) {
				error(_ex);
				return "";
			}
		}

		public void DownloadUpdate(string update, string version)
		{
			try {
				string downloadLink = "https://github.com/LagradOst/CloudStream-2/releases/download/" + update + $"/{version}com.CloudStreamForms.CloudStreamForms.apk";
				App.ShowToast("Download started!");
				//  DownloadUrl(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", true, "", "Download complete!");
				DownloadFromLink(downloadLink, "com.CloudStreamForms.CloudStreamForms.apk", "Download complete!", "", true, "");
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
				error(_ex);
				return "";
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

		public string ReadFile(string path)
		{
			try {
				path = path.Replace("file://", "").Replace("content://", "");
				Java.IO.File _file = new Java.IO.File(path);
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
			catch {
				return "";
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
			return player switch {
				// VideoPlayer.MPV => "is.xyz.mpv",
				VideoPlayer.MXPlayer => "com.mxtech.videoplayer.ad",
				VideoPlayer.MXPlayerPro => "com.mxtech.videoplayer.pro",
				VideoPlayer.VLC => "org.videolan.vlc",
				_ => "",
			};
		}

		public bool GetPlayerInstalled(VideoPlayer player)
		{
			if (player == VideoPlayer.None) return false;
			if (player == VideoPlayer.Auto) return true;

			return IsAppInstalled(GetVideoPlayerPackage(player));
		}

		//https://stackoverflow.com/questions/37112544/how-to-identify-processor-architecture-in-xamarin-android
		public int GetArchitecture()
		{
			IList<string> abis = Android.OS.Build.SupportedAbis;
			foreach (var item in abis) { // arm64-v8a armeabi-v7a armeabi x86 x86_64
				if (item == "arm64-v8a") {
					return (int)App.AndroidVersionArchitecture.arm64_v8a;
				}
				else if (item == "armeabi-v7a") {
					return (int)App.AndroidVersionArchitecture.armeabi_v7a;
				}
				else if (item == "x86") {
					return (int)App.AndroidVersionArchitecture.x86;
				}
				else if (item == "x86_64") {
					return (int)App.AndroidVersionArchitecture.x86_64;
				}
			}
			return 0;
		}

		public bool ResumeDownload(int id)
		{
			return DownloadHandle.ResumeDownload(id);
		}

		/*
		public void GetDownloadProgress(string imdbId, out long bytes, out long totalBytes)
		{
			
		}*/
	}
}
