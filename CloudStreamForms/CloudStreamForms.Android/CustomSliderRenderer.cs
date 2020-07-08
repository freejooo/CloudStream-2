using Android.Content;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

//[assembly: ExportFont("Lobster-Regular.ttf", Alias = "Lobster")]
[assembly: ExportRenderer(typeof(Slider), typeof(CustomSliderRenderer))]
public class CustomSliderRenderer : SliderRenderer
{
    private Context context;

    public CustomSliderRenderer(Context context) : base(context)
    {
        this.context = context;
    }

    protected override void OnElementChanged(ElementChangedEventArgs<Slider> e)
    {
        base.OnElementChanged(e);
        if (Control == null)
            return;
        var seek = Control as global::Android.Widget.SeekBar;
        //seek.Thumb.SetBounds(100, 100, 100, 100);

        int size = e.NewElement.ClassId == "big" ? 70 : 40;

        ShapeDrawable th = new ShapeDrawable(new OvalShape());
        th.SetIntrinsicWidth(size);
        th.SetIntrinsicHeight(size);
        th.SetColorFilter(e.NewElement.ThumbColor.ToAndroid(), Android.Graphics.PorterDuff.Mode.SrcOver);
        seek.SetThumb(th);
        //print("SEEK;::::::" + seek);
    }
}

