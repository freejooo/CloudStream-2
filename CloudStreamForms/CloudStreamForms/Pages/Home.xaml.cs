using CloudStreamForms.Core;
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

		public MainEpisodeView epView;
		public List<IMDbTopList> iMDbTopList = new List<IMDbTopList>();
		readonly List<string> genres = new List<string>() { "", "action", "adventure", "animation", "biography", "comedy", "crime", "drama", "family", "fantasy", "film-noir", "history", "horror", "music", "musical", "mystery", "romance", "sci-fi", "sport", "thriller", "war", "western" };
		readonly List<string> genresNames = new List<string>() { "Any", "Action", "Adventure", "Animation", "Biography", "Comedy", "Crime", "Drama", "Family", "Fantasy", "Film-Noir", "History", "Horror", "Music", "Musical", "Mystery", "Romance", "Sci-Fi", "Sport", "Thriller", "War", "Western" };

		readonly List<string> recomendationTypes = new List<string> { "Related", "Top 100", "Popular" };

		readonly LabelList MovieTypePicker;
		// readonly LabelList ImdbTypePicker;

		readonly BorderView[] selectTabItems;
		readonly Label[] selectTabLabels;

		int MovieIndex = 0;
		public bool IsRecommended { get { return MovieIndex == 0; } }
		public bool IsTop100 { get { return MovieIndex == 1; } }
		public bool IsPopular { get { return MovieIndex == 2; } }

		public int currentImageCount = 0;

		//static readonly ImageSource empty = App.GetImageSource("emtyPoster.png");

		public void LoadMoreImages(bool setHeight = true)
		{
			if (!Settings.Top100Enabled) return;
			Device.BeginInvokeOnMainThread(() => {
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

								AddEpisode(new EpisodeResult() { ForceDescript = true, Description = x.descript, Title = (x.place > 0 ? (x.place + ". ") : "") + x.name + " | ★ " + x.rating.Replace(",", "."), Id = x.place, PosterUrl = img, extraInfo = "Id=" + x.id + "|||Name=" + x.name + "|||" }, false);
							}

						}
						catch (Exception) {

						}

						// ItemGrid.Children.Add(cachedImages[currentImageCount]);
						//SetChashedImagePos(ItemGrid.Children.Count - 1);
					}
					currentImageCount++;

				}
				if (setHeight) {
					SetHeight();
				}
			});
		}


		public void GetFetchRecomended()
		{
			if (!Settings.Top100Enabled) return;

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
			mainCore.StartThread("FethTop100", () => {
				try {
					var f = FetchTop100(new List<string>() { genres[MovieTypePicker.SelectedIndex] }, start, top100: IsTop100, isAnime: Settings.Top100Anime);
					if (!mainCore.GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS

					iMDbTopList.AddRange(f);
					/*  Device.BeginInvokeOnMainThread(() => {

                          for (int i = 0; i < iMDbTopList.Count; i++) {

                              string img = ConvertIMDbImagesToHD(iMDbTopList[i].img, 67, 98, 4);
                              IMDbTopList x = iMDbTopList[i];

                              AddEpisode(new EpisodeResult() { Description = x.descript, Title = x.name + " | ★ " + x.rating.Replace(",", "."), Id = x.place, PosterUrl = img, extraInfo = "Id="+x.id+"|||Name="+x.name+"|||" }, false);
                          }

                    //  LoadMoreImages(false);
                    // LoadMoreImages();

                });*/
					//if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
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
			PushPageFromUrlAndName(FindShortend(episodeResult.extraInfo, "Id"), FindShortend(episodeResult.extraInfo, "Name"));

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
				selectTabLabels[i].ScaleTo(sel ? 1.1 : 0.9, 150, Easing.SinOut);
				selectTabLabels[i].FontAttributes = sel ? FontAttributes.Bold : FontAttributes.None;
				selectTabLabels[i].TextColor = sel ? Color.FromHex("#FFF") : Color.FromHex("#c9c9c9");
			}
			bool bookmarkVis = selectedTabItem == 0;
			SetHeight();
			Bookmarks.IsEnabled = bookmarkVis;
			Top100Stack.IsEnabled = !bookmarkVis;

			Bookmarks.IsVisible = bookmarkVis;
			Top100Stack.IsVisible = !bookmarkVis;
			Top100Stack.FadeTo(bookmarkVis ? 0 : 1);
			if (selectedTabItem > 0) {
				var _movieIndex = selectedTabItem - 1;
				if (MovieIndex != _movieIndex) {
					MovieIndex = _movieIndex;
					IndexChanged();
				}
			}
			await Bookmarks.FadeTo(bookmarkVis ? 1 : 0);

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
				return;
			}
			try {
				epView = new MainEpisodeView();
				BindingContext = epView;

				BackgroundColor = Settings.BlackRBGColor;

				MovieTypePicker = new LabelList(MovieTypePickerBtt, genresNames);

				// ImdbTypePicker = new LabelList(ImdbTypePickerBtt, recomendationTypes);

				//MovieTypePicker.ItemsSource = genresNames;
				// ImdbTypePicker.ItemsSource = recomendationTypes;
				//   UpdateBookmarks();

				MovieTypePicker.SelectedIndex = 0;
				/*
                ImdbTypePicker.SelectedIndex = -1;
                 
                ImdbTypePicker.SelectedIndexChanged += (o, e) => {
                    ClearEpisodes();
                    mainCore.PurgeThreads(21);
                    Fething = false;
                    if (IsRecommended) {
                        GetFetchRecomended();
                    }
                    else {
                        GetFetch();
                    }
                    //    ImdbTypePickerBtt.Text = ImdbTypePicker.Items[ImdbTypePicker.SelectedIndex];

                };*/

				//   MovieTypePickerBtt.Text = MovieTypePicker.ItemsSource[MovieTypePicker.SelectedIndex];

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

				double lastScroll = 0;

				episodeView.Scrolled += (o, e) => {
					MovieTypePickerBttScrollY -= lastScroll - e.ScrollY;
					lastScroll = e.ScrollY;

					if (MovieTypePickerBttScrollY > MovieTypePickerBttMinScrollY) {
						MovieTypePickerBttScrollY = MovieTypePickerBttMinScrollY;
					}
					else if (MovieTypePickerBttScrollY < 0) {
						MovieTypePickerBttScrollY = 0;
					}

					MovieTypePickerBtt.TranslationY = MovieTypePickerBttScrollY;

					double maxY = episodeView.HeightRequest - episodeView.Height;
					//print(maxY);
					if (e.ScrollY >= maxY - 200) {
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


		async Task AddEpisodeAsync(EpisodeResult episodeResult, bool setHeight = true, int delay = 10)
		{
			AddEpisode(episodeResult, setHeight);
			await Task.Delay(delay);
		}


		void AddEpisode(EpisodeResult episodeResult, bool setHeight = true)
		{
			episodeResult.ExtraColor = Settings.ItemBackGroundColor.ToHex();


			episodeResult.TapCom = new Command((s) => {
				PushPageFromUrlAndName(FindShortend(episodeResult.extraInfo, "Id"), FindShortend(episodeResult.extraInfo, "Name"));
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

		public int PosterAtScreenWith { get { return (int)(currentWidth / (double)POSTER_WIDTH); } }
		public int PosterAtScreenHight { get { return (int)(currentWidth / (double)POSTER_HIGHT); } }
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

				episodeView.HeightRequest = selectedTabItem == 0 ? 0 : epView.MyEpisodeResultCollection.Count * (episodeView.RowHeight) + 200;
			});

		}

		void SetChashedImagePos(int pos)
		{
			int x = pos % PosterAtScreenWith;
			int y = (int)(pos / PosterAtScreenWith);
			Grid.SetColumn(cachedImages[pos], x);
			Grid.SetRow(cachedImages[pos], y);
		}

		protected override void OnDisappearing()
		{
			//  OnIconEnd(0);
			base.OnDisappearing();
		}

		bool firstTimeNoBookmarks = true;
		bool hasAppered = false;
		protected override void OnAppearing()
		{
			if (Settings.IS_TEST_BUILD) {
				base.OnAppearing();
				return;
			}
			SetHeight();

			try {
				// OnIconStart(0);
				base.OnAppearing();
				App.SaveData();
				if (!hasAppered) {
					App.UpdateStatusBar();
					App.UpdateBackground();
				}
				if (UpdateIsRequired) {
					UpdateBookmarks();
					UpdateIsRequired = false;
				}
				if (!hasAppered) {
					IndexChanged();
				}
				ViewGrid.IsVisible = Settings.Top100Enabled;
				if(firstTimeNoBookmarks && !hasBookmarks && Settings.Top100Enabled) { // WILL REDIRECT TO TOP IMDb when no bookmarks
					firstTimeNoBookmarks = false;
					selectedTabItem = 2;
					ChangeSizeOfTabs();
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
				Bookmarks.Opacity = 0;
			}

			await Device.InvokeOnMainThreadAsync(async () => {
				
				int perCol = ((width ?? Application.Current.MainPage.Width) < (height ?? Application.Current.MainPage.Height)) ? 3 : 6;

				for (int i = 0; i < Bookmarks.Children.Count; i++) { // GRID
					Grid.SetColumn(Bookmarks.Children[i], i % perCol);
					Grid.SetRow(Bookmarks.Children[i], i / perCol);
				}
				// int row = (int)Math.Floor((Bookmarks.Children.Count - 1) / (double)perCol);
				// Recommendations.HeightRequest = (RecPosterHeight + Recommendations.RowSpacing) * (total / perCol);
				Bookmarks.HeightRequest = (bookmarkLabelTransY + RecPosterHeight + Bookmarks.RowSpacing) * (((Bookmarks.Children.Count - 1) / perCol) + 1) - 7 + Bookmarks.RowSpacing;
				if (isFromSizeChange) {
					await Task.Delay(100);
					Bookmarks.FadeTo(1, 75);
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

		void UpdateBookmarks()
		{
			try {
				int height = 150;
				Bookmarks.HeightRequest = height;
				List<string> keys = App.GetKeys<string>(App.BOOKMARK_DATA);
				List<string> data = new List<string>();
				bookmarkPosters = new List<BookmarkPoster>();
				Bookmarks.Children.Clear();
				int index = 0;
				bool allDone = false;
				for (int i = 0; i < keys.Count; i++) {
					string __key = App.ConvertToObject<string>(keys[i], "");
					if (__key == "") {
						continue;
					}
					string name = FindHTML(__key, "Name=", "|||");
					string posterUrl = FindHTML(__key, "PosterUrl=", "|||");
					posterUrl = ConvertIMDbImagesToHD(posterUrl, 182, 268);

					string id = FindHTML(__key, "Id=", "|||");
					if (name != "" && posterUrl != "" && id != "") {
						if (CheckIfURLIsValid(posterUrl)) {
							string posterURL = ConvertIMDbImagesToHD(posterUrl, 76, 113, 1.75); //.Replace(",76,113_AL", "," + pwidth + "," + pheight + "_AL").Replace("UY113", "UY" + pheight).Replace("UX76", "UX" + pwidth);
							if (CheckIfURLIsValid(posterURL)) {
								Grid stackLayout = new Grid() { VerticalOptions = LayoutOptions.Start };
								Button imageButton = new Button() { HeightRequest = RecPosterHeight, WidthRequest = RecPosterWith, BackgroundColor = Color.Transparent, VerticalOptions = LayoutOptions.Start };
								var ff = new FFImageLoading.Forms.CachedImage {
									Source = posterURL,
									HeightRequest = RecPosterHeight,
									WidthRequest = RecPosterWith,
									BackgroundColor = Color.Transparent,
									VerticalOptions = LayoutOptions.Start,
									Transformations = {
                            //  new FFImageLoading.Transformations.RoundedTransformation(10,1,1.5,10,"#303F9F")
                                    new FFImageLoading.Transformations.RoundedTransformation(10, 1, 1.5, 0, "#303F9F")
									},
									InputTransparent = true,
								};

								// ================================================================ RECOMMENDATIONS CLICKED ================================================================
								// stackLayout.SetValue(XamEffects.BorderView.CornerRadiusProperty, 20);

								var brView = new BorderView() { VerticalOptions = LayoutOptions.Fill, HorizontalOptions = LayoutOptions.Fill, CornerRadius = 5 };

								brView.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
								Commands.SetTap(brView, new Command((o) => {
									var z = (BookmarkPoster)o;
									PushPageFromUrlAndName(z.id, z.name);
								}));
								var _b = new BookmarkPoster() { id = id, name = name, posterUrl = posterUrl };
								Commands.SetTapParameter(brView, _b);


								bookmarkPosters.Add(_b);

								// var _color = Settings.BlackColor + 5;

								BoxView boxView = new BoxView() {
									BackgroundColor = Settings.ItemBackGroundColor,// Color.FromRgb(_color, _color, _color),
									InputTransparent = true,
									CornerRadius = 10,
									HeightRequest = RecPosterHeight + bookmarkLabelTransY,
									TranslationY = 0,
									WidthRequest = RecPosterWith,
								};

								stackLayout.Children.Add(boxView);
								stackLayout.Children.Add(ff);
								stackLayout.Children.Add(imageButton);
								stackLayout.Children.Add(new Label() { Text = name, VerticalOptions = LayoutOptions.Start, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center, Padding = 1, TextColor = Color.White, MaxLines = 2, ClassId = "OUTLINE", TranslationY = RecPosterHeight });
								stackLayout.Children.Add(brView);

								if (Settings.ShowNextEpisodeReleaseDate) {
									CloudStreamCore.NextAiringEpisodeData? nextData = App.GetKey<NextAiringEpisodeData?>(App.NEXT_AIRING, id, null);
									if (nextData.HasValue) {
										var nextAir = nextData.Value;
										string nextTxt = nextAir.airingAt > UnixTime ? $"{CoreHelpers.ConvertUnixTimeToString(nextAir.airingAt)}" : "NEW!";
										stackLayout.Children.Add(new Button() {
											//BackgroundColor = new Color(0.188, 0.247, 0.623, 0.8)
											BackgroundColor = new Color(0, 0, 0, 0.4)
											,
											Text = nextTxt,
											ClassId = "CUST",
											TextColor = Color.White,
											InputTransparent = true,
											HeightRequest = 20,
											FontSize = 11,
											VerticalOptions = LayoutOptions.Start,
											HorizontalOptions = LayoutOptions.End,
											CornerRadius = 1,
											Padding = 2,
											Margin = 0,
											Scale = 1,
											TranslationX = 1,
											WidthRequest = 60
										});
									}
								}

								stackLayout.Opacity = 0;

								async void WaitUntillComplete()
								{
									stackLayout.Opacity = 0;
									while (!allDone) {
										await Task.Delay(50);
									}
									await stackLayout.FadeTo(1, (uint)(200 + index * 50), Easing.Linear);
								}

								WaitUntillComplete();

								index++;
								Bookmarks.Children.Add(stackLayout);

								/*
                                Grid stackLayout = new Grid();


                                 var ff = new FFImageLoading.Forms.CachedImage {
                                    Source = posterUrl,
                                    HeightRequest = height,
                                    WidthRequest = 87,
                                    BackgroundColor = Color.Transparent,
                                    VerticalOptions = LayoutOptions.Start,
                                    Transformations = {
                                    new FFImageLoading.Transformations.RoundedTransformation(1,1,1.5,0,"#303F9F")
                                },
                                    InputTransparent = true,
                                };

                                //Source = p.posterUrl

                                stackLayout.Children.Add(ff);
                                // stackLayout.Children.Add(imageButton);
                                bookmarkPosters.Add(new BookmarkPoster() { id = id, name = name, posterUrl = posterUrl });
                                Grid.SetColumn(stackLayout, Bookmarks.Children.Count);
                                Bookmarks.Children.Add(stackLayout);

                                // --- RECOMMENDATIONS CLICKED -----
                                stackLayout.SetValue(XamEffects.TouchEffect.ColorProperty, Color.White);
                                Commands.SetTap(stackLayout, new Command((o) => {
                                    int z = (int)o;
                                    PushPageFromUrlAndName(bookmarkPosters[z].id, bookmarkPosters[z].name);
                                 }));
                                Commands.SetTapParameter(stackLayout, i);*/

							}
						}
						// data.Add(App.GetKey("BookmarkData"))
					}
					//await Task.Delay(100);

					//MScroll.HeightRequest = keys.Count > 0 ? 130 : 0;

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
		public double currentWidth { get { return Application.Current.MainPage.Width; } }

		private void Grid_BindingContextChanged(object sender, EventArgs e)
		{
			var s = ((Grid)sender);
			Commands.SetTap(s, new Command((o) => {
				var episodeResult = ((EpisodeResult)o);
				PushPageFromUrlAndName(FindShortend(episodeResult.extraInfo, "Id"), FindShortend(episodeResult.extraInfo, "Name"));
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

		public event EventHandler Added;

		public MainEpisode100View()
		{
			MyEpisodeResultCollection = new ObservableCollection<EpisodeResult>();
		}
	}

}