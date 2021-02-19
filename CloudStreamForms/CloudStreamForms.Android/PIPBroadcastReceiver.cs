using Android.Content;

namespace CloudStreamForms.Droid
{
	public class PIPBroadcastReceiver : BroadcastReceiver
	{
		//readonly MainActivity self;

		public PIPBroadcastReceiver() // MainActivity self
		{
			//this.self = self;
		}

		public override void OnReceive(Context context, Intent intent)
		{
			if (intent == null || Constants.ACTION_MEDIA_CONTROL != intent.Action) {
				return;
			}

			// This is where we are called back from Picture-in-Picture action items.
			int controlType = intent.GetIntExtra(Constants.EXTRA_CONTROL_TYPE, 0);
			App.OnRemovePlayAction?.Invoke(null, (App.PlayerEventType)controlType);
		}
	}
}