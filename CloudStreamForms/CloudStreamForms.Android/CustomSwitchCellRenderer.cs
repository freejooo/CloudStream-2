using Android.Graphics;
using Android.Widget;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(SwitchCell), typeof(CustomSwitchCellRenderer))]
public class CustomSwitchCellRenderer : SwitchCellRenderer
{
    protected override void OnCellPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        base.OnCellPropertyChanged(sender, args);  
    }

    protected override Android.Views.View GetCellCore(Cell item, Android.Views.View convertView, Android.Views.ViewGroup parent, Android.Content.Context context)
    {
        var cell = base.GetCellCore(item, convertView, parent, context);
        cell.FilterTouchesWhenObscured = true; 
        cell.SetBackgroundColor(new Android.Graphics.Color(20, 20, 20)); 
        try {
            Android.Widget.Switch child0 = (Android.Widget.Switch)((LinearLayout)cell).GetChildAt(2);
            child0.LayoutChange += (o, e) => { 
                SetColorOfToggle(o);

            };
            child0.Click += (o, e) => { 
                SetColorOfToggle(o);
            };

        }
        catch (Exception) {

        } 
        return cell;
    }

    void SetColorOfToggle(object o)
    {
        try {
            var _c = ((Android.Widget.Switch)o); 
            _c.ThumbDrawable.SetColorFilter(_c.Checked ? Android.Graphics.Color.ParseColor("#1363b1") : Android.Graphics.Color.White, PorterDuff.Mode.Multiply);
        }
        catch (Exception) {
        }

    }
}