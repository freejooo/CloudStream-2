using CloudStreamForms.Core;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InputPopupPage : PopupPage
    {
        public enum InputPopupResult
        {
            integrerNumber = 0,
            decimalNumber = 1,
            plainText = 2,
            password = 3,
            url = 4,
        }

        readonly InputPopupResult InputType;
        public InputPopupPage(InputPopupResult inputType, string placeHolder = "", string title = "", int offset = -1, bool autoPaste = true, string setText = null, string confirmText = "")
        {
            InitializeComponent();
            BackgroundColor = new Color(0, 0, 0, 0.9);
            InputType = inputType;

            CancelButton.Source = App.GetImageSource("netflixCancel.png");
            if (offset != -1) {
                void ChangeBtt(int add)
                {
                    if (InputF.Text == "") {
                        InputF.Text = "0";
                    }
                    decimal p = decimal.Parse(InputF.Text);
                    int round = ((int)System.Math.Round(p / offset)) * offset;
                    InputF.Text = (round + add).ToString();
                }

                UpButton.Source = App.GetImageSource("upButton3.png");
                DownButton.Source = App.GetImageSource("upButton3.png");
                UpButton.Clicked += (o, e) => {
                    ChangeBtt(offset);
                };
                DownButton.Clicked += (o, e) => {
                    ChangeBtt(-offset);
                };
            }
            else {
                OffsetButtons.IsVisible = false;
                OffsetButtons.IsEnabled = false;
            }

            bool confirmEnabled = confirmText != "";

            ConfirmButton.IsVisible = confirmEnabled;
            ConfirmButton.IsEnabled = confirmEnabled;
            if (confirmEnabled) {
                ConfirmButton.Text = confirmText;
                CloudStreamCore.print("CONFIRMDDADA: " + confirmText);
                ConfirmButton.Clicked += (o, e) => {
                    Done();
                };
            }

            UpdateScreenRot();
            TheStack.SizeChanged += (o, e) => {
                UpdateScreenRot();
            };
            CancelButtonBtt.Clicked += (o, e) => {
                cancel = true;
                Done("");
            };

            InputF.ReturnType = ReturnType.Done;
            InputF.Placeholder = placeHolder;

            // FORCE CORRENT TYPE
            InputF.TextChanged += (o, e) => {
                if (e.NewTextValue.Contains("-") && !e.NewTextValue.StartsWith("-")) {
                    InputF.Text = e.OldTextValue;
                }

                if (inputType == InputPopupResult.integrerNumber) {
                    InputF.Text = Regex.Replace(e.NewTextValue, "[^0-9-]", "");
                }
                else if (inputType == InputPopupResult.decimalNumber) {
                    InputF.Text = Regex.Replace(e.NewTextValue, "[^0-9.,-]", "");
                    if (e.OldTextValue.IsClean()) {
                        if (e.OldTextValue.Contains(",") || e.OldTextValue.Contains(".")) { // REMOVE DUPLICATES
                            if (e.NewTextValue.EndsWith(",") || e.NewTextValue.EndsWith(".")) {
                                InputF.Text = InputF.Text.Substring(0, InputF.Text.Length - 1);
                            }
                        }
                    }
                    else {
                        if (e.NewTextValue.StartsWith(",") || e.NewTextValue.StartsWith(".")) {
                            InputF.Text = "";
                        }
                    }
                }
            };

            InputF.Text = setText ?? "";

            if (inputType == InputPopupResult.decimalNumber || inputType == InputPopupResult.integrerNumber) {
                InputF.IsSpellCheckEnabled = false;
                InputF.IsTextPredictionEnabled = false;
                InputF.Keyboard = Keyboard.Numeric;
            }
            else if (inputType == InputPopupResult.password) {
                InputF.IsPassword = true;
            }
            else if (inputType == InputPopupResult.url) {
                InputF.Keyboard = Keyboard.Url;
                InputF.IsSpellCheckEnabled = false;
                if (autoPaste && Xamarin.Essentials.Clipboard.HasText) {
                    InputF.Text = Xamarin.Essentials.Clipboard.GetTextAsync().Result;
                }
            }
            InputF.ClearButtonVisibility = ClearButtonVisibility.WhileEditing;
            HeaderTitle.Text = title;

            InputF.WidthRequest = IsNumber ? 150 : 300;
            InputF.FontSize = IsNumber ? 25 : 20;

            InputF.Completed += (o, e) => {
                Done();
            };
        }

        string text = "";
        bool isDone = false;
        bool cancel = false;
        bool IsNumber { get { return (InputType == InputPopupResult.decimalNumber || InputType == InputPopupResult.integrerNumber); } }

        async void Done(string txt = null)
        {
            text = txt ?? InputF.Text;
            isDone = true;
            await Task.Delay(100);
            await PopupNavigation.PopAsync(true);
        }

        public async Task<string> WaitForResult()
        {
            while (!isDone) {
                await Task.Delay(10);
            }
            CloudStreamCore.print("MAIN RESTT:T::T: " + text);
            if (cancel) {
                if (InputType == InputPopupResult.decimalNumber || InputType == InputPopupResult.integrerNumber) {
                    return "-1";
                }
                else return "";
            }
            if (IsNumber && text == "") {
                return "-1";
            }

            return text;
        }

        const bool setOnLeft = true;

        void UpdateScreenRot()
        {
            bool hightOverWidth = Bounds.Height > Bounds.Width;
            if (setOnLeft) {
                CrossbttLayout.VerticalOptions = hightOverWidth ? LayoutOptions.End : LayoutOptions.Center;
                CrossbttLayout.HorizontalOptions = hightOverWidth ? LayoutOptions.Center : LayoutOptions.End;
                CrossbttLayout.TranslationY = hightOverWidth ? -40 : 20;
                CrossbttLayout.TranslationX = hightOverWidth ? 0 : 40;
                TheStack.TranslationX = hightOverWidth ? 0 : 80;
                CenterStack.VerticalOptions = hightOverWidth ? LayoutOptions.Start : LayoutOptions.Center;

                //TheStack.HorizontalOptions = hightOverWidth ? LayoutOptions.Center : LayoutOptions.CenterAndExpand;
                TheStack.TranslationY = hightOverWidth ? 100 : 70;
                Grid.SetRow(CrossbttLayout, hightOverWidth ? 1 : 0);
                Grid.SetColumn(CrossbttLayout, hightOverWidth ? 0 : 1);
            }
        }
    }
}