using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Label), typeof(CustomLabelRenderer))]
public class CustomLabelRenderer : LabelRenderer
{
    private Context context;
    public CustomLabelRenderer(Context context) : base(context)
    {
        this.context = context;
    }

    protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
    {
        base.OnElementChanged(e);
        if (Control == null)
            return;
        var tv = Control as global::Android.Widget.TextView;
        tv.VerticalScrollBarEnabled = false;

    }
}
