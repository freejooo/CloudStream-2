using Android.Content;
using Android.Widget;
using CloudStreamForms;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Button = Xamarin.Forms.Button;

[assembly: ExportRenderer(typeof(Button), typeof(CustomButtonRenderer))]
public class CustomButtonRenderer : ButtonRenderer
{
    private readonly Context context;
    public CustomButtonRenderer(Context context) : base(context)
    {
        this.context = context;
    }

    protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
    {
        base.OnElementChanged(e);
        if (e.NewElement.ClassId != "CUST") {
            e.NewElement.TextColor = Color.FromHex("e6e6e6");
        }

        if (e.NewElement.FontFamily == null) {
            e.NewElement.FontFamily = CloudStreamForms.Settings.CurrentFont.FontStyle;
        }

        if (Settings.TapjackProtectionButton) {
            Control.FilterTouchesWhenObscured = true;
        }
    }
}
