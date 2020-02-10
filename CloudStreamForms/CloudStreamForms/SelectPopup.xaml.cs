using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.App;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using Rg.Plugins.Popup.Services;
using System.Linq;
using static CloudStreamForms.CloudStreamCore;

namespace CloudStreamForms
{
    public class LabelList
    {
        public Button button;
        public List<string> ItemsSource;
        public EventHandler<int> SelectedIndexChanged;
        private int _SelectedIndex = -1;

        bool _IsVisible = true;
        public bool IsVisible { get { return _IsVisible; } set { _IsVisible = value; button.IsVisible = value; } }
        public int SelectedIndex { set { _SelectedIndex = value; SelectedIndexChanged?.Invoke(this, value); } get { return _SelectedIndex; } }

        // public List<string> Items { get { return ItemsSource; } }

        public LabelList(Button _Button, List<string> _ItemSource)
        {
            button = _Button;
            ItemsSource = _ItemSource;

            button.Clicked += (o, e) => {
                PopupNavigation.Instance.PushAsync(new SelectPopup(ItemsSource, SelectedIndex));
                SelectPopup.OnSelectedChanged += (_o, _e) => {
                    SelectedIndex = _e;
                };
            };


            SelectedIndexChanged += (o, e) => {
                if (this == o) {
                    if (e >= 0) {
                        button.Text = ItemsSource[e];
                    }
                }
                else {
                    print("ERRORHANDEL:" + button.Text);
                }
            };
        }
    }



    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SelectPopup : Rg.Plugins.Popup.Pages.PopupPage
    {
        public static int selected = -1;
        public static EventHandler<int> OnSelectedChanged;
        public static bool isOpen = false;

        List<string> currentOptions = new List<string>();
        void UpdateScreenRot()
        {
            epview.HeightRequest = epview.RowHeight * (Math.Min(currentOptions.Count, (Bounds.Height > Bounds.Width) ? 12 : 3)) + epview.RowHeight / 4;
        }

        public SelectPopup(List<string> options, int selected)
        {
            currentOptions = options;

            if (isOpen) {
                PopupNavigation.PopAsync(false);
            }
            isOpen = true;
            //  BackgroundColor = Color.Transparent;
            //   BackgroundImageSource = null;
            BackgroundColor = new Color(0, 0, 0, 0.9);
            InitializeComponent();
            UpdateScreenRot();
            TheStack.SizeChanged += (o, e) => {
                UpdateScreenRot();
            };





            CancelButton.Source = GetImageSource("netflixCancel.png");
            CancelButtonBtt.Clicked += (o, e) => {
                OnSelectedChanged = null;
                PopupNavigation.PopAsync(true);
            };

            epview.ItemSelected += (o, e) => {
                if (selected != e.SelectedItemIndex) {
                    OnSelectedChanged?.Invoke(this, e.SelectedItemIndex);
                }
                epview.SelectedItem = null;
                OnSelectedChanged = null;
                PopupNavigation.PopAsync(true);
            };

            MyNameCollection = new ObservableCollection<PopupName>();
            for (int i = 0; i < options.Count; i++) {
                bool isSel = i == selected;
                PopupName p = new PopupName() { IsSelected = isSel, Name = options[i], FontAtt = isSel ? FontAttributes.Bold : FontAttributes.None };
                MyNameCollection.Add(p);
            }
            BindingContext = this;
            epview.ScrollTo(MyNameCollection[selected], ScrollToPosition.Center, false);

        }

        public class PopupName
        {
            public string Name { set; get; }
            public bool IsSelected { set; get; }
            public FontAttributes FontAtt { set; get; }
        }
        public ObservableCollection<PopupName> MyNameCollection { set; get; }


        private void epview_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {

        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            isOpen = false;
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
            return base.OnBackButtonPressed();
        }

        // Invoked when background is clicked
        protected override bool OnBackgroundClicked()
        {
            // Return false if you don't want to close this popup page when a background of the popup page is clicked
            return base.OnBackgroundClicked();
        }
    }
}