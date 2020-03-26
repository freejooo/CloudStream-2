using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace CloudStreamForms
{
    public class CustomCell : ViewCell
    {
        protected override void OnParentSet()
        {
            base.OnParentSet(); 
            //View.Opacity = 0;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (View.Opacity == 0 || View.Opacity == 1) {
                View.Opacity = 0;
                View.Scale = 0.8;
                View.ScaleTo(1, 250);
                View.FadeTo(1, 250);
            }

            // Do animation
        }
    }
}
