using CloudStreamForms.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Core.CoreHelpers;

namespace CloudStreamForms.InterfacePages
{
	class MovieResultHolder
	{
		bool IsDead { get { return core == null; } }

		public int MaxTrailers = 4;
		public enum PickerType { SeasonPicker = 0, DubPicker = 1, EpisodeFromToPicker = 2, }
		public enum LabelType { NameLabel = 0, YearLabel = 1, RatingLabel = 2, DescriptionLabel = 3 }
		public enum ButtonType { SkipAnimeBtt = 0, BatchDownloadPicker = 1 }

		public EventHandler<bool> OnRunningChanged;
		public bool isRunning;
		public void ChangeRunning(bool _isRunning)
		{
			isRunning = _isRunning;
			OnRunningChanged?.Invoke(null, _isRunning);
		}

		public struct FadePickerEvent
		{
			public PickerType picker;
			public bool isVis;
		}

		//	public EventHandler<FadePickerEvent> FadePickerChanged;
		public event EventHandler<Movie> TitleLoaded;
		public event EventHandler<MALData> MalDataLoaded;

		public struct BackgroundImageEvent
		{
			public string posterUrl;
		}

		public EventHandler<BackgroundImageEvent> OnBackgroundChanged;

#pragma warning disable CS0649

		public class ButtonInfo
		{
			public bool isVisible;
			public string text;
			public ButtonType button;
		}

		public class LabelInfo
		{
			public bool isVisible;
			public string text;
			public LabelType label;
		}

#pragma warning restore CS0649

		public class PickerInfo
		{
			public bool isVisible;
			public string Text { get { return index == -1 ? "" : source[index]; } }
			public int index;
			public List<string> source;
			public PickerType picker;
		}

		public ButtonInfo[] buttons;
		public LabelInfo[] labels;
		public PickerInfo[] pickers;

		public EventHandler<LabelInfo> OnTextChanged;
		public EventHandler<ButtonInfo> OnBttChanged;
		public EventHandler<PickerInfo> OnPickerChanged;

		public string tId;


		public struct ReleaseDateEvent
		{
			public NextAiringEpisodeData nextAir;
			public string FormatedDate {
				get {
					return $"Next Episode {nextAir.episode}: {ConvertUnixTimeToString(nextAir.airingAt)}";
				}
			}
		}

		public EventHandler<ReleaseDateEvent> OnDateAdded;

		void ChangeText(LabelType label, string text)
		{
			var lbl = labels[(int)label];
			lbl.text = text ?? "";
			lbl.isVisible = text != null;
			OnTextChanged?.Invoke(null, lbl);
		}

		void ChangeText(ButtonType button, string text)
		{
			var btt = buttons[(int)button];
			btt.text = text ?? "";
			btt.isVisible = text != null;
			OnBttChanged?.Invoke(null, btt);
		}

		void ChangePicker(PickerType picker, List<string> source = null, int? index = null, bool? isVis = null)
		{
			var pick = pickers[(int)picker];
			pick.index = index ?? pick.index;
			pick.source = source ?? pick.source;
			pick.isVisible = isVis ?? pick.isVisible;

			OnPickerChanged?.Invoke(null, pick);
		}

		public bool hasSkipedLoading = false;

		public void SkipAnimeLoading()
		{
			hasSkipedLoading = true;
			core.shouldSkipAnimeLoading = true;
			ChangeText(ButtonType.SkipAnimeBtt, null);
		}

		public void Init(string id, string name, string year)
		{
			core = new CloudStreamCore();

			var _pickers = App.GetEnumList<PickerType>();
			pickers = new PickerInfo[_pickers.Count];
			for (int i = 0; i < _pickers.Count; i++) {
				pickers[i] = new PickerInfo() {
					picker = _pickers[i],
					index = 0,
					isVisible = false,
					source = new List<string>(),
				};
			}
			var _labels = App.GetEnumList<LabelType>();
			labels = new LabelInfo[_labels.Count];
			for (int i = 0; i < _labels.Count; i++) {
				labels[i] = new LabelInfo() { isVisible = true, label = _labels[i], text = "" };
			}
			var _buttons = App.GetEnumList<ButtonType>();
			buttons = new ButtonInfo[_buttons.Count];
			for (int i = 0; i < _buttons.Count; i++) {
				buttons[i] = new ButtonInfo() { text = "", isVisible = false, button = _buttons[i] };
			}

			ChangeText(LabelType.NameLabel, name);
			ChangeText(LabelType.YearLabel, year);

			core.FishProgressLoaded += (o, e) => {
				if (IsDead) return;
				if (!hasSkipedLoading) {
					ChangeText(ButtonType.SkipAnimeBtt, $"Skip - {e.currentProgress} of {e.maxProgress}");
					if (e.progressProcentage >= 1) {
						hasSkipedLoading = true;
						ChangeText(ButtonType.SkipAnimeBtt, null);
					}
				}
			};
			tId = id.Replace("https://imdb.com/title/", "");

			core.TitleLoaded += Core_titleLoaded;
			core.EpisodeHalfLoaded += EpisodesHalfLoaded;
			core.EpisodeLoaded += Core_episodeLoaded;
			core.MalDataLoaded += Core_malDataLoaded;
			core.GetImdbTitle(new Poster() { year = year, name = name, url = id });
		}

