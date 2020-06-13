using Android.Content;
using CloudStreamForms.Droid;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Xamarin.Forms.ProgressBar), typeof(CustomProgressBarRenderer))]
namespace CloudStreamForms.Droid
{
    public class CustomProgressBarRenderer : ProgressBarRenderer
    {
        private Context context;
        public CustomProgressBarRenderer(Context context) : base(context)
        {
            this.context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.ProgressBar> e)
        {
            base.OnElementChanged(e);
            Console.WriteLine("PROGRESSTO::" + e.NewElement.ClassId);
            if (e.NewElement.ClassId == "id") {
                Control.Indeterminate = true;
            }
        }
    }
}