using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Button), typeof(CustomButtonRenderer))]
public class CustomButtonRenderer : ButtonRenderer
{
    private Context context;
    public CustomButtonRenderer(Context context) : base(context)
    {
        this.context = context;
    }

    protected override void OnElementChanged(ElementChangedEventArgs<Button> e)
    {
        base.OnElementChanged(e);
        e.NewElement.TextColor = Color.FromHex("e6e6e6");
    }
}
