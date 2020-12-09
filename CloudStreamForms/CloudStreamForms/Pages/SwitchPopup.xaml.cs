using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.App;
using static CloudStreamForms.SwitchPopup;

namespace CloudStreamForms
{

	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SwitchPopup : Rg.Plugins.Popup.Pages.PopupPage
	{
		public static EventHandler<int> OnSelectedChanged;
		readonly SwitchLabelView selectBinding;
		const int fullNum = 12;
		const int halfNum = 6;
		const bool setOnLeft = true;

		void UpdateScreenRot()
		{
			bool hightOverWidth = Bounds.Height > Bounds.Width;
			epview.HeightRequest = epview.RowHeight * (Math.Min(optionsCount, (hightOverWidth) ? fullNum : halfNum)) + epview.RowHeight / 4;
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

		public async Task<List<bool>> WaitForResult()
		{
			while (true) {
				await Task.Delay(10);
				if (result != null) {
					return result;
				}
			}
		}

		readonly int optionsCount;
		List<bool> result = null;
		public SwitchPopup(List<string> options, List<bool> isToggled, string header = "")
		{
			InitializeComponent();
			if (ActionPopup.isOpen) {
				PopupNavigation.Instance.PopAsync(false);
			}
			ActionPopup.isOpen = true;
			optionsCount = options.Count;

			BackgroundColor = new Color(0, 0, 0, 0.9);

			UpdateScreenRot();
			TheStack.SizeChanged += (o, e) => {
				UpdateScreenRot();
			};

			HeaderTitle.Text = header;
			HeaderTitle.IsEnabled = header != "";
			HeaderTitle.IsVisible = header != "";

			CancelButton.Source = GetImageSource("netflixCancel.png");
			CancelButtonBtt.Clicked += async (o, e) => {
				result = selectBinding.MyNameCollection.Select(t => t.IsSelected).ToList();
				ActionPopup.isOpen = false;
				await PopupNavigation.Instance.PopAsync(true);
			};
			void ForceUpdate()
			{
				var _e = selectBinding.MyNameCollection.ToList();
				Device.BeginInvokeOnMainThread(() => {
					selectBinding.MyNameCollection.Clear();
					for (int i = 0; i < _e.Count; i++) {
						selectBinding.MyNameCollection.Add(_e[i]);
					}
				});
			}

			epview.ItemSelected += (o, e) => {
				if (e.SelectedItemIndex != -1) {
					selectBinding.MyNameCollection[e.SelectedItemIndex].IsSelected = !selectBinding.MyNameCollection[e.SelectedItemIndex].IsSelected;

					epview.SelectedItem = null;
					OnSelectedChanged = null;
					ForceUpdate();
				}
			};

			selectBinding = new SwitchLabelView();
			BindingContext = selectBinding;

			for (int i = 0; i < options.Count; i++) {
				selectBinding.MyNameCollection.Add(new SwitchName() { IsSelected = isToggled[i], Name = options[i], });
			}
		}

		public class SwitchName : ICloneable
		{
			public string Name { set; get; } = "Hello";
			public bool IsSelected { set; get; } = false;

			public object Clone()
			{
				return this.MemberwiseClone();
			}
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
			return base.OnBackButtonPressed();
		}

		// Invoked when background is clicked
		protected override bool OnBackgroundClicked()
		{
			// Return false if you don't want to close this popup page when a background of the popup page is clicked
			return base.OnBackgroundClicked();
		}
	}
	public class SwitchLabelView
	{
		public ObservableCollection<SwitchName> MyNameCollection { set { Added?.Invoke(null, null); _MyNameCollection = value; } get { return _MyNameCollection; } }
		private ObservableCollection<SwitchName> _MyNameCollection;

		public event EventHandler Added;

		public SwitchLabelView()
		{
			MyNameCollection = new ObservableCollection<SwitchName>();
		}
	}
}