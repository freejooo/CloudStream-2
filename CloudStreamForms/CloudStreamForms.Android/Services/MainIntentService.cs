using Android.App;
using Android.Content;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms.Droid.Services
{

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
}