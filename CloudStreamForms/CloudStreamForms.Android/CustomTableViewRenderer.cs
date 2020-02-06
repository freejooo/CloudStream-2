using Android.Content;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(TableView), typeof(CustomTableViewRenderer))]
public class CustomTableViewRenderer : TableViewRenderer
{
    private Context context;
    public CustomTableViewRenderer(Context context) : base(context)
    {
        this.context = context;
    }


    protected override void OnElementChanged(ElementChangedEventArgs<TableView> e)
    {
        System.Console.WriteLine("daaaaaaaaaaaaaaaaaaaaaaaa--");
        base.OnElementChanged(e);
        if (Control == null)
            return;
        var listView = Control as global::Android.Widget.ListView;
        listView.DividerHeight = 3;

        listView.Divider.SetAlpha(0);
        //  listView.Focusable = false;

        listView.VerticalScrollBarEnabled = CloudStreamForms.Settings.HasScrollBar;

    }
}
