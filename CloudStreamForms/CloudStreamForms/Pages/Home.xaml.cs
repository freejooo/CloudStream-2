using CloudStreamForms.Core;
using CloudStreamForms.Effects;
using CloudStreamForms.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.MainPage;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Home : ContentPage
	{
		const int POSTER_HIGHT = 96;
		const int POSTER_WIDTH = 67;

		public MainEpisode100View epView;
		public List<IMDbTopList> iMDbTopList = new List<IMDbTopList>();
		readonly List<string> genres = new List<string>() { "", "action", "adventure", "animation", "biography", "comedy", "crime", "drama", "family", "fantasy", "film-noir", "history", "horror", "music", "musical", "mystery", "romance", "sci-fi", "sport", "thriller", "war", "western" };
		readonly List<string> genresNames = new List<string>() { "Any", "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };

		//readonly List<string> recomendationTypes = new List<string> { "Related", "Top 100", "Popular" };

		readonly LabelList MovieTypePicker;
		// readonly LabelList ImdbTypePicker;

		readonly BorderView[] selectTabItems;
		readonly Label[] selectTabLabels;

		int MovieIndex = -1;
		public bool IsRecommended { get { return MovieIndex == 0; } }
		public bool IsTop100 { get { return MovieIndex == 1; } }
		public bool IsPopular { get { return MovieIndex == 2; } }

		public int currentImageCount = 0;

		//static readonly ImageSource empty = App.GetImageSource("emtyPoster.png");

		public void LoadMoreImages(bool setHeight = true)
		{
			if (!Settings.Top100Enabled) return;
			Device.BeginInvokeOnMainThread(() => {
				if (currentImageCount == 0) {
					episodeView.Opacity = 0;
				}
				int count = 10;//PosterAtScreenHight * PosterAtScreenWith * 3
				for (int i = 0; i < count; i++) {
					if (currentImageCount >= iMDbTopList.Count) {
						if (!Fething && !IsRecommended) {
							GetFetch(currentImageCount + 1);
						}
						return;
						//Feth more data
					}
					else {
						try {
							IMDbTopList x = iMDbTopList[currentImageCount];
							bool add = true;
							int selGen = MovieTypePicker.SelectedIndex - 1;
							if (selGen != -1 && IsRecommended) {
								if (!iMDbTopList[currentImageCount].contansGenres.Contains(selGen)) {
									add = false;
								}
							}
							if (add) {
								string img = ConvertIMDbImagesToHD(iMDbTopList[currentImageCount].img, IsRecommended ? 76 : 180, IsRecommended ? 113 : 268);

								AddEpisode(new EpisodeResult() {
									TapComThree = new Command(() => {
										PushPageFromUrlAndName(x.id, x.name);
									}),
									ForceDescript = true,
									Description = x.descript,
									Title = (x.place > 0 ? (x.place + ". ") : "") + x.name + (x.year.IsClean() ? $" ({x.year})" : "") + " ★ " + x.rating.Replace(",", "."),
									Id = x.place,
									PosterUrl = img,
									ExtraInfo = "Id=" + x.id + "|||Name=" + x.name + "|||"
								}, false);
							}
						}
						catch (Exception) { }
					}
					currentImageCount++;

				}
				if (setHeight) {
					SetHeight();
				}
				episodeView.FadeTo(1);
			});
		}

		public void GetFetchRecomended()
		{
			if (!Settings.Top100Enabled) return;
			if (bookmarkPosters != null && bookmarkPosters.Count > 0) {
				if (!Fething) {
					Fething = true;
					TempThread tempThred = mainCore.CreateThread(21);
					mainCore.StartThread("GetFetchRecomended", () => {
						try {
							var f = FetchRecomended(bookmarkPosters.Select(t => t.id).ToList());
							if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
							iMDbTopList.AddRange(f);
							LoadMoreImages();
						}
						finally {
							Fething = false;
							mainCore.JoinThred(tempThred);
						}
					});
				}
			}
		}

		bool _fething = false;
		public bool Fething {
			set {
				if ((value && epView.MyEpisodeResultCollection.Count == 0) || !value) {
					Device.BeginInvokeOnMainThread(() => { LoadingIndicator.IsVisible = value; LoadingIndicator.IsEnabled = value; /*LoadingIndicator.IsRunning = value;*/ });
				}
				_fething = value;
			}
			get { return _fething; }
		}

		public void GetFetch(int start = 1)
		{
			if (!Settings.Top100Enabled) return;

			Fething = true;
			TempThread tempThred = mainCore.CreateThread(21);
			mainCore.StartThread("FethTop100", async () => {
				try {
					var f = await mainCore.FetchTop100(new List<string>() { genres[MovieTypePicker.SelectedIndex] }, start, top100: IsTop100, isAnime: Settings.Top100Anime);
					if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

					iMDbTopList.AddRange(CachedTop100[f]);
					LoadMoreImages();
				}
				finally {
					Fething = false;
					mainCore.JoinThred(tempThred);
				}
			});
		}

		private void episodeView_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			episodeView.SelectedItem = null;
			//EpisodeResult episodeResult = ((EpisodeResult)((ListView)sender).BindingContext);
			//PlayEpisode(episodeResult);
		}

		private void ImageButton_Clicked(object sender, EventArgs e)
		{
			EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
			PushPage(episodeResult);
			//  PlayEpisode(episodeResult);
		}

		public void PushPage(EpisodeResult episodeResult)
		{
			PushPageFromUrlAndName(FindShortend(episodeResult.ExtraInfo, "Id"), FindShortend(episodeResult.ExtraInfo, "Name"));

		}

		private void ViewCell_Tapped(object sender, EventArgs e)
		{
			EpisodeResult episodeResult = (EpisodeResult)(((ViewCell)sender).BindingContext);
			PushPage(episodeResult);

			// EpsodeShow(episodeResult);
			//EpisodeResult episodeResult = ((EpisodeResult)((ImageButton)sender).BindingContext);
			//App.PlayVLCWithSingleUrl(episodeResult.mirrosUrls[0], episodeResult.Title);
			//episodeView.SelectedItem = null;
		}

		readonly List<FFImageLoading.Forms.CachedImage> play_btts = new List<FFImageLoading.Forms.CachedImage>();

		private void Image_PropertyChanging(object sender, PropertyChangingEventArgs e)
		{

			FFImageLoading.Forms.CachedImage image = ((FFImageLoading.Forms.CachedImage)sender);

			if (play_btts.Where(t => t.Id == image.Id).Count() == 0) {
				play_btts.Add(image);
				//image.Source = ImageSource.FromResource("CloudStreamForms.Resource.playBtt.png", Assembly.GetExecutingAssembly());
				/*
                if (Device.RuntimePlatform == Device.Android) {
                    image.Scale = 0.5f;
                }
                else {
                    image.Scale = 0.3f;
                }*/
			}
		}

		public Command TapCommand { set; get; } = new Command((o) => { print("Hello:"); });

		public int selectedTabItem = 0;

		public async void ChangeSizeOfTabs()
		{
			UpdateNoBookmarks();
			for (int i = 0; i < selectTabItems.Length; i++) {
				bool sel = selectedTabItem == i;
				_ = selectTabLabels[i].ScaleTo(sel ? 1.1 : 0.9, 150, Easing.SinOut);
				selectTabLabels[i].FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None;
				selectTabLabels[i].TextColor = sel ? Color.FromHex("#FFF") : Color.FromHex("#c9c9c9");
			}
			bool bookmarkVis = selectedTabItem == 0;
			SetHeight();
			BookHolder.IsEnabled = bookmarkVis;
			Top100Stack.IsEnabled = !bookmarkVis;

			BookHolder.IsVisible = bookmarkVis;
			Top100Stack.IsVisible = !bookmarkVis;
			_ = Top100Stack.FadeTo(bookmarkVis ? 0 : 1);
			if (selectedTabItem > 0) {
				var _movieIndex = selectedTabItem - 1;
				if (MovieIndex != _movieIndex) {
					MovieIndex = _movieIndex;
					IndexChanged();
				}
			}
			await BookHolder.FadeTo(bookmarkVis ? 1 : 0);
		}

		void IndexChanged()
		{
			ClearEpisodes();
			mainCore.PurgeThreads(21);
			Fething = false;
			if (IsRecommended) {
				GetFetchRecomended();
			}
			else {
				GetFetch();
			}
		}

		public Home()
		{
			InitializeComponent();
			baseImg.Source = App.GetImageSource("NoBookmarks.png");

			selectTabItems = new BorderView[] {
				HomeBtt,RelatedBtt,TopBtt,TrendingBtt,
			};
			selectTabLabels = new Label[] {
				HomeLbl,RelatedLbl,TopLbl,TrendingLbl,
			};
			FastTxtBtt.Clicked += async (o, e) => {
				string a = await ActionPopup.DisplayActionSheet("Clear watching", "Yes, clear currently watching", "No, dont clear currently watching");
				if (a.StartsWith('Y')) {
					App.RemoveFolder(nameof(CachedCoreEpisode));
					UpdateNextEpisode();
				}
			};
			ChangeSizeOfTabs();
			for (int i = 0; i < selectTabItems.Length; i++) {
				Commands.SetTap(selectTabItems[i], new Command((o) => {
					int id = int.Parse(o.ToString());
					selectedTabItem = id;
					ChangeSizeOfTabs();
				}));
				Commands.SetTapParameter(selectTabItems[i], i.ToString());
			}

			if (Settings.IS_TEST_BUILD) {
#pragma warning disable CS0162 // Unreachable code detected
				return;
#pragma warning restore CS0162 // Unreachable code detected
			}
			try {
				epView = new MainEpisode100View();
				BindingContext = epView;

				BackgroundColor = Settings.BlackRBGColor;

				MovieTypePicker = new LabelList(MovieTypePickerBtt, genresNames) {
					SelectedIndex = 0
				};

				MovieTypePicker.SelectedIndexChanged += (o, e) => {
					ClearEpisodes(!IsRecommended);
					if (IsRecommended) {
						CoreHelpers.Shuffle(iMDbTopList);
						LoadMoreImages();
					}
					else {
						mainCore.PurgeThreads(21);
						Fething = false;
						GetFetch();
					}
				};

				episodeView.Scrolled += (o, e) => {
					MovieTypePickerBttScrollY += e.VerticalDelta;

					if (MovieTypePickerBttScrollY > MovieTypePickerBttMinScrollY) {
						MovieTypePickerBttScrollY = MovieTypePickerBttMinScrollY;
					}
					else if (MovieTypePickerBttScrollY < 0) {
						MovieTypePickerBttScrollY = 0;
					}

					MovieTypePickerBtt.TranslationY = MovieTypePickerBttScrollY;

					//	double maxY = episodeView.items.HeightRequest - episodeView.Height;
					//print(maxY);
					if (e.LastVisibleItemIndex >= epView.MyEpisodeResultCollection.Count - 10) {
						LoadMoreImages();
					}
				};

				if (Device.RuntimePlatform == Device.UWP) {
					// BlueSeperator.IsVisible = false;
					// BlueSeperator.IsEnabled = false;
					OffBar.IsVisible = false;
					OffBar.IsEnabled = false;
				}
				else {
					OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3;
				}

				episodeView.VerticalScrollBarVisibility = Settings.ScrollBarVisibility;
			}
			catch (Exception _ex) {
				error(_ex);
			}
			/*
            ImageScroller.Scrolled += (o, e) => {
                double maxY = ImageScroller.ContentSize.Height - ImageScroller.Height;
                if (e.ScrollY >= maxY - 200) {
                    LoadMoreImages();
                }

            };*/
		}

		public static string FindShortend(string d, string key)
		{
			return FindHTML(d, key + "=", "|||");
		}

		void AddEpisode(EpisodeResult episodeResult, bool setHeight = true)
		{
			episodeResult.ExtraColor = Settings.ItemBackGroundColor.ToHex();
			episodeResult.TapCom = new Command((s) => {
				PushPageFromUrlAndName(FindShortend(episodeResult.ExtraInfo, "Id"), FindShortend(episodeResult.ExtraInfo, "Name"));
				PushPage(episodeResult);
			});
			epView.MyEpisodeResultCollection.Add(episodeResult);

			if (setHeight) {
				SetHeight();
			}
		}

		void ClearEpisodes(bool clearData = true)
		{
			//ItemGrid.Children.Clear();
			epView.MyEpisodeResultCollection.Clear();
			cachedImages.Clear();
			currentImageCount = 0;
			SetHeight();
			if (clearData) {

				iMDbTopList.Clear();
			}
		}

		double MovieTypePickerBttScrollY = 0;
		const int MovieTypePickerBttMinScrollY = 100;

		public int PosterAtScreenWith { get { return (int)(CurrentWidth / (double)POSTER_WIDTH); } }
		public int PosterAtScreenHight { get { return (int)(CurrentWidth / (double)POSTER_HIGHT); } }
		readonly List<FFImageLoading.Forms.CachedImage> cachedImages = new List<FFImageLoading.Forms.CachedImage>();
		public const int bookmarkLabelTransY = 30;

		void SetHeight()
		{
			/*
            ItemGrid.RowSpacing = POSTER_HIGHT / 2;

            for (int i = 0; i < cachedImages.Count; i++) {
                SetChashedImagePos(i);
            }*/

			Device.BeginInvokeOnMainThread(() => {
				bool enabled = epView.MyEpisodeResultCollection.Count > 0;
				MovieTypePickerBtt.IsEnabled = enabled;
				if (enabled) {
					MovieTypePickerBtt.FadeTo(1);
				}
				else {
					MovieTypePickerBtt.Opacity = 0;
				}

				//episodeView.HeightRequest = selectedTabItem == 0 ? 0 : epView.MyEpisodeResultCollection.Count * (episodeView.RowHeight) + 200;
			});
		}

		protected override void OnDisappearing()
		{
			//  OnIconEnd(0);
			base.OnDisappearing();
		}

		void UpdateHasNext(bool show)
		{
			FastTxt.IsEnabled = show;
			FastTxt.IsVisible = show;
			NextEpisodeView.IsEnabled = show;
			NextEpisodeView.IsVisible = show;
			BookTxt.IsEnabled = show;
			BookTxt.IsVisible = show;
		}

		void UpdateNextEpisode()
		{
			var epis = App.GetKeys<CloudStreamCore.CachedCoreEpisode>(nameof(CloudStreamCore.CachedCoreEpisode)).OrderBy(t => -t.createdAt.Ticks).ToArray();
			bool hasTxt = epis.Length > 0;
			UpdateHasNext(hasTxt);

			//NextEpisode.Children.Clear();
			var pSource = App.GetImageSource("nexflixPlayBtt.png");

			epView.NextEpisodeCollection.Clear();
			var bgColor = Settings.ItemBackGroundColor.ToHex();
			for (int i = 0; i < Math.Min(epis.Length, 5); i++) {
				var ep = epis[i];

				string title = ep.episode > 0 && ep.season > 0 ? $"S{ep.season}:E{ep.episode} {ep.episodeName}" : $"{ep.episodeName}";
				/*const int MAX_TITLE_LENGHT = 20;
				if (title.Length > MAX_TITLE_LENGHT) {
					title = title[0..MAX_TITLE_LENGHT] + "...";
				}*/

				epView.NextEpisodeCollection.Add(new HomeNextEpisode() {
					ImdbId = ep.imdbId,
					PosterUrl = ep.poster,
					Title = title,
					Progress = ep.progress,
					ExtraColor = bgColor,
					InfoCommand = new Command(async () => {
						var res = new MovieResult(ep.state);
						await Navigation.PushModalAsync(res, false);
					}),
					OpenCommand = new Command(async () => {
						var res = new MovieResult(ep.state);
						await Navigation.PushModalAsync(res, false);
						for (int i = 0; i < 100; i++) {
							if (res.epView.MyEpisodeResultCollection.Count > 0) { // FIXES NEXT EPISODE PROBLEM
								break;
							}
							await Task.Delay(50);
						}
						await res.LoadLinksForEpisode(new EpisodeResult() { Episode = ep.episode, Season = ep.season, Id = ep.episode - 1, Description = ep.description, IMDBEpisodeId = ep.imdbId, OgTitle = ep.episodeName });
					}),
					RemoveCommand = new Command(() => {
						App.RemoveKey(nameof(CachedCoreEpisode), ep.parentImdbId);
						for (int i = 0; i < epView.NextEpisodeCollection.Count; i++) {
							if (epView.NextEpisodeCollection[i].ImdbId == ep.imdbId) {
								epView.NextEpisodeCollection.RemoveAt(i);
								break;
							}
						}
						if (epView.NextEpisodeCollection.Count == 0) {
							UpdateNextEpisode();
						}
					}),
				});
			}
		}

		bool firstTimeNoBookmarks = true;
		bool hasAppered = false;
		protected override void OnAppearing()
		{
			if (Settings.IS_TEST_BUILD) {
#pragma warning disable CS0162 // Unreachable code detected
				base.OnAppearing();
				return;
#pragma warning restore CS0162 // Unreachable code detected
			}
			UpdateLabels();
			SetHeight();
			if (Settings.CacheNextEpisode) {
				UpdateNextEpisode();
			}
			else {
				UpdateHasNext(false);
			}

			try {
				// OnIconStart(0);
				base.OnAppearing();
				App.SaveData();
				if (!hasAppered) {
					App.UpdateStatusBar();
					App.UpdateBackground();
				}
				//if (UpdateIsRequired) {
					UpdateBookmarks();
					UpdateIsRequired = false;
				//}

				ViewGrid.IsVisible = Settings.Top100Enabled;
				if (firstTimeNoBookmarks && !hasBookmarks && Settings.Top100Enabled) { // WILL REDIRECT TO TOP IMDb when no bookmarks
					firstTimeNoBookmarks = false;
					selectedTabItem = 2;
					ChangeSizeOfTabs();
					if (!hasAppered) {
						IndexChanged();
					}
				}
				else if (!Settings.Top100Enabled && selectedTabItem != 0) {
					selectedTabItem = 0;
					ChangeSizeOfTabs();
				}
				BackgroundColor = Settings.BlackRBGColor;
				//Color.FromHex(Settings.MainBackgroundColor);
				hasAppered = true;
			}
			catch (Exception _ex) {
				error(_ex);
			}

		}

		//            await PopupNavigation.Instance.PushAsync(new SelectPopup(new List<string>() { "Season 1", "Season 2", "Season 3", "Season 3", "Season 3", }, 1));


		const double _RecPosterMulit = 1.75;
		const int _RecPosterHeight = 100;
		const int _RecPosterWith = 65;
		int RecPosterHeight { get { return (int)Math.Round(_RecPosterHeight * _RecPosterMulit); } }
		int RecPosterWith { get { return (int)Math.Round(_RecPosterWith * _RecPosterMulit); } }

		const double _FastPosterMulit = 1.5;
		const int _FastPosterHeight = 72;
		const int _FastPosterWith = 92;//127;
		int FastPosterHeight { get { return (int)Math.Round(_FastPosterHeight * _FastPosterMulit); } }
		int FastPosterWith { get { return (int)Math.Round(_FastPosterWith * _FastPosterMulit); } }

		List<BookmarkPoster> bookmarkPosters = new List<BookmarkPoster>();


		static double lastWidth = -1;
		static double lastHeight = -1;
		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			if (lastHeight != height || lastWidth != width) {
				lastWidth = width;
				lastHeight = height;
				SetRecs(true, (int)height, (int)width);
			}
		}

		async void SetRecs(bool isFromSizeChange = false, int? height = null, int? width = null)
		{
			if (isFromSizeChange) {
				BookHolder.Opacity = 0;
			}

			await Device.InvokeOnMainThreadAsync(async () => {

				int perCol = ((width ?? Application.Current.MainPage.Width) < (height ?? Application.Current.MainPage.Height)) ? 3 : 6;

				// int row = (int)Math.Floor((Bookmarks.Children.Count - 1) / (double)perCol);
				// Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
				var lay = new GridItemsLayout(perCol, ItemsLayoutOrientation.Vertical);
				lay.VerticalItemSpacing = 5;
				lay.HorizontalItemSpacing = 5;
				BookmarkView.ItemsLayout = lay;

				BookmarkView.HeightRequest = Math.Ceiling(bookmarkPosters.Count / (float)perCol) * 200 + 20; //(bookmarkLabelTransY + RecPosterHeight + Bookmarks.RowSpacing) * (((Bookmarks.Children.Count - 1) / perCol) + 1) - 7 + Bookmarks.RowSpacing;
																						  //Bookmarks.WidthRequest = Application.Current.MainPage.Width < Application.Current.MainPage.Height ? Application.Current.MainPage.Width : Application.Current.MainPage.Height;
				if (isFromSizeChange) {
					await Task.Delay(100);
					_ = BookHolder.FadeTo(1, 75);
				}
			});
		}

		public static bool UpdateIsRequired = true;
		static bool hasBookmarks = false;

		void UpdateNoBookmarks()
		{
			bool vis = (!hasBookmarks && selectedTabItem == 0);
			baseImg.IsVisible = vis;
			baseTxt.IsVisible = vis;
		}

		struct UpdateLabel
		{
			public string id;
			public Button label;
		}

		readonly List<UpdateLabel> updateLabels = new List<UpdateLabel>();

		string GetAirDate(long airAt)
		{
			return airAt > UnixTime ? $"{CoreHelpers.ConvertUnixTimeToString(airAt)}" : "NEW!";
		}

		void UpdateLabels()
		{
			for (int i = 0; i < updateLabels.Count; i++) {
				string txt = "No Data";
				CloudStreamCore.NextAiringEpisodeData? nextData = App.GetKey<NextAiringEpisodeData?>(App.NEXT_AIRING, updateLabels[i].id, null);
				if (nextData.HasValue) {
					var nextAir = nextData.Value;
					txt = GetAirDate(nextAir.airingAt);
				}
				updateLabels[i].label.Text = txt;
			}
		}

		public static Dictionary<string, bool> IsBookmarked = new Dictionary<string, bool>();

		void UpdateBookmarks()
		{
			try {
				string[] keys = App.GetKeys<string>(App.BOOKMARK_DATA);
				List<string> data = new List<string>();
				bookmarkPosters = new List<BookmarkPoster>();
				updateLabels.Clear();
				IsBookmarked.Clear();
				epView.HomeCollection.Clear();

				var bgColor = Settings.ItemBackGroundColor.ToHex();

				int index = 0;
				bool allDone = false;
				for (int i = 0; i < keys.Length; i++) {
					string __key = keys[i];
					if (__key == "") {
						continue;
					}
					string name = FindHTML(__key, "Name=", "|||");
					string posterUrl = FindHTML(__key, "PosterUrl=", "|||");
					posterUrl = ConvertIMDbImagesToHD(posterUrl, 182, 268);

					string id = FindHTML(__key, "Id=", "|||");
					if (name != "" && posterUrl != "" && id != "") {
						if (CheckIfURLIsValid(posterUrl)) {
							IsBookmarked.Add(id, true);
							string posterURL = ConvertIMDbImagesToHD(posterUrl, 76, 113, 1.75);
							if (CheckIfURLIsValid(posterURL)) {
								var _b = new BookmarkPoster() { id = id, name = name, posterUrl = posterUrl };
								bookmarkPosters.Add(_b);

								bool showNext = false;
								string nextTxt = "";
								if (Settings.ShowNextEpisodeReleaseDate) {
									CloudStreamCore.NextAiringEpisodeData? nextData = App.GetKey<NextAiringEpisodeData?>(App.NEXT_AIRING, id, null);
									if (nextData.HasValue) {
										showNext = true;
										var nextAir = nextData.Value;
										nextTxt = GetAirDate(nextAir.airingAt);
									}
								}

								epView.HomeCollection.Add(new HomeBookmark() {
									ExtraColor = bgColor,
									ImdbId = id,
									Title = name,
									PosterUrl = posterUrl,
									OpenCommand = new Command(() => {
										PushPageFromUrlAndName(id, name);
									}),
									NextEpisodeText = nextTxt,
									ShowNextEpisode = showNext,
								});
							}
						}
					}
				}
				allDone = true;
				hasBookmarks = bookmarkPosters.Count > 0;

				UpdateNoBookmarks();
				/*if (ImdbTypePicker.SelectedIndex == -1) {
                    ImdbTypePicker.SelectedIndex = bookmarkPosters.Count > 0 ? 0 : 2; // SET TO POPULAR BY DEAFULT
                }*/
				SetRecs();

			}
			catch (Exception _ex) {
				error(_ex);
			}
			ChangeSizeOfTabs();
		}

		public double CurrentWidth { get { return Application.Current.MainPage.Width; } }

		private void Grid_BindingContextChanged(object sender, EventArgs e)
		{
			var s = ((Grid)sender);
			Commands.SetTap(s, new Command((o) => {
				var episodeResult = ((EpisodeResult)o);
				PushPageFromUrlAndName(FindShortend(episodeResult.ExtraInfo, "Id"), FindShortend(episodeResult.ExtraInfo, "Name"));
				PushPage(episodeResult);

				//do something
			}));
			Commands.SetTapParameter(s, (EpisodeResult)s.BindingContext);
			//s.BindingContext = this;
		}
	}
	public class MainEpisode100View
	{
		private ObservableCollection<EpisodeResult> _MyEpisodeResultCollection;
		public ObservableCollection<EpisodeResult> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

		public ObservableCollection<HomeNextEpisode> NextEpisodeCollection { set; get; }
		public ObservableCollection<HomeBookmark> HomeCollection { set; get; }

		public event EventHandler Added;

		public MainEpisode100View()
		{
			MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
			NextEpisodeCollection = new ObservableCollection<HomeNextEpisode>();
			HomeCollection = new ObservableCollection<HomeBookmark>();
		}
	}

	public class HomeNextEpisode
	{
		public string ImdbId { set; get; }
		public string Title { get; set; }
		public string PosterUrl { get; set; }
		public double Progress { get; set; }
		public string ExtraColor { get; set; }
		public Command OpenCommand { get; set; }
		public Command InfoCommand { get; set; }
		public Command RemoveCommand { get; set; }

		/*public double progress;
				public EpisodeOrigin origin;
				public Movie state;
				public DateTime createdAt;
				public int episode;
				public int season;
				public string episodeName;
				public string description;
				public string rating;
				public string poster;
				public string parentName;
				public string imdbId;
				public string parentImdbId;*/
	}
	public class HomeBookmark
	{
		public string ExtraColor { get; set; }
		public string ImdbId { set; get; }
		public string Title { get; set; }
		public string PosterUrl { get; set; }
		public string NextEpisodeText { get; set; }
		public bool ShowNextEpisode { get; set; }
		public Command OpenCommand { get; set; }
	}
}