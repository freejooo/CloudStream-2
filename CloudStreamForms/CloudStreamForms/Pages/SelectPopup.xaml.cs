using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.InputPopupPage;
using static CloudStreamForms.LoginPopupPage;
using static CloudStreamForms.SelectPopup;
using Button = Xamarin.Forms.Button;

namespace CloudStreamForms
{
	public static class ActionPopup
	{
		public static bool isOpen = false;

		public static async Task<string> DisplayActionSheet(string title, int sel, params string[] buttons)
		{
			var page = new SelectPopup(buttons.ToList(), sel, title, true, title == "Subtitle Font" || title == "App Font");
			await PopupNavigation.Instance.PushAsync(page);
			return await page.WaitForResult();
		}

		public static async Task<List<string>> DisplayLogin(string okButton, string cancelButton, string header, params PopupFeildsDatas[] loginData)
		{
			var page = new LoginPopupPage(okButton, cancelButton, header, loginData);
			await PopupNavigation.Instance.PushAsync(page);
			return await page.WaitForResult();
		}

		public static async Task<string> DisplayActionSheet(string title, params string[] buttons)
		{
			return await DisplayActionSheet(title, -1, buttons);
		}

		public static async Task DisplayLoadingBar(int loadingTime, string title = "Loading")
		{
			var page = new LoadingPopupPage(loadingTime, title);
			await PopupNavigation.Instance.PushAsync(page);
			if (loadingTime != -1) {
				await page.Main(loadingTime);
			}
		}

		static LoadingPopupPage currentLoading;
		public static async Task StartIndeterminateLoadinbar(string title)
		{
			currentLoading = new LoadingPopupPage(-1, title);
			await PopupNavigation.Instance.PushAsync(currentLoading);
		}

		public static async Task StopIndeterminateLoadinbar()
		{
			if (currentLoading != null) {
				await currentLoading.End();
			}
			//PopupNavigation.Instance.PopAsync(false);
		}

		public static async Task<List<bool>> DisplaySwitchList(List<string> names, List<bool> toggled, string header)
		{
			var page = new SwitchPopup(names, toggled, header);
			await PopupNavigation.Instance.PushAsync(page);
			return await page.WaitForResult();
		}

		public static async Task<string> DisplayEntry(InputPopupResult result, string placeHolder, string title = "", int offset = -1, bool autoPaste = true, string setText = null, string confirmText = "", int min = int.MinValue, int max = int.MaxValue)
		{
			var page = new InputPopupPage(result, placeHolder, title, offset, autoPaste, setText, confirmText, min, max);
			await PopupNavigation.Instance.PushAsync(page);
			return await page.WaitForResult();
		}

		public static async Task<decimal> DisplayDecimalEntry(string placeHolder, string title = "", int offset = -1, bool autoPaste = true, string setText = null, string confirmText = "")
		{
			return decimal.Parse((await DisplayEntry(InputPopupResult.decimalNumber, placeHolder, title, offset, autoPaste, setText, confirmText)).Replace(".", ","));
		}

		public static async Task<int> DisplayIntEntry(string placeHolder, string title = "", int offset = -1, bool autoPaste = true, string setText = null, string confirmText = "", int min = int.MinValue, int max = int.MaxValue)
		{
			return int.Parse(await DisplayEntry(InputPopupResult.integrerNumber, placeHolder, title, offset, autoPaste, setText, confirmText, min, max));
		}
	}

	public class LabelList
	{
		public Button button;

		private List<string> _ItemsSource;
		public List<string> ItemsSource { set { _ItemsSource = value; OnUpdateList(); } get { return _ItemsSource; } }
		public EventHandler<int> SelectedIndexChanged;
		private int _SelectedIndex = -1;

		bool _IsVisible = true;
		public bool IsVisible { get { return _IsVisible; } set { _IsVisible = value; button.IsVisible = value; } }
		public int SelectedIndex { set { _SelectedIndex = value; SelectedIndexChanged?.Invoke(this, value); } get { return _SelectedIndex; } }

		public void SetIndexWithoutChange(int val)
		{
			_SelectedIndex = val;
		}
		// public List<string> Items { get { return ItemsSource; } }

		readonly Color bgColor;
		public void OnUpdateList()
		{
			bool isEmty = ItemsSource.Count <= 1;
			button.BackgroundColor = isEmty ? Color.Transparent : bgColor;
			button.InputTransparent = isEmty;
		}


