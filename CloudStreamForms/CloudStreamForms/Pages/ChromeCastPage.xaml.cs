using CloudStreamForms.Core;
using CloudStreamForms.Models;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Core.MainChrome;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ChromeCastPage : ContentPage
	{
		//   public EpisodeResult episodeResult;
		//     public Movie chromeMovieResult;

		public string TitleName { set { NameLabel.Text = value; } }
		public string DescriptName { set { EpsodeName.Text = value; } }
		public string EpisodeTitleName { set { EpTitleLabel.Text = value; } }
		public string EpisodePosterUrl { set {/* EpisodePoster.Source = value; */} }
		public string EpisodeDescription { set { EpTitleDescript.Text = value; /* EpisodePoster.Source = value; */} }

		public string DescriptSetName { get { return currentChromeData.isFromFile ? (currentChromeData.movieType.IsMovie() ? $"{currentChromeData.episodeTitleName}" : $"S{currentChromeData.season}:E{currentChromeData.episode} - {currentChromeData.episodeTitleName}") : currentChromeData.MirrorsNames[currentSelected]; } }

		public string PosterUrl { set { Poster.Source = value; } }
		public int IconSize { set; get; } = 48;
		public int BigIconSize { set; get; } = 60;

		public int FastForwardTime {
			get { return Settings.ChromecastSkipTime; }
		}
		public int BackForwardTime {
			get { return Settings.ChromecastSkipTime; }
		}

		public float ScaleAll { set; get; } = 1.4f;
		public float ScaleAllBig { set; get; } = 2f;

		public static int currentSelected = 0;
		public static double changeTime = -1;
		readonly object subtitleMutex = new object();
		public static List<Subtitle> subtitles = new List<Subtitle>();
		public static int subtitleIndex = -1;
		public static bool HasSubtitlesOn = false;
		public static int subtitleDelay = 0;

		public async void SelectSubtitleOption()
		{
			List<string> options = new List<string>();
			if (subtitles.Count == 0) {
				options.Add($"Download Subtitles ({Settings.NativeSubLongName})");
			}
			else {
				// if (subtitleIndex != -1) {
				// options.Add($"Turn {(HasSubtitlesOn ? "Off" : $"On ({subtitles[subtitleIndex].name})")}");
				//}
				if (HasSubtitlesOn) {
					//if (subtitleIndex != -1) {
					//  options.Add($"Turn off");
					//}
					options.Add($"Change Delay ({subtitleDelay} ms)");
				}
				options.Add("Select Subtitles");
			}
			options.Add("Download Subtitles");

			static async Task UpdateSubtitles()
			{
				_ = ActionPopup.StartIndeterminateLoadinbar("Loading Subtitles...");
				if (subtitleIndex != -1) {
					await ChangeSubtitles(subtitles[subtitleIndex].data, subtitles[subtitleIndex].name, subtitleDelay);
				}
				else {
					await ChangeSubtitles("", "", 0);
				}
				await ActionPopup.StopIndeterminateLoadinbar();
			}

			string action = await ActionPopup.DisplayActionSheet("Subtitles", options.ToArray());
			if (action == "Download Subtitles") {
				string subAction = await ActionPopup.DisplayActionSheet("Download Subtitles", subtitleNames);
				if (subAction != "Cancel") {
					int index = subtitleNames.IndexOf(subAction);
					PopulateSubtitle(subtitleShortNames[index], subAction);
				}
			}
			else if (action == "Select Subtitles") {
				List<string> subtitlesList = subtitles.Select(t => t.name).ToList();
				subtitlesList.Insert(0, "None");

				string subAction = await ActionPopup.DisplayActionSheet("Select Subtitles", subtitleIndex + 1, subtitlesList.ToArray());

				if (subAction != "Cancel") {
					int setTo = subtitlesList.IndexOf(subAction) - 1;
					if (setTo != subtitleIndex) {
						subtitleIndex = setTo;
						HasSubtitlesOn = subtitleIndex != -1;
						await Task.Delay(100);
						await UpdateSubtitles();
					}
				}
			}
			else if (action == $"Download Subtitles ({Settings.NativeSubLongName})") {
				PopulateSubtitle();
			}
			else if (action.StartsWith("Turn off")) {
				subtitleIndex = -1;
				await UpdateSubtitles();
				// await MainChrome.ToggleSubtitles(!HasSubtitlesOn);
				HasSubtitlesOn = !HasSubtitlesOn;
			}
			else if (action.StartsWith("Change Delay")) {
				int del = await ActionPopup.DisplayIntEntry("ms", "Subtitle Delay", 50, false, subtitleDelay.ToString(), "Set Delay");
				if (del != -1) {
					subtitleDelay = del;
					await Task.Delay(100);
					await UpdateSubtitles();
				}
			}
		}

		readonly Dictionary<string, bool> searchingForLang = new Dictionary<string, bool>();

		public void PopulateSubtitle(string lang = "", string name = "")
		{
			if (lang == "") {
				lang = Settings.NativeSubShortName;
				name = Settings.NativeSubLongName;
			}

			bool ContainsLang()
			{
				lock (subtitleMutex) {
					return subtitles.Where(t => t.name == name).Count() != 0;
				}
			}

			if (ContainsLang()) {
				App.ShowToast("Subtitle already downloaded"); // THIS SHOULD NEVER HAPPEND
				return;
			}

			if (searchingForLang.ContainsKey(lang)) {
				App.ShowToast("Searching for subtitles");
				return;
			}
			searchingForLang[lang] = true;

			var thread = mainCore.CreateThread(6);
			mainCore.StartThread("PopulateSubtitles", () => {
				try {
					string data = mainCore.DownloadSubtitle(currentChromeData.episodeId, lang, false, true); // MovieResult.GetId(episodeResult, chromeMovieResult)
					if (data.IsClean()) {
						if (!ContainsLang()) {
							lock (subtitleMutex) {
								Subtitle s = new Subtitle {
									name = name,
									data = data
								};
								subtitles.Add(s);
							}
							App.ShowToast(name + " subtitles added");
						}
					}
					else {
						App.ShowToast(data == null ? "Connection error" : "No subtitles found");
					}
				}
				finally {
					if (searchingForLang.ContainsKey(lang)) {
						searchingForLang.Remove(lang);
					}
				}
			});
		}

		async void SelectMirror()
		{
			bool succ = false;
			currentSelected--;
			while (!succ) {
				currentSelected++;

				if (currentSelected >= currentChromeData.MirrorsNames.Count) {
					succ = true;
				}
				else {
					try {
						DescriptName = DescriptSetName;//currentChromeData.MirrorsNames[currentSelected];
					}
					catch (Exception) {

					}

					/*
                    string _sub = "";
                    if (chromeMovieResult.subtitles != null) {
                        if (chromeMovieResult.subtitles.Count > 0) {
                            _sub = chromeMovieResult.subtitles[0].data;
                        }
                    }
                    */
					if (MainChrome.CurrentTime > 120) {
						changeTime = MainChrome.CurrentTime;
						print("CHANGE TIME TO " + changeTime);
					}

					string subTxt = "";
					if (subtitleIndex != -1 && HasSubtitlesOn) {
						subTxt = subtitles[subtitleIndex].data;
					}
					//   succ = await MainChrome.CastVideo(episodeResult.mirrosUrls[currentSelected], episodeResult.Mirros[currentSelected], subtitleUrl: subTxt, posterUrl: chromeMovieResult.title.hdPosterUrl, movieTitle: chromeMovieResult.title.name, setTime: changeTime, subtitleDelay: subtitleDelay);
					succ = await MainChrome.CastVideo(currentChromeData.MirrorsUrls[currentSelected], currentChromeData.MirrorsNames[currentSelected], subtitleUrl: subTxt, posterUrl: currentChromeData.hdPosterUrl, movieTitle: currentChromeData.titleName, setTime: changeTime, subtitleDelay: subtitleDelay);

				}
			}
			try {
				DescriptName = DescriptSetName;// currentChromeData.MirrorsNames[currentSelected];//episodeResult.Mirros[currentSelected];
			}
			catch (Exception) {

			}

			// CastVideo(episodeResult.mirrosUrls[currentSelected], episodeResult.Mirros[currentSelected], CurrentTime);
		}

		void OnStop()
		{
			if (isActive) {
				Navigation.PopModalAsync();
			}
			isActive = false;
		}

		protected override bool OnBackButtonPressed()
		{
			isActive = false;
			return base.OnBackButtonPressed();
		}

		public static bool isActive = false;


		public void OnDisconnectedHandle(object sender, EventArgs e)
		{
			OnStop();
		}
		public void OnPauseChangedHandle(object sender, bool e)
		{
			SetPause(e);
		}
		public void OnForceUpdateTimeHandle(object sender, double e)
		{
			UpdateTxt();
		}

		static string lastId = "";
		//static List<Subtitle> lastSubtitles = new List<Subtitle>();

		public struct ChromecastData
		{
			public string episodeId;
			public string headerId;
			public int episode;
			public int season;
			public bool isFromFile;
			public List<string> MirrorsUrls;
			public List<string> MirrorsNames;

			public string titleName;
			public string episodeTitleName;
			public string descript;
			public string hdPosterUrl;
			public string episodePosterUrl;
			public MovieType movieType;
		}

		public static ChromecastData currentChromeData;

		public static ChromeCastPage CreateChromePage(EpisodeResult episodeResult, Movie movie)
		{
			return new ChromeCastPage(new ChromecastData() {
				descript = episodeResult.Description,
				episode = episodeResult.Episode,
				episodeId = episodeResult.IMDBEpisodeId,
				episodePosterUrl = episodeResult.PosterUrl,
				episodeTitleName = episodeResult.OgTitle,
				hdPosterUrl = movie.title.hdPosterUrl,
				headerId = movie.title.id,
				season = episodeResult.Season,
				isFromFile = false,
				MirrorsNames = episodeResult.GetMirros(),
				MirrorsUrls = episodeResult.GetMirrosUrls(),
				titleName = movie.title.name,
				movieType = movie.title.movieType
			});
		}

		public static ChromeCastPage CreateChromePage(DownloadEpisodeInfo info)
		{
			var header = Download.downloadHeaders[info.downloadHeader];

			return new ChromeCastPage(new ChromecastData() {
				descript = info.description,
				episode = info.episode,
				episodeId = info.episodeIMDBId,
				episodePosterUrl = info.hdPosterUrl,
				episodeTitleName = info.name,
				hdPosterUrl = header.movieType == MovieType.YouTube ? info.hdPosterUrl : header.hdPosterUrl,
				headerId = info.source,
				season = info.season,
				isFromFile = true,
				MirrorsNames = null,
				MirrorsUrls = null,
				titleName = header.name,
				movieType = header.movieType,
			});
		}

		public ChromeCastPage(ChromecastData data)
		{
			currentChromeData = data;
			isActive = true;
			//episodeResult = MovieResult.chromeResult;
			//  chromeMovieResult = MovieResult.chromeMovieResult;

			InitializeComponent();

			if (lastId != currentChromeData.episodeId) {//chromeMovieResult.title.id) {
				lastId = currentChromeData.episodeId;//chromeMovieResult.title.id;
				subtitles = new List<Subtitle>();
				subtitleDelay = 0;
				subtitleIndex = -1;
				HasSubtitlesOn = false;
				print("NOT THE SAME AT LAST ONE");
				if (GlobalSubtitlesEnabled) {
					PopulateSubtitle();
				}
			}
			else {
				//subtitles = lastSubtitles;
			}

			Subbutton.Source = App.GetImageSource("outline_subtitles_white_48dp.png");
			BindingContext = this;
			TitleName = currentChromeData.titleName;//chromeMovieResult.title.name;
			EpisodeTitleName = currentChromeData.episodeTitleName;//episodeResult.Title;
			PosterUrl = CloudStreamCore.ConvertIMDbImagesToHD(currentChromeData.hdPosterUrl, 150, 225);//chromeMovieResult.title.hdPosterUrl, 150, 225);
			EpisodePosterUrl = currentChromeData.episodePosterUrl;//episodeResult.PosterUrl;
			EpisodeDescription = currentChromeData.descript;//episodeResult.Description;
			BackgroundColor = Settings.BlackRBGColor;
			//  CloudStreamForms.MainPage.mainPage.BarBackgroundColor = Color.Transparent;
			ChromeLabel.Text = "Connected to " + MainChrome.chromeRecivever.FriendlyName;

			try {
				DescriptName = DescriptSetName;// currentChromeData.MirrorsNames[currentSelected];//episodeResult.Mirros[currentSelected];
			}
			catch (Exception _ex) {
				print("ERROR LOADING MIRROR " + _ex);
			}



			//https://material.io/resources/icons/?style=baseline
			VideoSlider.DragStarted += (o, e) => {
				draging = true;
			};


			VideoSlider.DragCompleted += (o, e) => {
				MainChrome.SetChromeTime(VideoSlider.Value * CurrentCastingDuration);
				draging = false;
				UpdateTxt();
			};

			const bool rotateAllWay = false;
			const int rotate = 90;
			const int time = 100;

			Commands.SetTap(FastForwardBtt, new Command(async () => {
				SeekMedia(FastForwardTime);
				FastForward.Rotation = 0;
				if (rotateAllWay) {
#pragma warning disable CS0162 // Unreachable code detected
					await FastForward.RotateTo(360, 200, Easing.SinOut);
#pragma warning restore CS0162 // Unreachable code detected
				}
				else {
					_ = FastForward.ScaleTo(0.9, time, Easing.SinOut);
					await FastForward.RotateTo(rotate, time, Easing.SinOut);
					_ = FastForward.ScaleTo(1, time, Easing.SinOut);
					await FastForward.RotateTo(0, time, Easing.SinOut);
				}
			}));

			Commands.SetTap(BackForwardBtt, new Command(async () => {
				SeekMedia(-BackForwardTime);
				BackForward.Rotation = 0;
				if (rotateAllWay) {
#pragma warning disable CS0162 // Unreachable code detected
					await BackForward.RotateTo(-360, 200, Easing.SinOut);
#pragma warning restore CS0162 // Unreachable code detected
				}
				else {
					_ = BackForward.ScaleTo(0.9, time, Easing.SinOut);
					await BackForward.RotateTo(-rotate, time, Easing.SinOut);
					_ = BackForward.ScaleTo(1, time, Easing.SinOut);
					await BackForward.RotateTo(0, time, Easing.SinOut);
				}
			}));

			StopAll.Clicked += (o, e) => {
				//  MainChrome.StopCast();
				JustStopVideo();
				OnStop();
			};

			if (currentChromeData.isFromFile) {
				UpperIconHolder.TranslationX = 20;
				LowerIconHolder.ColumnSpacing = 40;

				PlayList.IsEnabled = false;
				PlayList.IsVisible = false;

				SkipForward.IsEnabled = false;
				SkipForward.IsVisible = false;

				SkipBack.IsEnabled = false;
				SkipBack.IsVisible = false;

				Grid.SetColumn(Subbutton, 0);
				Grid.SetColumn(StopAll, 1);
				Grid.SetColumn(Audio, 2);

				if (currentChromeData.movieType == MovieType.YouTube) {
					Subbutton.IsEnabled = false;
					Subbutton.Opacity = 0;
				}
			}
			else {
				PlayList.Clicked += async (o, e) => {
					//ListScale();
					string a = await ActionPopup.DisplayActionSheet("Select Mirror", currentChromeData.MirrorsNames.ToArray()); //await DisplayActionSheet("Select Mirror", "Cancel", null, episodeResult.Mirros.ToArray());
																																//ListScale();
					for (int i = 0; i < currentChromeData.MirrorsNames.Count; i++) {
						if (a == currentChromeData.MirrorsNames[i]) {
							currentSelected = i;
							SelectMirror();
							return;
						}
					}
				};

				SkipForward.Clicked += async (o, e) => {
					currentSelected++;
					if (currentSelected > currentChromeData.MirrorsNames.Count) { currentSelected = 0; }
					SelectMirror();
					await SkipForward.TranslateTo(6, 0, 50, Easing.SinOut);
					await SkipForward.TranslateTo(0, 0, 50, Easing.SinOut);
				};

				SkipBack.Clicked += async (o, e) => {
					currentSelected--;
					if (currentSelected < 0) { currentSelected = currentChromeData.MirrorsNames.Count - 1; }
					SelectMirror();
					await SkipBack.TranslateTo(-6, 0, 50, Easing.SinOut);
					await SkipBack.TranslateTo(0, 0, 50, Easing.SinOut);
				};
			}
			ConstUpdate();

			MainChrome.Volume = (MainChrome.Volume);

			/*
            LowVol.Source = GetImageSource("round_volume_down_white_48dp.png");
            MaxVol.Source = GetImageSource("round_volume_up_white_48dp.png");*/

			//   UserDialogs.Instance.TimePrompt(new TimePromptConfig() { CancelText = "Cancel", Title = "da", Use24HourClock = false, OkText = "OK", IsCancellable = true });

		}

		bool draging = false;
		public async void ConstUpdate()
		{
			while (true) {
				await Task.Delay(1000);
				UpdateTxt();
			}
		}

		public void UpdateTxt()
		{
			StartTxt.Text = ConvertTimeToString(CurrentTime);
			EndTxt.Text = ConvertTimeToString(CurrentCastingDuration - CurrentTime);
			if (CurrentCastingDuration - CurrentTime < -1) {
				OnStop();
			}
			if (!draging) {
				VideoSlider.Value = CurrentTime / CurrentCastingDuration;
			}
		}

		const bool IsRounded = false;
		public static string RoundedPrefix { get { return IsRounded ? "round" : "baseline"; } }


		void SetPause(bool paused)
		{
			Pause.Source = paused ? "netflixPlay128v2.png" : "netflixPause128.png";//GetImageSource(paused ? "netflixPlay.png" : "netflixPause.png");//GetImageSource(RoundedPrefix + "_play_arrow_white_48dp.png") : GetImageSource(RoundedPrefix + "_pause_white_48dp.png");
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();
			App.UpdateToTransparentBg();
			//BackFaded.Source = GetImageSource("faded.png");
			PlayList.Source = GetImageSource(RoundedPrefix + "_playlist_play_white_48dp.png");
			StopAll.Source = GetImageSource(RoundedPrefix + "_stop_white_48dp.png");
			BackForward.Source = GetImageSource("netflixSkipBack.png");//GetImageSource(RoundedPrefix + "_replay_white_48dp.png");
			FastForward.Source = GetImageSource("netflixSkipForward.png"); // GetImageSource(RoundedPrefix + "_replay_white_48dp_mirror.png");
			SkipBack.Source = GetImageSource(RoundedPrefix + "_skip_previous_white_48dp.png");
			SkipForward.Source = GetImageSource(RoundedPrefix + "_skip_next_white_48dp.png");
			Audio.Source = App.GetImageSource("AudioVol3.png");//GetImageSource(RoundedPrefix + "_volume_up_white_48dp.png");
			SetPause(IsPaused);
			UpdateTxt();

			OnDisconnected += OnDisconnectedHandle;
			OnPauseChanged += OnPauseChangedHandle;
			OnForceUpdateTime += OnForceUpdateTimeHandle;
		}


		protected override void OnDisappearing()
		{
			string lastId = currentChromeData.episodeId;//MovieResult.GetId(episodeResult, chromeMovieResult);

			print("SETTITME::: : " + lastId + "|" + (long)(MainChrome.CurrentTime * 1000) + "|" + (long)(MainChrome.CurrentCastingDuration * 1000));
			App.SetViewPos(lastId, (long)(MainChrome.CurrentTime * 1000));
			App.SetViewDur(lastId, (long)(MainChrome.CurrentCastingDuration * 1000));
			App.ForceUpdateVideo?.Invoke(null, EventArgs.Empty);

			App.UpdateBackground();
			OnDisconnected -= OnDisconnectedHandle;
			OnPauseChanged -= OnPauseChangedHandle;
			OnForceUpdateTime -= OnForceUpdateTimeHandle;
			base.OnDisappearing();
		}

		private void AudioClicked(object sender, EventArgs e)
		{
			PopupNavigation.Instance.PushAsync(new CloudStreamForms.AudioPopupPage());
			ScaleAudio();
		}

		private void SubClicked(object sender, EventArgs e)
		{
			ScaleSub();
			SelectSubtitleOption();
		}

		async void ScaleSub()
		{
			Subbutton.AbortAnimation("ScaleTo");
			await Subbutton.ScaleTo(1.3, 100, Easing.SinOut);
			await Subbutton.ScaleTo(1.2, 100, Easing.SinOut);
		}

		async void ScaleAudio()
		{
			Audio.AbortAnimation("ScaleTo");
			await Audio.ScaleTo(1.1, 100, Easing.SinOut);
			await Audio.ScaleTo(1.0, 100, Easing.SinOut);
		}

		private void Pause_Clicked(object sender, EventArgs e)
		{
			SetPause(!IsPaused);
			PauseAndPlay(!IsPaused);
			PauseScale();
		}

		async void PauseScale()
		{
			Pause.Scale = 1.7;
			await Pause.ScaleTo(1.6, 50, Easing.SinOut);
			await Pause.ScaleTo(1.7, 50, Easing.SinOut);
		}
		/*
		async void ListScale()
		{
			PlayList.Scale = 1.4;
			await PlayList.ScaleTo(2, 50, Easing.SinOut);
			await PlayList.ScaleTo(1.4, 50, Easing.SinOut);
		}*/
	}
}