		private void Core_malDataLoaded(object sender, MALData e)
		{
			MalDataLoaded?.Invoke(null, e);
		}

		public struct EpisodeData
		{
			public int Episode;
			public int Season;
			public string IMDBEpisodeId;
			public string Title;
			public int Id;
			public string Description;
			public string PosterUrl;
			public string Rating;
		}

		EpisodeData[] allEpisodes;
		public EpisodeData[] exposedEpisodes;
		public EventHandler<EpisodeData[]> OnExposedEpisodesChanged;

		int maxEpisodes;
		List<Episode> CurrentEpisodes { get { return CurrentMovie.episodes; } }

		void AddEpisode(EpisodeData data, int index)
		{
			allEpisodes[index] = data;
		}

		void SetEpisodeFromTo(int segment, int max = -1)
		{
			if (IsDead) return;
			int start = MovieResultMainEpisodeView.MAX_EPS_PER * segment;
			if (max == -1) {
				max = allEpisodes.Length;
			}
			else {
				max = Math.Min(max, allEpisodes.Length);
			}

			int end = Math.Min(MovieResultMainEpisodeView.MAX_EPS_PER * (segment + 1), max);
			exposedEpisodes = allEpisodes[start..end];
			OnExposedEpisodesChanged?.Invoke(null, exposedEpisodes);
			ChangeRunning(false);
		}

		void SetChangeTo(int maxEp = -1)
		{
			if (maxEp == -1) {
				maxEp = maxEpisodes;
			}
			var source = new List<string>();

			int times = (int)Math.Ceiling((decimal)maxEp / (decimal)MovieResultMainEpisodeView.MAX_EPS_PER);

			for (int i = 0; i < times; i++) {
				int fromTo = maxEp - i * MovieResultMainEpisodeView.MAX_EPS_PER;
				string f = (i * MovieResultMainEpisodeView.MAX_EPS_PER + 1) + "-" + ((i) * MovieResultMainEpisodeView.MAX_EPS_PER + Math.Min(fromTo, MovieResultMainEpisodeView.MAX_EPS_PER));
				source.Add(f);
			}

			ChangePicker(PickerType.EpisodeFromToPicker, source, 0, times > 1);
		}