		public LabelList(Button _Button, List<string> __ItemSource, string title = "", bool fontTest = false)
		{
			button = _Button;
			bgColor = button.BackgroundColor;
			ItemsSource = __ItemSource;
			print("fontTes1111t: " + _Button.Text + "|" + fontTest);

			button.Clicked += async (o, e) => {
				//  await ActionPopup.DisplayEntry(InputPopupResult.decimalNumber, "ms", "Audio Delay",offset:50,setText:"0",confirmText:"Set Delay");
				// await ActionPopup.DisplayEntry(InputPopupResult.url, "https://youtu.be/", "Youtube link",confirmText:"Download")
				print("fontTest: " + _Button.Text + "|" + fontTest);
				await PopupNavigation.Instance.PushAsync(new SelectPopup(ItemsSource, SelectedIndex, title, fontTest: fontTest));
				SelectPopup.OnSelectedChanged += (_o, _e) => {
					SelectedIndex = _e;
				};
			};

			SelectedIndexChanged += (o, e) => {
				if (this == o) {
					if (e >= 0 && ItemsSource.Count > 0) {
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
		readonly SelectLabelView selectBinding;
		const int fullNum = 12;
		const int halfNum = 6;
		const bool setOnLeft = true;

		readonly List<string> currentOptions = new List<string>();
		void UpdateScreenRot()
		{
			bool hightOverWidth = Bounds.Height > Bounds.Width;
			epview.HeightRequest = epview.RowHeight * (Math.Min(currentOptions.Count, (hightOverWidth) ? fullNum : halfNum)) + epview.RowHeight / 4;
			if (setOnLeft) {
				CrossbttLayout.VerticalOptions = hightOverWidth ? LayoutOptions.End : LayoutOptions.Center;
				CrossbttLayout.HorizontalOptions = hightOverWidth ? LayoutOptions.Center : LayoutOptions.End;
				CrossbttLayout.TranslationY = hightOverWidth ? -80 : -40;
				CrossbttLayout.TranslationX = hightOverWidth ? 0 : 40;
				TheStack.TranslationX = hightOverWidth ? 0 : 80;
				//TheStack.HorizontalOptions = hightOverWidth ? LayoutOptions.Center : LayoutOptions.CenterAndExpand;
				TheStack.TranslationY = hightOverWidth ? 40 : 40;
				epview.TranslationY = hightOverWidth ? 0 : -40;
				HeaderTitle.TranslationY = hightOverWidth ? 0 : -30;
				Grid.SetRow(CrossbttLayout, hightOverWidth ? 1 : 0);
				Grid.SetColumn(CrossbttLayout, hightOverWidth ? 0 : 1);
			}
		}

		public async Task<string> WaitForResult()
		{
			while (true) {
				await Task.Delay(10);
				if (optionSelected != "") {
					return optionSelected;
				}
			}
		}

		string optionSelected = "";

		void Cancel()
		{
			OnSelectedChanged = null;
			optionSelected = "Cancel";
			ActionPopup.isOpen = false;
			PopupNavigation.Instance.PopAsync(true);
		}

		public SelectPopup(List<string> options, int selected, string header = "", bool isCenter = true, bool fontTest = false)
		{
			currentOptions = options;

			if (ActionPopup.isOpen) {
				PopupNavigation.Instance.PopAsync(false);
			}

			ActionPopup.isOpen = true;
			//  BackgroundColor = Color.Transparent;
			//   BackgroundImageSource = null;
			BackgroundColor = new Color(0, 0, 0, 0.9);
			InitializeComponent();

			// On<Xamarin.Forms.PlatformConfiguration.Android>().Element.windo

			UpdateScreenRot();
			TheStack.SizeChanged += (o, e) => {
				UpdateScreenRot();
			};

			HeaderTitle.Text = header;
			HeaderTitle.IsEnabled = header != "";
			HeaderTitle.IsVisible = header != "";

			CancelButton.Source = GetImageSource("netflixCancel.png");
			CancelButtonBtt.Clicked += (o, e) => {
				Cancel();
			};

			epview.ItemSelected += (o, e) => {
				if (e.SelectedItemIndex != -1) {
					if (selected != e.SelectedItemIndex) {
						OnSelectedChanged?.Invoke(this, e.SelectedItemIndex);
					}
					epview.SelectedItem = null;
					OnSelectedChanged = null;

					optionSelected = options[e.SelectedItemIndex];
					PopupNavigation.Instance.PopAsync(true);
				}
			};

			selectBinding = new SelectLabelView();
			BindingContext = selectBinding;

			for (int i = 0; i < currentOptions.Count; i++) {
				bool isSel = i == selected;
				selectBinding.MyNameCollection.Add(new PopupName() { IsSelected = isSel, Name = currentOptions[i].Replace("(Mirror ", "("), LayoutCenter = isCenter ? LayoutOptions.Center : LayoutOptions.Start, FontFam = fontTest ? GetFont(currentOptions[i]) : "" });
			}

			if (selected != -1) {
				epview.ScrollTo(selectBinding.MyNameCollection[selected], ScrollToPosition.Center, false);
			}
		}


		public class PopupName
		{
			public string Name { set; get; } = "Hello";
			public bool IsSelected { set; get; } = false;
			public FontAttributes FontAtt { get { return IsSelected ? FontAttributes.Bold : FontAttributes.None; } }
			public int FontSize { get { return IsSelected ? 21 : 19; } }
			public LayoutOptions LayoutCenter { get; set; }
			public string FontFam { get; set; }
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

		/*
		protected override bool OnBackButtonPressed()
		{
			isActive = false;
			return base.OnBackButtonPressed();
		}*/

		// ### Overrided methods which can prevent closing a popup page ###

		// Invoked when a hardware back button is pressed
		protected override bool OnBackButtonPressed()
		{
			// Return true if you don't want to close this popup page when a back button is pressed
			//Cancel();
			return base.OnBackButtonPressed();
		}

		// Invoked when background is clicked
		protected override bool OnBackgroundClicked()
		{
			// Return false if you don't want to close this popup page when a background of the popup page is clicked
			return base.OnBackgroundClicked();
		}
	}
	public class SelectLabelView
	{
		public ObservableCollection<PopupName> MyNameCollection { set { Added?.Invoke(null, null); _MyNameCollection = value; } get { return _MyNameCollection; } }
		private ObservableCollection<PopupName> _MyNameCollection;

		public event EventHandler Added;

		public SelectLabelView()
		{
			MyNameCollection = new ObservableCollection<PopupName>();
		}
	}
}