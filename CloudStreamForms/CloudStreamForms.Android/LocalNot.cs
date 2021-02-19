using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using CloudStreamForms.Droid.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using static CloudStreamForms.Droid.MainActivity;

namespace CloudStreamForms.Droid
{


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

		public static NotificationManager NotManager => (NotificationManager)Application.Context.GetSystemService(Context.NotificationService);
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
				NotManager.CreateNotificationChannel(channel);

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
					builder.SetStyle(b);
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
				stackBuilder.AddNextIntent(resultIntent);

				builder.SetContentIntent(GetCurrentPending());
			}

			try {
				NotManager.Notify(not.id, builder.Build());
			}
			catch { }
		}
		public static Intent GetLauncherActivity(Context context = null)
		{
			var cc = context ?? Application.Context;
			var packageName = cc.PackageName;
			return cc.PackageManager.GetLaunchIntentForPackage(packageName).SetPackage(null);
		}
	}
}