		private void Core_episodeLoaded(object sender, List<Episode> e)
		{
			if (IsDead) return;
			if (e == null || e.Count == 0) {
				ChangeRunning(false);
				return;
			};
			bool isAnime = CurrentMovie.title.movieType == MovieType.Anime;

			if (!isMovie) {
				allEpisodes = new EpisodeData[CurrentEpisodes.Count];
				maxEpisodes = allEpisodes.Length;
				bool dateAdded = isAnime;
				for (int i = 0; i < CurrentEpisodes.Count; i++) {
					AddEpisode(new EpisodeData() { Episode = i + 1, IMDBEpisodeId = CurrentEpisodes[i].id, Title = CurrentEpisodes[i].name, Id = i, Description = CurrentEpisodes[i].description.Replace("\n", "").Replace("  ", ""), PosterUrl = CurrentEpisodes[i].posterUrl, Rating = CurrentEpisodes[i].rating, Season = currentSeason }, i);
					var _date = CurrentEpisodes[i].date;
					if (!dateAdded && _date.IsClean()) {
						try {
							var unixReleaseTime = DateTimeOffset.Parse(_date).ToUnixTimeSeconds();
							if (unixReleaseTime > UnixTime) {
								dateAdded = true;
								App.SetKey(App.NEXT_AIRING, tId, new NextAiringEpisodeData { airingAt = unixReleaseTime, source = AirDateType.IMDb, episode = i + 1, refreshId = -1 });
							}
						}
						catch (Exception _ex) {
							error(_ex);
						}
					}
				}
				if (!isAnime) {
					SetEpisodeFromTo(0);
					SetChangeTo();
				}
			}
			else { // MOVE
				maxEpisodes = 1;
				allEpisodes = new EpisodeData[1];
				AddEpisode(new EpisodeData() { Episode = 1, Rating = CurrentMovie.title.rating, Title = CurrentMovie.title.name, IMDBEpisodeId = CurrentMovie.title.id, Description = CurrentMovie.title.description, Id = 0, PosterUrl = "", Season = currentSeason }, 0);
				SetEpisodeFromTo(0);
			}

			if (isAnime) {
				core.GetSubDub(currentSeason, out bool subExists, out bool dubExists);

				isDub = dubExists && Settings.DefaultDub;

				List<string> dubSource = new List<string>();

				if (Settings.DefaultDub) {
					if (dubExists) {
						dubSource.Add("Dub");
					}
				}
				if (subExists) {
					dubSource.Add("Sub");
				}
				if (!Settings.DefaultDub) {
					if (dubExists) {
						dubSource.Add("Dub");
					}
				}
				ChangePicker(PickerType.DubPicker, dubSource, 0, true);
				SetDubExist();
			}
		}
		readonly static Dictionary<string, bool> GetLatestDub = new Dictionary<string, bool>();
		void ChangeBatchDownload()
		{
			bool canBatchDownload = exposedEpisodes.Length > 1;
			ChangeText(ButtonType.BatchDownloadPicker, canBatchDownload ? "Download All" : null);
		}

		void SetDubExist()
		{
			TempThread tempThred = core.CreateThread(6);
			core.StartThread("Set SUB/DUB", () => {
				try {
					if (IsDead) return;
					int max = core.GetMaxEpisodesInAnimeSeason(currentSeason, isDub, tempThred);
					if (max > 0) {
						if (IsDead) return;
						max = Math.Min(max, maxEpisodes);

						SetEpisodeFromTo(0, max);
						SetChangeTo(max);
						if (IsDead) return;

						// CLEAR EPISODES SO SWITCHING SUB DUB 
						if (GetLatestDub.ContainsKey(CurrentMovie.title.id)) {
							if (GetLatestDub[CurrentMovie.title.id] != isDub) {
								try {
									for (int i = 0; i < exposedEpisodes.Length; i++) {
										CloudStreamCore.ClearCachedLink(exposedEpisodes[i].IMDBEpisodeId);
									}
								}
								catch (Exception _ex) {
									print("MAIN ERROR IN CLEAR: " + _ex);
								}
							}
						}
						GetLatestDub[CurrentMovie.title.id] = isDub;
						ChangeBatchDownload();
					}
					else {
						ChangeRunning(false);
					}
				}
				finally {
					if (core != null) {
						core.JoinThred(tempThred);
					}
				}
			});
		}

		private void EpisodesHalfLoaded(object sender, List<Episode> e)
		{
			if (e.Count > 0) {
				if (setFirstEpAsFade) {
					OnBackgroundChanged?.Invoke(null, new BackgroundImageEvent() { posterUrl = e[0].posterUrl });
				}
			}
		}

		/// <summary>
		/// If this is true, then it will chose the first poster (ep) as the poster
		/// </summary>
		bool setFirstEpAsFade = false;

		void FadePicker(PickerType picker, bool isVis)
		{
			ChangePicker(picker, isVis: isVis);
			//OnPickerChanged?.Invoke(null,)
			//	FadePickerChanged?.Invoke(null, new FadePickerEvent() { isVis = isVis, picker = picker });
		}

		public void SeasonPickerSelectIndex(int index)
		{
			ChangeRunning(true);

			currentSeason = index + 1;
			FadePicker(PickerType.DubPicker, false);
			ChangeText(ButtonType.BatchDownloadPicker, null);
			FadePicker(PickerType.EpisodeFromToPicker, false);
			ChangePicker(PickerType.SeasonPicker, index: index);

			App.SetKey("SeasonIndex", core.activeMovie.title.id, index);
			core.GetImdbEpisodes(currentSeason);
		}

		public void DubPickerSelectIndex(bool _isDub)
		{
			isDub = _isDub;
			SetDubExist();
		}

