using CloudStreamForms.Core;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.ReviewPage;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ReviewPage : ContentPage
	{
		public const int MIN_MAXLINES = 3;
		public const int MAX_MAXLINES = -1;
		ReviewHolder holder;
		readonly string mainId;

		private ObservableCollection<ReviewItem> _MyEpisodeResultCollection;
		public ObservableCollection<ReviewItem> MyEpisodeResultCollection { set { Added?.Invoke(null, null); _MyEpisodeResultCollection = value; } get { return _MyEpisodeResultCollection; } }

		public event EventHandler Added;

		private bool _isRefreshing = false;
		public bool IsRefreshing {
			get { return _isRefreshing; }
			set {
				_isRefreshing = value;
				OnPropertyChanged(nameof(IsRefreshing));
			}
		}

		public ICommand RefreshCommand {
			get {
				return new Command(async () => {
					IsRefreshing = true;

					await Task.Delay(100);
					// await RefreshData();

					IsRefreshing = false;
				});
			}
		}

		void SetHeight()
		{
			Device.BeginInvokeOnMainThread(() => episodeView.HeightRequest = 10000);//episodeView.HeightRequest = MyEpisodeResultCollection.Count * episodeView.RowHeight + 200);
		}

		void TryGetNewReviews(bool firstTime = false, int tryc = 0)
		{
			if (tryc >= 5) return;
			if (holder.isSearchingforReviews) return;
			if (!firstTime && !holder.ajaxKey.IsClean()) return;
			holder.isSearchingforReviews = true;



			TempThread tempThred = mainCore.CreateThread(9);
			mainCore.StartThread("Reviews Thread", () => {
				try {
					var h = mainCore.GetReview(holder, mainId);
					if (h != null) {
						holder = (ReviewHolder)h;
					}
					else {
						holder.ajaxKey = "";
					}
					//if (!GetThredActive(tempThred)) { return; }; // COPY UPDATE PROGRESS
				}
				finally {
					var bgColor = Settings.ItemBackGroundColor;
					mainCore.JoinThred(tempThred);
					if (holder.reviews != null) {
						Device.BeginInvokeOnMainThread(() => {
							MyEpisodeResultCollection.Clear();
							int index = 0;
							foreach (var rew in holder.reviews) {
								int _index = index;
								MyEpisodeResultCollection.Add(new ReviewItem() {
									ClickToExpand = new Command(() => {
										TapCell(_index);
									}),
									ExtraColor = bgColor,
									Author = rew.author,
									Date = rew.date,
									Rating = $"★ {rew.rating}",
									SpoilerText = rew.containsSpoiler ? "Contains Spoilers" : "",
									Text = rew.text,
									Title = rew.title,
									IsExpanded = false,
									index = index
								});
								index++;
							}
							SetHeight();
							if (firstTime) {
								episodeView.FadeTo(1, 200);
								MainLoading.IsEnabled = false;
								MainLoading.IsVisible = false;
							}
							holder.isSearchingforReviews = false;
						});
					}
					if (holder.reviews == null) {
						holder.isSearchingforReviews = false;
						TryGetNewReviews(firstTime, tryc: ++tryc);
					}

					holder.isSearchingforReviews = false;
				}
			});
		}

		public static bool isOpen = false;

		public ReviewPage(string id, string reviewTitle)
		{
			isOpen = true;
			mainId = id;
			InitializeComponent();
			episodeView.Opacity = 0;
			MyEpisodeResultCollection = new ObservableCollection<ReviewItem>();
			BackgroundColor = Settings.BlackRBGColor;
			ReviewTitle.Text = "Reviews for " + reviewTitle;
			BindingContext = this;
			episodeView.Scrolled += (o, e) => {
				//double maxY =  e.ScrollX  
				//print(maxY);
				/*if (e.ScrollY >= maxY - 200) {
					TryGetNewReviews();
				}*/
			};
		}

		protected override void OnDisappearing()
		{
			base.OnDisappearing();
			isOpen = false;
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			TryGetNewReviews(true);
		}

		void TapCell(int index)
		{
			ReviewItem episodeResult = MyEpisodeResultCollection[index];
			MyEpisodeResultCollection.Remove(episodeResult);
			episodeResult.IsExpanded = !episodeResult.IsExpanded;
			MyEpisodeResultCollection.Insert(episodeResult.index, episodeResult);
			var _e = MyEpisodeResultCollection.ToList();

			Device.BeginInvokeOnMainThread(() => {
				MyEpisodeResultCollection.Clear();
				for (int i = 0; i < _e.Count; i++) {
					MyEpisodeResultCollection.Add((ReviewItem)_e[i].Clone());
				}
				episodeView.SelectedItem = null;

				SetHeight();

			});
		}

		private void episodeView_ItemTapped(object sender, ItemTappedEventArgs e)
		{
			episodeView.SelectedItem = null;
		}

		private void ViewCell_Appearing(object sender, EventArgs e)
		{
			ReviewItem episodeResult = (ReviewItem)(((ViewCell)sender).BindingContext);
			if (episodeResult.index > MyEpisodeResultCollection.Count - 20) {
				TryGetNewReviews();
			}
		}
	}

	[Serializable]
	public class ReviewItem : ICloneable
	{
		public int index;
		public string Author { get; set; }
		public string Date { get; set; }
		public string Text { get; set; }
		public string Title { set; get; }
		public string Rating { get; set; }
		public string SpoilerText { get; set; }
		public int MaxLines { get { return IsExpanded ? MAX_MAXLINES : MIN_MAXLINES; } }
		public bool IsExpanded { set; get; }
		public Command ClickToExpand { set; get; }
		public Color ExtraColor { set; get; }
		public object Clone()
		{
			return this.MemberwiseClone();
		}
	}
}