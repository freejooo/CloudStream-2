using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Views;
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
				bool isBg = e.NewElement.ClassId == "BGBLACK";
				if (!Settings.SubtitlesHasOutline && isBg) {
					e.NewElement.Opacity = 0;
				};

				StrokeTextView2 strokeTextView = new StrokeTextView2(context) {
					Text = e.NewElement.Text,
					ShadowStr = (!isBg && Settings.SubtitlesHasDropShadow) ? Settings.SubtitlesShadowStrenght : 1,
				};

				strokeTextView.SetTypeface(Control.Typeface, Settings.SubtitlesHasOutline ? TypefaceStyle.Bold : TypefaceStyle.Normal);
				if (Settings.SubtitlesHasDropShadow && !isBg) {
					float offset = Settings.SubtitlesOutlineIsCentered ? 0 : 5f;
					strokeTextView.SetShadowLayer(5f, offset, offset, Android.Graphics.Color.Black);
				}
				strokeTextView.SetTextSize(Android.Util.ComplexUnitType.Dip, Settings.SubtitlesSize);
				strokeTextView.TextAlignment = Android.Views.TextAlignment.Center;
				//strokeTextView.BreakStrategy = Android.Text.BreakStrategy.;
				strokeTextView.Gravity = GravityFlags.CenterHorizontal | GravityFlags.Bottom;

				if (isBg) {
					strokeTextView.SetTextColor(Android.Graphics.Color.Black);
					TextPaint tp1 = strokeTextView.Paint;
					tp1.StrokeWidth = (Settings.SubtitlesSize / 5.4f); //3f;                    
					tp1.SetStyle(Paint.Style.Stroke);
				}
				else {
					strokeTextView.SetTextColor(Android.Graphics.Color.White);
				}

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