using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using CloudStreamForms.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

/*
[assembly: ExportRenderer(typeof(Picker), typeof(CustomPickerRenderer))]
namespace CloudStreamForms.Droid
{
    public class CustomPickerRenderer : PickerRenderer
    {
        private Context context;
        public CustomPickerRenderer(Context context) : base(context)
        {
            this.context = context;
        }


        protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
        {
            base.OnElementChanged(e);

            if (Control == null || e.NewElement == null) return;


            //Control.Typeface = Control.IsFocused ? Typeface.DefaultBold : Typeface.Default;
            //for example ,change the line to red:
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                Control.BackgroundTintList = ColorStateList.ValueOf(Android.Graphics.Color.ParseColor("#303F9F"));
            else
                Control.Background.SetColorFilter(Android.Graphics.Color.ParseColor("#303F9F"), PorterDuff.Mode.SrcAtop);
        }

    }
}
*/

[assembly: ExportRenderer(typeof(Picker), typeof(CustomPickerRenderer))]
namespace CloudStreamForms.Droid
{
	public class CustomPickerRenderer : PickerRenderer
	{
		private Context context;
		public CustomPickerRenderer(Context context) : base(context)
		{
			this.context = context;
		}
		/*
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            SetControlStyle();
        }*/
		protected override void OnElementChanged(ElementChangedEventArgs<Picker> e)
		{
			base.OnElementChanged(e);

			if (Control == null || e.NewElement == null) return;

			// Control.SetBackgroundColor(new Android.Graphics.Color(60, 60, 60));
			//Control.back
			//Control.Typeface = Control.IsFocused ? Typeface.DefaultBold : Typeface.Default;
			//for example ,change the line to red:
			//  SetControlStyle();
			var c = Android.Graphics.Color.Transparent;
			if (Settings.TapjackProtectionPicker) {
				Control.FilterTouchesWhenObscured = true;
			}
			if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
				Control.BackgroundTintList = ColorStateList.ValueOf(c);
			else
				Control.Background.SetColorFilter(c, PorterDuff.Mode.SrcAtop);
		}

		/*
        private void SetControlStyle()
        {
            if (Control != null) {
                Drawable imgDropDownArrow = Resources.GetDrawable(Resource.Drawable.dropdownLow);
                imgDropDownArrow.SetBounds(5, 5, 5, 5);
                imgDropDownArrow.
                Control.SetCompoundDrawablesRelativeWithIntrinsicBounds(null, null, imgDropDownArrow, null);
            }
        }*/

	}
}