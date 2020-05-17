using Android.Content;
using Android.Graphics;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V4.View;
using Android.Views;
using CloudStreamForms;
using System.ComponentModel;
using CloudStreamForms.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppCompat;
using Color = Xamarin.Forms.Color;
using Android.Graphics.Drawables.Shapes;

[assembly: ExportRenderer(typeof(TabbedPage), typeof(CustomTabbedPageRenderer))]
namespace CloudStreamForms.Droid.Renderers
{ 
    public class CustomTabbedPageRenderer : TabbedPageRenderer
    {
     
        public CustomTabbedPageRenderer(Context context) : base(context) { }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e) // HOMESCREEN LINE HERE
        {
            base.OnElementPropertyChanged(sender, e);
            //System.Console.WriteLine("sender::::" + sender.ToString());
            // System.Console.WriteLine(ViewGroup.GetChildAt(1).ToString());

            var rel = (Android.Widget.RelativeLayout)ViewGroup.GetChildAt(0);
            /*for (int i = 0; i < rel.ChildCount; i++) {
                System.Console.WriteLine(i + "S::S:S:S::S:S:" + rel.GetChildAt(i));
            }*/
            var pager = (ViewPager)rel.GetChildAt(0);
            var view = (BottomNavigationView)rel.GetChildAt(1);
            if (view.ChildCount == 1) {

                // rel.SetBackgroundColor(Android.Graphics.Color.Blue);
                var _v = new Android.Widget.ImageView(Context) { };
                _v.SetBackgroundColor(new Android.Graphics.Color(50, 50, 50));
                //  _v.Text = "HELLLOO";
                var c = new LayoutParams(10000, 3);
                _v.LayoutParameters = c;
                view.AddView(_v);
                ////            _v.SetHeight(2);
                //rel.AddView(_v);
            } 
        }
    }
}