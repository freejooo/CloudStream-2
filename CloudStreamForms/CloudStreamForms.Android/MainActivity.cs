using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.Annotation;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V4.Media.Session;
using Android.Views;
using CloudStreamForms.Core;
using CloudStreamForms.Droid.Services;
using LibVLCSharp.Forms.Shared;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Droid.LocalNot;
using static CloudStreamForms.Droid.MainHelper;
using Application = Android.App.Application;

namespace CloudStreamForms.Droid
{
	// THIS IS USED TO SAVE ALL PROGRESS WHEN THE APP IS KILLED
	public class BecomingNoisyReceiver : BroadcastReceiver // WHEN REMOVE HEADPHONES
	{
		public override void OnReceive(Context context, Intent intent)
		{
			if (AudioManager.ActionAudioBecomingNoisy == intent.Action) {
				OnAudioFocusChanged?.Invoke(null, false);
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

	[Activity(Label = "CloudStream 2", Name = "com.CloudStreamForms.CloudStreamForms.MainActivity", Icon = "@mipmap/ic_launcher", Theme = "@style/MainTheme.Splash", LaunchMode = LaunchMode.SingleTop,
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
		}

		protected override void OnNewIntent(Intent intent)
		{
			if (Settings.IS_TEST_BUILD) {
#pragma warning disable CS0162 // Unreachable code detected
				return;
#pragma warning restore CS0162 // Unreachable code detected
			}
			var clip = intent.ClipData;
			var datastring = intent.DataString;
			//var fullData = intent.Data;
			//var type = intent.Type;

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
		MediaSessionCompat mediaSession;

		public class MediaSessionCallback : MediaSessionCompat.Callback
		{
			public override bool OnMediaButtonEvent(Intent mediaButtonEvent)
			{
				try {
					if (!(mediaButtonEvent.GetParcelableExtra(Intent.ExtraKeyEvent) is KeyEvent keyEvent)) {
						return false;
					}
					switch (keyEvent.KeyCode) {
						case Keycode.MediaPlay:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Play);
							break;
						case Keycode.MediaPause:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Pause);
							break;
						case Keycode.MediaNext:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SkipCurrentChapter);
							break;
						case Keycode.MediaSkipForward:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SeekForward);
							break;
						case Keycode.MediaSkipBackward:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SeekBack);
							break;
						case Keycode.MediaPlayPause:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SeekForward);
							break;
						case Keycode.MediaStop:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Stop);
							break;
						case Keycode.Headsethook:
							App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Pause);
							break;
						default:
							break;
					}

					print("EVENT:" + keyEvent);
				}
				catch (Exception) {
					return false;
				}

				return base.OnMediaButtonEvent(mediaButtonEvent);
			}

			public override void OnPause()
			{
				App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Pause);
				base.OnPause();
			}

			public override void OnPlay()
			{
				App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.Play);

				base.OnPlay();
			}

			public override void OnFastForward()
			{
				App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SeekForward);

				base.OnFastForward();
			}

			public override void OnRewind()
			{
				App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SeekBack);

				base.OnRewind();
			}

			public override void OnSkipToNext()
			{
				App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.SkipCurrentChapter);

				base.OnSkipToNext();
			}

			public override void OnSkipToPrevious()
			{
				//App.OnRemovePlayAction?.Invoke(null, App.PlayerEventType.PrevMirror);

				base.OnSkipToPrevious();
			}
		}

		public AudioManager AudManager => Application.Context.GetSystemService(Context.AudioService) as AudioManager;

		void CreateMediaSession()
		{
			mediaSession = new MediaSessionCompat(this, "CloudStream 2");
			mediaSession.SetFlags((int)(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls));
			mediaSession.SetCallback(new MediaSessionCallback());
		}

		protected override void OnCreate(Bundle savedInstanceState)
		{
			LogFile($"============================== ON CREATE AT {UnixTime} ==============================");
			print("ON CREATED:::::!!!!!!!!!");
			CreateMediaSession();

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

				// ======================================= INIT =======================================

				LogFile("Starting Init");

				FFImageLoading.Forms.Platform.CachedImageRenderer.Init(enableFastRenderer: true);
				Rg.Plugins.Popup.Popup.Init(this);
				LibVLCSharpFormsRenderer.Init();
				XamEffects.Droid.Effects.Init();

				Xamarin.Essentials.Platform.Init(this, savedInstanceState);
				global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

				LogFile("Completed Init");

				//LocalNotificationsImplementation.NotificationIconId = PublicNot;
				MainDroid.NotificationIconId = PublicNot;

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
				error(_ex);
			}

			if (Settings.IS_TEST_BUILD) {
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
							mainCore.StartThread("IMDb Thread", () => {
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
				//GetLatency(); // https://developer.amazon.com/docs/fire-tv/audio-video-synchronization.html#section1-2

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
					receiver = new PIPBroadcastReceiver();
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
#pragma warning disable CS0162, IDE0060 // Unreachable code detected

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
#pragma warning restore CS0162, IDE0060 // Unreachable code detected

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
			UnregisterReceiver(becomingNoisyReceiver); // AudioManager.ActionHeadsetPlug 

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

		BecomingNoisyReceiver becomingNoisyReceiver;

		protected override void OnResume()
		{
			base.OnResume();
			becomingNoisyReceiver = new BecomingNoisyReceiver();
			RegisterReceiver(becomingNoisyReceiver, new IntentFilter(AudioManager.ActionAudioBecomingNoisy)); // AudioManager.ActionHeadsetPlug 

			if (!App.IsPictureInPicture) {
				OnAppResume?.Invoke(null, EventArgs.Empty);
			}
		}

		//https://github.com/dot42/api/blob/b6bb6cf9ed9b4a548c788b19abb6134d5b9d4b3b/Generated/v2.1/Android.Media.cs#L1490
		//https://github.com/google/ExoPlayer/blob/b5beb32618ac99adc58b537031a6f7c3dd761b9a/library/core/src/main/java/com/google/android/exoplayer2/audio/AudioTrackPositionTracker.java#L172
		/*void GetLatency()
		{
			try {
				var method = typeof(AudioTrack).GetMethod("getLatency");

				var res = method.Invoke(null, null);
				print(res);
			}
			catch (Exception _ex) {
				error(_ex);
			}
		}*/


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
				Manifest.Permission.WriteExternalStorage, Manifest.Permission.RequestInstallPackages,Manifest.Permission.InstallPackages,Manifest.Permission.WriteSettings, Manifest.Permission.InstallShortcut,  //Manifest.Permission.Bluetooth
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
	}
}
