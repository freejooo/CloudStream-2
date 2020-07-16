using Rg.Plugins.Popup.Services;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoadingPopupPage : Rg.Plugins.Popup.Pages.PopupPage
    {
        public LoadingPopupPage(int loadingMs, string title = "Loading")
        {
            InitializeComponent();
            // BackgroundColor = new Color(0, 0, 0, 0.9);

            if (ActionPopup.isOpen) {
                PopupNavigation.PopAsync(false);
            }
            ActionPopup.isOpen = true;

            HeaderTitle.Text = title;
            if (loadingMs == -1) {
                MainProgressBar.ClassId = "id";
            }
            else {
                Main(loadingMs);
            }
        }

        const int RELEASE_MS = 100;
        async void Main(int loadingMs)
        {
            await MainProgressBar.ProgressTo(1, (uint)Math.Max(loadingMs - RELEASE_MS, 0), Easing.SinIn);
            End();
        }

        public async Task End()
        {
            await MainProgressBar.ScaleYTo(0, RELEASE_MS, Easing.SinIn);
            await PopupNavigation.PopAsync(false);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            ActionPopup.isOpen = false;
            base.OnDisappearing();
        }

        // ### Methods for supporting animations in your popup page ###

        // Invoked before an animation appearing
        protected override void OnAppearingAnimationBegin()
        {
            base.OnAppearingAnimationBegin();
        }

        // Invoked after an animation appearing
        protected override void OnAppearingAnimationEnd()
        {
            base.OnAppearingAnimationEnd();
        }

        // Invoked before an animation disappearing
        protected override void OnDisappearingAnimationBegin()
        {
            base.OnDisappearingAnimationBegin();
        }

        // Invoked after an animation disappearing
        protected override void OnDisappearingAnimationEnd()
        {
            base.OnDisappearingAnimationEnd();
        }

        protected override Task OnAppearingAnimationBeginAsync()
        {
            return base.OnAppearingAnimationBeginAsync();
        }

        protected override Task OnAppearingAnimationEndAsync()
        {
            return base.OnAppearingAnimationEndAsync();
        }

        protected override Task OnDisappearingAnimationBeginAsync()
        {
            return base.OnDisappearingAnimationBeginAsync();
        }

        protected override Task OnDisappearingAnimationEndAsync()
        {
            return base.OnDisappearingAnimationEndAsync();
        }

        // ### Overrided methods which can prevent closing a popup page ###

        // Invoked when a hardware back button is pressed
        protected override bool OnBackButtonPressed()
        {
            // Return true if you don't want to close this popup page when a back button is pressed
            return true;//base.OnBackButtonPressed();
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            return false;//base.OnBackgroundClicked();
        }
    }
}