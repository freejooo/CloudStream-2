using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CloudStreamForms.Droid.Render;
using CloudStreamForms.Render;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(OutLineLabel), typeof(MyOutLineTextView))]
namespace CloudStreamForms.Droid.Render
{
	public class MyOutLineTextView : LabelRenderer
	{
		Context context;
		public MyOutLineTextView(Context context) : base(context)
		{
			this.context = context;
		}

		protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
		{
			base.OnElementChanged(e);

			if (Control != null) {

				StrokeTextView2 strokeTextView = new StrokeTextView2(context) {
					Text = e.NewElement.Text,
				};

				strokeTextView.SetTypeface(Control.Typeface, TypefaceStyle.Normal);
				strokeTextView.SetShadowLayer(5f, 0, 0, Android.Graphics.Color.Black);
				strokeTextView.SetTextSize(Android.Util.ComplexUnitType.Dip, (float)e.NewElement.FontSize);
				strokeTextView.TextAlignment = Android.Views.TextAlignment.Center;
				//strokeTextView.BreakStrategy = Android.Text.BreakStrategy.;
				strokeTextView.Gravity = GravityFlags.CenterHorizontal | GravityFlags.Bottom;

				Control.AfterTextChanged += (_o, _e) => {
					var _txt = e.NewElement.Text;
					strokeTextView.Text = _txt;
					//strokeTextView.ChangeText();
				};

					SetNativeControl(strokeTextView);
				//strokeTextView.SetTextColor(Android.Graphics.Color.Purple);
			}
		}
	}
}