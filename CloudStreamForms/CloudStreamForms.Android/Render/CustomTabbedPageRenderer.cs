using Android.Content;
using Android.Graphics;
using Android.Support.Design.Internal;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using CloudStreamForms.Droid.Renderers;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android.AppCompat;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(CustomTabbedPageRenderer))]
namespace CloudStreamForms.Droid.Renderers
{
    public class CustomTabbedPageRenderer : TabbedPageRenderer
    {
        private Context context;

        public CustomTabbedPageRenderer(Context context) : base(context)
        {
            this.context = context;
        }
        const bool line = false;

        public static bool done = false;
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) // HOMESCREEN LINE HERE
        {
            base.OnElementPropertyChanged(sender, e);
            if (done) return;
            var path = CloudStreamForms.Settings.CurrentFont.FontStylePath;
            if (path == null) return;

            //((TabbedPage)sender).bar
            var rel = (Android.Widget.RelativeLayout)ViewGroup.GetChildAt(0);

            //    var pager = (ViewPager)rel.GetChildAt(0);
            var view = (BottomNavigationView)rel.GetChildAt(1);
            var _f = (BottomNavigationMenuView)view.GetChildAt(0);
            var fontFace = Typeface.CreateFromAsset(context.Assets, path);
            for (int i = 0; i < _f.ChildCount; i++) {
                var item = (BottomNavigationItemView)_f.GetChildAt(i);
                //this shows all titles of items
                item.SetChecked(true);
                //every BottomNavigationItemView has two children, first is an itemIcon and second is an itemTitle
                var itemTitle = item.GetChildAt(1);
                //every itemTitle has two children, first is a smallLabel and second is a largeLabel. these two are type of AppCompatTextView
                ((TextView)((BaselineLayout)itemTitle).GetChildAt(0)).SetTypeface(fontFace, TypefaceStyle.Normal);
                ((TextView)((BaselineLayout)itemTitle).GetChildAt(1)).SetTypeface(fontFace, TypefaceStyle.Normal);
            }
            done = true;

            /*
            var _v = new Android.Widget.ImageView(Context) { };
            _v.SetBackgroundColor(new Android.Graphics.Color(50, 50, 50));
            var c = new LayoutParams(10000, 3);
            _v.LayoutParameters = c;
            view.AddView(_v);*/

            /*if (!line) return; 

            var rel = (Android.Widget.RelativeLayout)ViewGroup.GetChildAt(0); 
            var pager = (ViewPager)rel.GetChildAt(0);
            var view = (BottomNavigationView)rel.GetChildAt(1);
            if (view.ChildCount == 1) { 
                var _v = new Android.Widget.ImageView(Context) { };
                _v.SetBackgroundColor(new Android.Graphics.Color(50, 50, 50)); 
                var c = new LayoutParams(10000, 3);
                _v.LayoutParameters = c;
                view.AddView(_v); 
            }*/
        }
    }
}