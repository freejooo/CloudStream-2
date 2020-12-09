using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.Core.CloudStreamCore;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Search : ContentPage
	{
		public ObservableCollection<SearchResult> MySearchResultCollection { get; set; }
		public static Poster mainPoster;

		protected override void OnAppearing()
		{
			base.OnAppearing();
			BackgroundColor = Settings.BlackRBGColor;
		}
		public Search()
		{
			InitializeComponent();
			mainCore.SearchLoaded += Search_searchLoaded;
			BackgroundColor = Settings.BlackRBGColor;

			MainSearchBar.TextChanged += SearchBar_TextChanged;
			MainSearchBar.SearchButtonPressed += SearchBar_SearchButtonPressed;

			MySearchResultCollection = new ObservableCollection<SearchResult>();

			BindingContext = this;

			if (Device.RuntimePlatform == Device.UWP) {
				OffBar.IsVisible = false;
				OffBar.IsEnabled = false;
			}
			else {
				OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
			}
		}

		private void SearchBar_SearchButtonPressed(object sender, EventArgs e)
		{
			_ = mainCore.QuickSearch(((SearchBar)sender).Text);
		} 

		protected override bool OnBackButtonPressed()
		{
			if (Settings.BackPressToHome) {
				MainPage.SelectMainPageIndex(0);
				return true;
			}
			else {
				return base.OnBackButtonPressed();
			}
		}


		public static async void PushPage(Poster _mainPoster, INavigation navigation)
		{
			if (mainPoster.url == _mainPoster.url) return;

			mainPoster = _mainPoster;
			Page p = new MovieResult();// { mainPoster = mainPoster };
			try {
				await navigation.PushModalAsync(p, false);

			}
			catch (Exception) {

			}
		}

		private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Settings.SearchEveryCharEnabled) {
				_ = mainCore.QuickSearch(e.NewTextValue);
			}
		}

		List<Poster> activePosters = new List<Poster>();

		private void Search_searchLoaded(object sender, List<Poster> e)
		{
			activePosters = e;
			var bg = Settings.ItemBackGroundColor;
			var bgColor = Settings.ItemBackGroundColor.ToHex();
			var bgBlue = new Color(bg.R / 1.2, bg.G / 1.2, bg.B, 1.0).ToHex();

			Device.BeginInvokeOnMainThread(() => {
				MySearchResultCollection.Clear();
				for (int i = 0; i < mainCore.activeSearchResults.Count; i++) {
					bool isBook = Home.IsBookmarked.ContainsKey(mainCore.activeSearchResults[i].url);
					string extra = mainCore.activeSearchResults[i].extra;
					if (extra != "") {
						extra = " - " + extra;
					}
					int _id = i;
					MySearchResultCollection.Add(new SearchResult() {
						OnClick = new Command(() => {
							PushPage(activePosters[_id], Navigation);
						}),
						IsBookmarked = isBook,
						ExtraColor = isBook ? bgBlue : bgColor,
						Id = i,
						Title = mainCore.activeSearchResults[i].name + extra,
						Extra = mainCore.activeSearchResults[i].year,
						Poster = CloudStreamForms.Core.CloudStreamCore.ConvertIMDbImagesToHD(mainCore.activeSearchResults[i].posterUrl, 40, 60, multi: 2)
					});
				}
			});
		}
	}
}