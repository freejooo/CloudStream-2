using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

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