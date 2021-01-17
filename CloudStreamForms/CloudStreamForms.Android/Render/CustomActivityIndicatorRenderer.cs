using Android.Content;
using Android.Views;
using Android.Widget;
using CloudStreamForms;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(CustomActivityIndicator), typeof(CustomActivityIndicatorRenderer))]
public class CustomActivityIndicatorRenderer : ActivityIndicatorRenderer
{
	private readonly Context context;
	public CustomActivityIndicatorRenderer(Context context) : base(context)
	{
		this.context = context;
	}

	protected override void OnElementChanged(ElementChangedEventArgs<ActivityIndicator> e)
	{
		base.OnElementChanged(e);
		int.TryParse(e.NewElement.ClassId, out int internalId);

		App.onExtendedButtonPressed += (object o, int id) => {
			if (internalId == id) {
				PopupMenu p = new PopupMenu(context, Control);
				var items = new string[] { "Play file", "Delete file" };
				foreach (var item in items) {
					p.Menu.Add(item);
				}
				p.Show();
			}
		};
	}
}
