using CloudStreamForms.Core;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPopupPage : PopupPage
    {
        public struct PopupFeildsDatas
        {
            public string placeholder;
            public string setText;
            public bool isPassword;
        }

        const bool setOnLeft = true;
        void UpdateScreenRot()
        {
            bool hightOverWidth = Bounds.Height > Bounds.Width;
            if (setOnLeft) {
                TheStack.TranslationY = hightOverWidth ? -100 : 0;
            }
        }

        public LoginPopupPage(string okButton, string cancelButton, string header, params PopupFeildsDatas[] loginData)
        {
            InitializeComponent();
            BackgroundColor = new Color(0, 0, 0, 0.9);
            
            UpdateScreenRot();
            TheStack.SizeChanged += (o, e) => {
                UpdateScreenRot();
            };

            HeaderTitle.Text = header;

            List<Entry> entrys = new List<Entry>();
            for (int i = 0; i < loginData.Length; i++) {
                var login = loginData[i];

                var entry = new Entry() { Margin = 10, HorizontalOptions = LayoutOptions.Center, TranslationX = 12, WidthRequest = 250, FontSize = 20, HeightRequest = 50, MinimumWidthRequest = 50, Placeholder = login.placeholder, IsPassword = login.isPassword, Text = login.setText, };
                GridRow.Children.Add(entry);
                Grid.SetRow(entry, i);
                entrys.Add(entry);
            }

            ConfirmButton.Text = okButton;
            CancelButton.Text = cancelButton;

            CancelButton.Clicked += (o, e) => {
                Done();
                logins = new List<string>();
            };

            ConfirmButton.Clicked += (o, e) => {
                Done();
                for (int i = 0; i < loginData.Length; i++) {
                    logins.Add(entrys[i].Text);
                }
            };
        }

        bool done = false;
        List<string> logins = new List<string>();

        async void Done()
        {
            await PopupNavigation.PopAsync(true);
            done = true;
        }

        public async Task<List<string>> WaitForResult()
        {
            while (!done) {
                await Task.Delay(100);
            }
            return logins;
        }
    }
}