using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace CloudStreamForms.Droid.Services
{
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
		}
	}
}