using Acr.UserDialogs;
using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.Annotation;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Text;
using Android.Views;
using Android.Widget;
using CloudStreamForms.Core;
using LibVLCSharp.Forms.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
	public class MainIntentService : IntentService
	{
		public MainIntentService() : base("MainIntentService") { }

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
			try {
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
			catch (Exception) {

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

	/*
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
	}*/

	[BroadcastReceiver]
	public class AlertReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
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
		}
	}



	/*
	[Activity(Label = "CloudStream 2", Icon = "@drawable/bicon512", Theme = "@style/MainTheme.Splash", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation), IntentFilter(new[] { Intent.ActionView }, DataScheme = "cloudstreamforms", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable })]*/
	[Activity(Label = "CloudStream 2", Icon = "@drawable/bicon512", Theme = "@style/MainTheme.Splash", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
		ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.SmallestScreenSize | ConfigChanges.ScreenLayout  // MUST HAVE FOR PIP MODE OR ELSE IT WILL TRIGGER ONCREATE
		, SupportsPictureInPicture = true, ResizeableActivity = true),
		IntentFilter(new[] { Intent.ActionView }, Label = "Open in CloudStream", DataScheme = "cloudstreamforms", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }),
		IntentFilter(new[] { Intent.ActionView }, Label = "Open in CloudStream", DataScheme = "https", DataPathPrefix = "/title", DataHost = "www.imdb.com", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }),

		IntentFilter(new[] { Intent.ActionSend }, Label = "Download Video", Categories = new[] { Intent.CategoryDefault }, DataMimeType = "text/plain", DataHosts = new[] { "youtube.com", "youtu.be" }), // VIA SHARE
		IntentFilter(new[] { Intent.ActionView }, Label = "Download Video", Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable }, DataScheme = "https", DataHosts = new[] { "youtube.com", "youtu.be" }), // VIA LINK
	]

	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		public static MainDroid mainDroid;
		public static MainActivity activity;

		public const int REQUEST_CODE = 42;
		public const string EXTRA_POSITION_OUT = "extra_position";
		public const string EXTRA_DURATION_OUT = "extra_duration";
		public const bool LOG_STARTUP_DATA = false;

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

		public override void OnLowMemory()
		{
			try {
				App.SaveData();
				MainPage.mainPage.Navigation.PopToRootAsync();
				cachedBitmaps.Clear();
			}
			catch (Exception) { }

			base.OnLowMemory();
		}

		static bool IsFromYoutube(string data)
		{
			return data.Contains("www.youtube.com") || data.Contains("youtu.be");
		}

		static async void HandleYoutubeUrl(string url)
		{
			App.ShowToast("Downloading video");
			await YouTube.HandleDownload(url);
			/*
				string action = await ActionPopup.DisplayActionSheet("YouTube Video", "Download video", "Play video");
				if(action == "Download video") {
					await YouTube.HandleDownload(datastring);
				}
				else if(action == "Play video") {
					await App.RequestVlc(datastring, "Youtube");
				}*/
		}

		protected override async void OnNewIntent(Intent intent)
		{
			if (Settings.IS_TEST_BUILD) {
				return;
			}
			var clip = intent.ClipData;
			var datastring = intent.DataString;
			var fullData = intent.Data;
			var type = intent.Type;


			if (datastring != null) {
				print("INTENTNADADA:::" + datastring);
				print("GOT NON NULL DATA");
				if (datastring != "") {
					if (IsFromYoutube(datastring)) {
						HandleYoutubeUrl(datastring);
					}
					else if (datastring.ToLower().Contains("cloudstreamforms:")) {
						if (datastring.Contains("mallogin")) {
							CloudStreamForms.Script.MALSyncApi.AuthenticateLogin(datastring);
						}
						else if (datastring.Contains("anilistlogin")) {
							CloudStreamForms.Script.AniListSyncApi.AuthenticateLogin(datastring);
						}
						else {
							MainPage.PushPageFromUrlAndName(datastring);
						}
					}
				}
			}
			else if (clip != null) { // THIS HANDELS SHARE ACTION
				try {
					var first = clip.GetItemAt(0);
					var t = first.Text;
					if (IsFromYoutube(t)) {
						HandleYoutubeUrl(t);
					}
				}
				catch (Exception) { }
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
			LogFile($"============================== ON CREATE AT {UnixTime} ==============================");
			print("ON CREATED:::::!!!!!!!!!");

			try {
				SetTheme(Resource.Style.MainTheme_NonSplash);
				LogFile("SetTheme Done");

				PublicNot = Resource.Drawable.bicon512;

				TabLayoutResource = Resource.Layout.Tabbar;
				ToolbarResource = Resource.Layout.Toolbar;

				LogFile("Setup Done 1/2");
				base.OnCreate(savedInstanceState);
				LogFile("Setup Done 2/2");
#if DEBUG

				System.AppDomain.CurrentDomain.UnhandledException += MainPage.UnhandledExceptionTrapper;
#endif
				string data = Intent?.Data?.EncodedAuthority;

				try {
					MainPage.intentData = data;
				}
				catch (Exception) { }

				// int intHeight = (int)(Resources.DisplayMetrics.HeightPixels / Resources.DisplayMetrics.Density);
				//int intWidth = (int)(Resources.DisplayMetrics.WidthPixels / Resources.DisplayMetrics.Density);


				// ======================================= INIT =======================================

				LogFile("Starting Init");

				FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);
				Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);
				UserDialogs.Init(this);
				LibVLCSharpFormsRenderer.Init();
				XamEffects.Droid.Effects.Init();

				Xamarin.Essentials.Platform.Init(this, savedInstanceState);
				global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

				LogFile("Completed Init");

				//LocalNotificationsImplementation.NotificationIconId = PublicNot;
				MainDroid.NotificationIconId = PublicNot;

				trustEveryone();

				LogFile("Start Application");
				LoadApplication(new App());
				LogFile("Completed Startup");

				App.OnVideoStatusChanged += (o, e) => {
					UpdatePipVideostatus();
				};
				LogFile("Completed PIP");
			}
			catch (Exception _ex) {
				LogFile("ERROR LOADING APP: " + _ex);
				App.ShowToast("Error Loading App: " + _ex);
			}
			/*F
            if (!CanDrawOverlays(this)) {
                Intent intent = new Intent(Settings.ACTION_MANAGE_OVERLAY_PERMISSION, Uri.parse("package:" + getPackageName()));
                startActivityForResult(intent, 0);
            }*/

			if (Settings.IS_TEST_BUILD) {
				PlatformDep = new NullPlatfrom();
				return;
			}
			try {
				LogFile("Creating activity");

				activity = this;

				mainDroid = new MainDroid();
				LogFile("Creating Maindroid");

				mainDroid.Awake();
				LogFile("Completed Awake");

				//Typeface.CreateFromAsset(Application.Context.Assets, "Times-New-Roman.ttf");

				var datastring = Intent.DataString;
				var clip = Intent.ClipData;

				if (datastring != null) {
					print("GOT NON NULL DATA");
					if (datastring != "") {
						print("INTENTDATA::::" + datastring);
						if (datastring.Contains("www.imdb.com")) {
							string id = FindHTML(datastring + "/", "title/", "/");
							//  var _thread = mainCore.CreateThread(2);
							mainCore.StartThread("IMDb Thread", async () => {
								string json = mainCore.DownloadString($"https://v2.sg.media-imdb.com/suggestion/t/{id}.json");
								//  await Task.Delay(1000);
								Device.BeginInvokeOnMainThread(() => {
									if (json != "") {
										MainPage.PushPageFromUrlAndName(id, FindHTML(json, "\"l\":\"", "\"")); ;
									}
									else {
										App.ShowToast("Error loading imdb");
									}
								});
							});
						}
						else if (IsFromYoutube(datastring)) {
							HandleYoutubeUrl(datastring);
						}
						else {
							MainPage.PushPageFromUrlAndName(datastring);
						}
					}
				}

				LogFile("Calling RequestPermission");

				RequestPermission(this);
				LogFile("Permissions Loaded");

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
				LogFile("OnLoad Completed");


				//ResumeIntentData();
				LogFile("Resume intents completed");

				StartService(new Intent(BaseContext, typeof(OnKilledService)));
				LogFile("OnKilled completed");

				Window.SetSoftInputMode(Android.Views.SoftInput.AdjustNothing);
				LogFile("OnSetsoft completed");
			}
			catch (Exception _ex) {
				LogFile("FATAL ERROR : " + _ex);
				error(_ex);
			}
			LogFile($"============================== STARTUP DONE AT {UnixTime} ==============================");

			// TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
			//  Android.Renderscripts.ta
			// var bar = new Xamarin.Forms.Platform.Android.TabbedRenderer();//.Platform.Android.

			//ShowBlackToast("Yeet", 3);
			// DownloadHandle.ResumeIntents();
			//   ShowLocalNot(new LocalNot() { mediaStyle = false, title = "yeet", data = "", progress = -1, showWhen = false, autoCancel = true, onGoing = false, id = 1234, smallIcon = Resource.Drawable.bicon, body = "Download ddddd" }, Application.Context);

			// ShowLocalNot(new LocalNot() { mediaStyle = false, title = "yeet", autoCancel = true, onGoing = false, id = 123545, smallIcon = Resource.Drawable.bicon, body = "Download Failed!",showWhen=false }); // ((e.Cancelled || e.Error != null) ? "Download Failed!"
		}


		#region ================================================ PICTURE IN PICTURE ================================================

		//https://github.com/bobby5892/235AM-Android/blob/dda3cc85f8345902cf96ccf437ba7fc3001a04e6/Xam-Examples/android-o/PictureInPicture/PictureInPicture/MainActivity.cs


		public PictureInPictureParams.Builder pictureInPictureParamsBuilder;

		public bool ShouldShowPictureInPicture()
		{
			return Settings.PictureInPicture && currentVideoStatus.isInVideoplayer; // && currentVideoStatus.isLoaded;
		}

		//https://stackoverflow.com/questions/52594181/how-to-know-if-user-has-disabled-picture-in-picture-feature-permission
		//https://developer.android.com/guide/topics/ui/picture-in-picture
		public bool CanShowPictureInPicture()
		{
			try {
				var appMang = Application.Context.GetSystemService(Context.AppOpsService) as AppOpsManager;
				return Build.VERSION.SdkInt >= BuildVersionCodes.O &&  // ANDROID OVER PIP UPDATE
					PackageManager.HasSystemFeature(PackageManager.FeaturePictureInPicture) && // HAS FEATURE, MIGHT BE BLOCKED DUE TO PROWER DRAIN
					appMang.CheckOp(AppOpsManager.OpstrPictureInPicture, Android.OS.Process.MyUid(), PackageName) == AppOpsManagerMode.Allowed; // CHECK IF FEATURE IS ENABLED IN SETTINGS
			}
			catch (Exception) {
				return false; // JUST IN CASE (CheckOp)
			}
		}

		void UpdatePipVideostatus()
		{
			try {
				if (App.IsPictureInPicture) {
					if (App.currentVideoStatus.isPaused) {
						UpdatePictureInPictureActions(Resource.Drawable.netflixPlay128v2, "Play", (int)App.PlayerEventType.Play);
					}
					else {
						UpdatePictureInPictureActions(Resource.Drawable.netflixPause128v2, "Pause", (int)App.PlayerEventType.Pause);
					}
				}
			}
			catch (Exception) {
			}
		}

		private void EnterPipMode()
		{
			if (!ShouldShowPictureInPicture()) return;

			try {
				if (App.FullPictureInPictureSupport) {
					App.OnPictureInPictureModeChanged?.Invoke(null, true);

					if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
						//Rational rational = new Rational(450, 250);
						PictureInPictureParams.Builder builder = new PictureInPictureParams.Builder();
						//builder.SetAspectRatio(rational);
						EnterPictureInPictureMode(builder.Build());
					}
					else {
						var param = new PictureInPictureParams.Builder().Build();
						EnterPictureInPictureMode(param);
					}

					new Handler().PostDelayed(CheckPipPermission, 30);
				}
			}
			catch (Exception e) {
				error(e);
			}
		}

		private void CheckPipPermission()
		{
			try {
				if (!IsInPictureInPictureMode) {
					App.OnPictureInPictureModeChanged?.Invoke(null, false);
					OnBackPressed();
				}
			}
			catch (Exception) { }
		}

		public void ShowPictureInPicture()
		{
			try {
				EnterPipMode();
			}
			catch (Exception) { }
		}

		BroadcastReceiver receiver;

		/// <summary>Updat
		/// Update the state of pause/resume action item in Picture-in-Picture mode.
		/// </summary>
		/// <param name="iconId">The icon to be used.</param>
		/// <param name="title">The title text.</param>
		/// <param name="controlType">The type of action.</param>
		/// <param name="requestCode">The request code for the pending intent.</param>
		public void UpdatePictureInPictureActions([DrawableRes] int iconId, string title, int controlType)
		{
			try {
				var actions = new List<RemoteAction>();

				// This is the PendingIntent that is invoked when a user clicks on the action item.
				// You need to use distinct request codes for play and pause, or the PendingIntent won't
				// be properly updated.
				PendingIntent GetPen(int code)
				{
					return PendingIntent.GetBroadcast(this, code, new Intent(Constants.ACTION_MEDIA_CONTROL).PutExtra(Constants.EXTRA_CONTROL_TYPE, code), 0);
				}

				PendingIntent intent = GetPen(controlType);

				Icon icon = Icon.CreateWithResource(this, iconId);

				var context = Application.Context;
				if (App.currentVideoStatus.isLoaded) {
					actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.netflixSkipMobileBackEmpty), "Back", "Seek Back", GetPen((int)App.PlayerEventType.SeekBack)));
					actions.Add(new RemoteAction(icon, title, title, intent));

					if (App.currentVideoStatus.shouldSkip) {
						actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.baseline_skip_next_white_48dp), "Skip", "Skip", GetPen((int)App.PlayerEventType.SkipCurrentChapter)));
					}
					else {
						actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.netflixSkipMobileEmpty), "Forward", "Seek Forward", GetPen((int)App.PlayerEventType.SeekForward)));
					}
				}
				else {
					actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.baseline_skip_previous_white_48dp), "Previous Mirror", "Previous Mirror", GetPen((int)App.PlayerEventType.PrevMirror)));
					//actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.baseline_stop_white_48dp), "Stop", "Stop", GetPen((int)App.PlayerEventType.Stop)));
					actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.baseline_skip_next_white_48dp), "Next Mirror", "Next Mirror", GetPen((int)App.PlayerEventType.NextMirror)));
				}
				// MAX 3 ACTIONS
				/*if (App.currentVideoStatus.hasNextEpisode) {
                    actions.Add(new RemoteAction(Icon.CreateWithResource(context, Resource.Drawable.baseline_skip_next_white_48dp), "Next", "Next Episode", GetPen((int)App.PlayerEventType.NextEpisode)));
                }*/

				pictureInPictureParamsBuilder.SetActions(actions).Build();

				SetPictureInPictureParams(pictureInPictureParamsBuilder.Build());
			}
			catch (Exception) {
			}
		}

		public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration newConfig)
		{
			base.OnPictureInPictureModeChanged(isInPictureInPictureMode, newConfig);
			try {
				App.IsPictureInPicture = isInPictureInPictureMode;

				if (!isInPictureInPictureMode) {
					// Android.App.Application.Context.StartActivity(new Intent(MainActivity.activity.ApplicationContext, typeof(MainActivity)).AddFlags(ActivityFlags.ReorderToFront | ActivityFlags.NewTask));

				}

				//https://docs.microsoft.com/en-us/samples/xamarin/monodroid-samples/android-o-pictureinpicture/
				if (isInPictureInPictureMode) {
					UpdatePipVideostatus();
					// Starts receiving events from action items in PiP mode.
					receiver = new PIPBroadcastReceiver(this);
					RegisterReceiver(receiver, new IntentFilter(Constants.ACTION_MEDIA_CONTROL));
				}
				else {
					//  We are out of PiP mode. We can stop receiving events from it.
					UnregisterReceiver(receiver);
					receiver = null;

					//   Show the video controls if the video is not playing
					/*if (PIPMovieView != null && !PIPMovieView.IsPlaying) {
                        PIPMovieView.ShowControls();
                     }*/
				}
			}
			catch (Exception) {
			}
		}
		#endregion

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

		public void LogFile(string data, bool nextLine = true)
		{
			if (!LOG_STARTUP_DATA) return;
			string path = GetPath(true, "/cloudstreamlog.txt");
			var file = new Java.IO.File(path);
			if (!file.Exists()) {
				file.CreateNewFile();
			}
			Java.IO.FileWriter writer = new Java.IO.FileWriter(file, true);
			writer.Write(data + (nextLine ? "\n" : ""));
			writer.Flush();
			writer.Close();
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
			/*
            if (MainActivity.activity.ShouldShowPictureInPicture()) {
                activity.ShowPictureInPicture();
            }
            else {*/
			//     }
			//if (!App.IsPictureInPicture) {
			App.OnAppNotInForground?.Invoke(null, EventArgs.Empty);
			//}
			base.OnStop();
		}

		protected override void OnPause()
		{
			base.OnPause();
		}

		protected override void OnUserLeaveHint()
		{
			base.OnUserLeaveHint();
			/*  if (MainActivity.activity.ShouldShowPictureInPicture()) {
                  activity.ShowPictureInPicture();
              }*/
		}

		protected override void OnRestart()
		{
			base.OnRestart();
			//  if (!App.IsPictureInPicture) {
			if (!App.IsPictureInPicture) {
				App.OnAppReopen?.Invoke(null, EventArgs.Empty);
			}
			//  }
		}

		protected override void OnResume()
		{
			base.OnResume();
			if (!App.IsPictureInPicture) {
				OnAppResume?.Invoke(null, EventArgs.Empty);
			}
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
				Manifest.Permission.WriteExternalStorage, Manifest.Permission.RequestInstallPackages,Manifest.Permission.InstallPackages,Manifest.Permission.WriteSettings,  //Manifest.Permission.Bluetooth
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
			return (mainPath ? (Android.OS.Environment.ExternalStorageDirectory + "/" + Android.OS.Environment.DirectoryDownloads) : Android.OS.Environment.ExternalStorageDirectory + "/") + extraPath;
		}

		public static void ShowBlackToast(string msg, double duration)
		{
			Device.BeginInvokeOnMainThread(() => {
				ToastLength toastLength = ToastLength.Short;
				if (duration >= 3) {
					toastLength = ToastLength.Long;
				}
				Toast toast = Toast.MakeText(Application.Context, Html.FromHtml("<font color='#ffffff' >" + msg + "</font>"), toastLength);
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
		public void TryToDispose()
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
		public void PictureInPicture()
		{
			MainActivity.activity.ShowPictureInPicture();
			//.SetActions(new List<RemoteAction>() { new RemoteAction() { } })
			// TESTING STUFF
			//  var _b = new PictureInPictureParams.Builder().Build();
			//  MainActivity.activity.EnterPictureInPictureMode(_b);
		}


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
		private const long AUDIO_LATENCY_NOT_ESTIMATED = long.MinValue + 1;

		/**
         * The audio latency default value if we cannot estimate it
         */
		private const long DEFAULT_AUDIO_LATENCY = 100L * 1000L * 1000L; // 100ms

		private static long FramesToNanoSeconds(long frames)
		{
			return frames * 1000000000L / 16000;
		}

		private static long NanoTime()
		{
			long nano = 10000L * Stopwatch.GetTimestamp();
			nano /= TimeSpan.TicksPerMillisecond;
			nano *= 100L;
			return nano;
		}

		// Source: https://stackoverflow.com/a/52559996/497368
		private long GetDelay()
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
					long frameTimeDelta = FramesToNanoSeconds(frameIndexDelta);
					long nextFramePresentationTime = audioTimestamp.NanoTime + frameTimeDelta;

					// Assume that the next frame will be written at the current time
					long nextFrameWriteTime = NanoTime();

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

		public void ShowNotIntentAsync(string title, string body, int id, string titleId, string titleName, DateTime? time = null, string bigIconUrl = "")
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

		readonly static MediaSession mediaSession = new MediaSession(Application.Context, "Chromecast");

		public static async void UpdateChromecastNotification(string title, string body, bool isPaused, string poster)
		{
			try {
				var builder = new Notification.Builder(Application.Context);
				builder.SetContentTitle(title);
				builder.SetContentText(body);
				builder.SetAutoCancel(false);

				builder.SetSmallIcon(Resource.Drawable.biconWhite512);//LocalNotificationIconId);
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

		// static bool hidden = false;
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
				//hidden = true;

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

		public static void OpenVlc(Activity _activity, int requestId, Android.Net.Uri uri, long time, String title, string subfile = "")
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
					string writeData = CloudStreamForms.App.ConvertPathAndNameToM3U8(urls, names, subtitlesEnabled, "content://" + absolutePath + "/");
					WriteFile(CloudStreamForms.App.baseM3u8Name, absolutePath, writeData);

					if (subtitlesEnabled) {
						print("WRITING SUBFILE: " + absolutePath + "|" + baseSubtitleName + "|" + subtitleFull + "|" + (subtitleFull == "") + "|" + (subtitleFull == null));
						subFile = WriteFile(CloudStreamForms.App.baseSubtitleName, absolutePath, subtitleFull);
					}

					_bpath = absolutePath + "/" + CloudStreamForms.App.baseM3u8Name;
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
#if DEBUG
			TestAwake();
#endif
			//MainDelayTest();
			// long delay = getDelay();

			//  print("MAIN DELAYYYY::: " + delay); 
		}

#if DEBUG
		ActivityManager activityManager => Application.Context.GetSystemService(Context.ActivityService) as ActivityManager;
#endif
		readonly static Stopwatch mainS = new Stopwatch();
		async void MainDelayTest()
		{
#if DEBUG

			await Task.Delay(5000);
			ActivityManager.MemoryInfo? mi = new ActivityManager.MemoryInfo();
			activityManager.GetMemoryInfo(mi);
			double availableMegs = mi.AvailMem / 0x100000L;

			//Percentage can be calculated for API 16+
			double percentAvail = mi.AvailMem / (double)mi.TotalMem * 100.0;
			long f1 = mainS.ElapsedMilliseconds;
			App.ShowToast("GG: " + (int)percentAvail + "MEGS: " + availableMegs + " | ");
			/*
			Device.BeginInvokeOnMainThread(() => {

				long f2 = mainS.ElapsedMilliseconds;
				App.ShowToast("SHOW: " + (f2 - f1));
			});*/
			//MainDelayTest();
#endif
		}

		async void TestAwake()
		{
			await Task.Delay(4000);
			//	DownloadHandle.ParseM3u8("https://pl.crunchyroll.com/evs1/2a397fdeaa9a0b59d8d5d9e24cda6420/assets/ca303e340e0f0169b8e8ba682855f2da_,3865381.mp4,3865382.mp4,3865380.mp4,3865379.mp4,3865378.mp4,.urlset/master.m3u8?Policy=eyJTdGF0ZW1lbnQiOlt7IlJlc291cmNlIjoiaHR0cCo6Ly9wbC5jcnVuY2h5cm9sbC5jb20vZXZzMS8yYTM5N2ZkZWFhOWEwYjU5ZDhkNWQ5ZTI0Y2RhNjQyMC9hc3NldHMvY2EzMDNlMzQwZTBmMDE2OWI4ZThiYTY4Mjg1NWYyZGFfLDM4NjUzODEubXA0LDM4NjUzODIubXA0LDM4NjUzODAubXA0LDM4NjUzNzkubXA0LDM4NjUzNzgubXA0LC51cmxzZXQvbWFzdGVyLm0zdTgiLCJDb25kaXRpb24iOnsiRGF0ZUxlc3NUaGFuIjp7IkFXUzpFcG9jaFRpbWUiOjE1OTk4MzQ2MDd9fX1dfQ__&Signature=UxXK9xHvYtoRhlgqCwbalv2USxrvwkiDYWGYWMwj6pyAzDj35PYrgNGFMHQcij7MQxzwd0Lmlxhcs98muF0u~wdhxbKOZ2vIJTXcjblRvf3dVKbEnwRgzOV3zhWobK2nGmFqC2SYXrGigdHYl2qxh1LvtHC-hTKenAWenRRIoRu9~1nQSmFqRDz0KqBbVMRcHlfCBISJUctYXcEWLrTQGzwZvz08o5N7Q-6Hf1~Qejz7n2bAlf1-ITJobH7LQIXIdUzQJ3EIkd1aIEJY~i36l3DgmGrATuQEbaAQh0G5VTFSgfcUtWDU7t9iJfcEOHVZ7YiLr6UyEGRH4bOPOlPGDg__&Key-Pair-Id=APKAJMWSQ5S7ZB3MF5VA", "");
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
				using WebClient wc = new WebClient();
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
							App.ShowNotIntent(toast, body, rng.Next(0, 100000), "", "");
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
			catch (Exception) {
				App.ShowToast("Download Failed");
				return "";
			}

			return GetPath(mainPath, extraPath) + "/" + CensorFilename(fileName);
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
			catch (Exception _ex) {
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

		public string DownloadFile(string path, string data)
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

		public void DownloadUpdate(string update, string version)
		{
			throw new NotImplementedException();
		}

		public string DownloadUrl(string url, string fileName, bool mainPath, string extraPath, string toast = "", bool isNotification = false, string body = "")
		{
			return "";
		}

		public bool GainAudioFocus()
		{
			return true;
		}

		public int GetArchitecture()
		{
			throw new NotImplementedException();
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

		public void PictureInPicture()
		{
			throw new NotImplementedException();
		}

		public void PlayExternalApp(VideoPlayer player)
		{
			throw new NotImplementedException();
		}


		public string ReadFile(string fileName, bool mainPath, string extraPath)
		{
			throw new NotImplementedException();
		}

		public string ReadFile(string path)
		{
			return "";
		}

		public void ReleaseAudioFocus()
		{
		}

		public void RequestVlc(List<string> urls, List<string> names, string episodeName, string episodeId, long startId = -2, string subtitleFull = "", VideoPlayer preferedPlayer = VideoPlayer.VLC, bool generateM3u8 = true)
		{
		}

		public bool ResumeDownload(int id)
		{
			throw new NotImplementedException();
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