		public void SelFromToPickerSelectIndex(int index)
		{
			SetEpisodeFromTo(index);
		}

		private void Core_titleLoaded(object sender, Movie e)
		{
			if (IsDead) return;
			isMovie = (e.title.movieType == MovieType.Movie || e.title.movieType == MovieType.AnimeMovie);
			TitleLoaded?.Invoke(null, e);

			setFirstEpAsFade = true;

			try {
				string souceUrl = e.title.trailers.First().PosterUrl;
				if (CheckIfURLIsValid(souceUrl)) {
					OnBackgroundChanged?.Invoke(null, new BackgroundImageEvent() { posterUrl = souceUrl });
					setFirstEpAsFade = false;
				}
				else {
					OnBackgroundChanged?.Invoke(null, new BackgroundImageEvent() { posterUrl = null });
				}
			}
			catch (Exception) {
				OnBackgroundChanged?.Invoke(null, new BackgroundImageEvent() { posterUrl = null });
			}

			string rYear = e.title.year;
			ChangeText(LabelType.YearLabel, ((rYear + " | " + e.title.runtime).Replace("|  |", "|")).Replace("|", "  "));
			ChangeText(LabelType.RatingLabel, "Rated: " + e.title.rating);
			ChangeText(LabelType.DescriptionLabel, Settings.MovieDecEnabled ? CloudStreamCore.RemoveHtmlChars(e.title.description) : "");
			bool haveSeasons = e.title.seasons != 0;

			if (haveSeasons) {
				List<string> seasonList = new List<string>();
				for (int i = 1; i <= e.title.seasons; i++) {
					seasonList.Add("Season " + i);
				}

				int selIndex = App.GetKey<int>("SeasonIndex", core.activeMovie.title.id, 0);
				try {
					selIndex = Math.Min(selIndex, seasonList.Count - 1);
				}
				catch (Exception) {
					selIndex = 0; // JUST IN CASE
				}

				ChangePicker(PickerType.SeasonPicker, seasonList, selIndex, true);

				currentSeason = selIndex + 1;

				core.GetImdbEpisodes(currentSeason);
			}
			else {
				currentSeason = 0; // MOVIES
				core.GetImdbEpisodes();
			}


			if (Settings.ShowNextEpisodeReleaseDate) {
				void UpdateNextEpisodeInfoUI(NextAiringEpisodeData nextAir)
				{
					OnDateAdded?.Invoke(null, new ReleaseDateEvent() {
						nextAir = nextAir,
					});
				}

				CloudStreamCore.NextAiringEpisodeData? data = App.GetKey<NextAiringEpisodeData?>(App.NEXT_AIRING, tId, null);
				if (data.HasValue) {
					var nextAir = data.Value;
					if (nextAir.airingAt > UnixTime) {
						UpdateNextEpisodeInfoUI(nextAir);
					}
					else {
						Task.Run(async () => {
							var _next = await core.RefreshNextEpisodeData(nextAir);
							if (_next.HasValue) {
								if (_next.Value.airingAt > UnixTime) { // JUST IN CASE SOMEONE HAS MESSED W TIME
									App.SetKey(App.NEXT_AIRING, tId, _next);
									UpdateNextEpisodeInfoUI(_next.Value);
								}
							}
							else {
								App.RemoveKey(App.NEXT_AIRING, tId);
								//Home.UpdateIsRequired = true;
							}
						});
					}
				}
			}
		}

		public CloudStreamCore core;
		public Movie CurrentMovie { get { return core.activeMovie; } }
		public bool isDub = true;
		public bool isMovie = false;
		public int currentSeason = 0;
		//List<Poster> RecomendedPosters { get { return CurrentMovie.title.recomended; } }

		public string CurrentMalLink {
			get {
				try {
					string s = CurrentMovie.title.MALData.seasonData[currentSeason].malUrl;
					if (s != "https://myanimelist.net") {
						return s;
					}
					else {
						return "";
					}
				}
				catch (Exception) {
					return "";
				}
			}
		}

		public string CurrentAniListLink {
			get {
				try {
					string s = CurrentMovie.title.MALData.seasonData[currentSeason].aniListUrl;
					if (s.IsClean()) {
						return s;
					}
					else {
						return "";
					}
				}
				catch (Exception) {
					return "";
				}
			}
		}

		public void Dispose()
		{
			if (core != null) {
				core.PurgeThreads(-1);
				core = null;
			}
		}
	}
}
