using CloudStreamForms.Core;
using CloudStreamForms.Cryptography;
using CloudStreamForms.Script;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamEffects;
using static CloudStreamForms.MainPage;
using static CloudStreamForms.Script.MALSyncApi;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Settings : ContentPage
	{
		public const bool SUBTITLES_INVIDEO_ENABELD = true;

		public const bool IS_TEST_BUILD = false;

		readonly LabelList ColorPicker;
		readonly LabelList SubLangPicker;
		readonly LabelList SubStylePicker;
		readonly LabelList SubFontPicker;
		LabelList VideoplayerOptionPicker;

		public string MainTxtColor { set; get; } = "#e1e1e1";


		public const string errorEpisodeToast = "No Links Found";

		public static int malTimeout = -1;

		public static int LoadingMiliSec {
			set {
				App.SetKey("Settings", nameof(LoadingMiliSec), value);
			}
			get {
				return App.GetKey("Settings", nameof(LoadingMiliSec), 5000);
			}
		}

		public static int PreferedVideoPlayer {
			set {
				App.SetKey("Settings", nameof(PreferedVideoPlayer), value);
			}
			get {
				return App.GetKey("Settings", nameof(PreferedVideoPlayer), -1);
			}
		}

		public static bool PremitM3u8Download {
			set {
				App.SetKey("Settings", nameof(PremitM3u8Download), value);
			}
			get {
				return App.GetKey("Settings", nameof(PremitM3u8Download), false);
			}
		}

		public static bool CheckDownloadLinkBefore {
			set {
				App.SetKey("Settings", nameof(CheckDownloadLinkBefore), value);
			}
			get {
				return App.GetKey("Settings", nameof(CheckDownloadLinkBefore), true);
			}
		}

		public static bool VideoPlayerShowSkip {
			set {
				App.SetKey("Settings", nameof(VideoPlayerShowSkip), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoPlayerShowSkip), true);
			}
		}

		public static string VideoDownloadLocation {
			set {
				App.SetKey("Settings", nameof(VideoDownloadLocation), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoDownloadLocation), "Download/{type}/");
			}
		}

		public static string VideoDownloadTvSeries {
			set {
				App.SetKey("Settings", nameof(VideoDownloadTvSeries), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoDownloadTvSeries), "{tname}/S{se}:E{ep} {name}");
			}
		}

		public static string VideoDownloadMovie {
			set {
				App.SetKey("Settings", nameof(VideoDownloadMovie), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoDownloadMovie), "{name}");
			}
		}

		public static bool ShowNextEpisodeReleaseDate {
			set {
				App.SetKey("Settings", nameof(ShowNextEpisodeReleaseDate), value);
			}
			get {
				return App.GetKey("Settings", nameof(ShowNextEpisodeReleaseDate), true);
			}
		}



		public static bool VideoPlayerSponsorblock {
			set {
				App.SetKey("Settings", nameof(VideoPlayerSponsorblock), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoPlayerSponsorblock), true);
			}
		}

		public static bool VideoPlayerSponsorblockAutoSkipAds {
			set {
				App.SetKey("Settings", nameof(VideoPlayerSponsorblockAutoSkipAds), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoPlayerSponsorblockAutoSkipAds), true);
			}
		}

		public static bool BackPressToHome {
			set {
				App.SetKey("Settings", nameof(BackPressToHome), value);
			}
			get {
				return App.GetKey("Settings", nameof(BackPressToHome), true);
			}
		}

		public static bool CacheNextEpisode {
			set {
				App.SetKey("Settings", nameof(CacheNextEpisode), value);
			}
			get {
				return App.GetKey("Settings", nameof(CacheNextEpisode), true);
			}
		}

		public static bool SubtitlesHasDropShadow {
			get {
				return SubtitleType == 1 || SubtitleType == 3;
			}
		}

		public static bool SubtitlesHasOutline {
			get {
				return SubtitleType >= 2;
			}
		}

		public static int SubtitlesSize {
			set {
				App.SetKey("Settings", nameof(SubtitlesSize), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesSize), 20);
			}
		}

		public static int SubtitlesEmptyTime {
			set {
				App.SetKey("Settings", nameof(SubtitlesEmptyTime), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesEmptyTime), 30);
			}
		}

		public static bool SubtitlesOutlineIsCentered {
			set {
				App.SetKey("Settings", nameof(SubtitlesOutlineIsCentered), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesOutlineIsCentered), true);
			}
		}

		public static int SubtitlesShadowStrenght {
			set {
				App.SetKey("Settings", nameof(SubtitlesShadowStrenght), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesShadowStrenght), 10);
			}
		}

		/// <summary>
		/// Disable or enable https://en.wikipedia.org/wiki/Closed_captioning 
		/// 
		/// I GOT THE MACHINE READY.
		/// [engine starting]
		/// </summary>
		public static bool SubtitlesClosedCaptioning {
			set {
				App.SetKey("Settings", nameof(SubtitlesClosedCaptioning), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesClosedCaptioning), true);
			}
		}

		public static bool AutoAddInternalSubtitles {
			set {
				App.SetKey("Settings", nameof(AutoAddInternalSubtitles), value);
			}
			get {
				return App.GetKey("Settings", nameof(AutoAddInternalSubtitles), true);
			}
		}

		public static readonly string[] SubtitleTypeNames = {
			"None", "Dropshadow", "Outline", "Outline + dropshadow"
		};

		public static bool IsTransparentNonMain = false;
		public static int TransparentAddPaddingEnd {
			get { return (Settings.IsTransparentNonMain ? 40 : 0); }
		}

		/// <summary>
		/// 0 = None, 1 = Dropshadow, 2 = Outline, 3 = Outline + dropshadow 
		/// </summary>
		public static int SubtitleType {
			set {
				App.SetKey("Settings", nameof(SubtitleType), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitleType), 1);
			}
		}

		public static bool ShowAppUpdate {
			set {
				App.SetKey("Settings", nameof(ShowAppUpdate), value);
			}
			get {
				return App.GetKey("Settings", nameof(ShowAppUpdate), true);
			}
		}

		public static bool IsProviderActive(string name)
		{
			return App.GetKey("ProviderActive", name, true);
		}

		public static readonly List<string> GlobalFonts = new List<string>() { "App default", "Gotham", "Trebuchet MS", "Open Sans", "Google Sans", "Lucida Grande", "Verdana", "Futura", "STIXGeneral", "Times New Roman" }; //ARIAL IS TO BIG

		public static string GlobalSubtitleFont {
			set {
				App.SetKey("Settings", nameof(GlobalSubtitleFont), value);
			}
			get {
				// return "Trebuchet MS";
				return App.GetKey("Settings", nameof(GlobalSubtitleFont), GlobalFonts[0]); // Trebuchet MS, Open Sans, Google Sans
			}
		}

		public static bool VideoplayerLockLandscape {
			set {
				App.SetKey("Settings", nameof(VideoplayerLockLandscape), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoplayerLockLandscape), false);
			}
		}

		public static bool HasAccountLogin {
			set {
				App.SetKey("Account", nameof(HasAccountLogin), value);
			}
			get {
				return App.GetKey("Account", nameof(HasAccountLogin), false);
			}
		}

		public static bool HasMalAccountLogin {
			get {
				return MalApiToken != "";
			}
		}

		// I know this is not considered safe, but you cant really protect it when the app is open source
		public static string MalApiToken {
			set {
				App.SetKey("MalAccount", nameof(MalApiToken), value);
			}
			get {
				return App.GetKey("MalAccount", nameof(MalApiToken), "");
			}
		}

		public static string MalApiRefreshToken {
			set {
				App.SetKey("MalAccount", nameof(MalApiRefreshToken), value);
			}
			get {
				return App.GetKey("MalAccount", nameof(MalApiRefreshToken), "");
			}
		}

		public static int MalApiTokenUnixTime {
			set {
				App.SetKey("MalAccount", nameof(MalApiTokenUnixTime), value);
			}
			get {
				return App.GetKey("MalAccount", nameof(MalApiTokenUnixTime), 0);
			}
		}

		public static MalUser? CurrentMalUser {
			set {
				App.SetKey("MalAccount", nameof(CurrentMalUser), value);
			}
			get {
				return App.GetKey<MalUser?>("MalAccount", nameof(CurrentMalUser), null);
			}
		}

		public static AniListSyncApi.AniListUser? CurrentAniListUser {
			set {
				App.SetKey("AniListAccount", nameof(CurrentAniListUser), value);
			}
			get {
				return App.GetKey<AniListSyncApi.AniListUser?>("AniListAccount", nameof(CurrentAniListUser), null);
			}
		}

		public static string AniListToken {
			set {
				App.SetKey("AniListAccount", nameof(AniListToken), value);
			}
			get {
				return App.GetKey("AniListAccount", nameof(AniListToken), "");
			}
		}

		public static int AniListTokenUnixTime {
			set {
				App.SetKey("AniListAccount", nameof(AniListTokenUnixTime), value);
			}
			get {
				return App.GetKey("AniListAccount", nameof(AniListTokenUnixTime), 0);
			}
		}

		public static bool HasAniListLogin {
			get {
				return AniListToken != "";
			}
		}


		public static string AccountUsername {
			set {
				App.SetKey("Account", nameof(AccountUsername), value);
			}
			get {
				return App.GetKey("Account", nameof(AccountUsername), "");
			}
		}

		public static string AccountSite {
			set {
				App.SetKey("Account", nameof(AccountSite), value);
			}
			get {
				return App.GetKey("Account", nameof(AccountSite), "");
			}
		}

		public static string AccountPassword {
			set {
				App.SetKey("Account", nameof(AccountPassword), value);
			}
			get {
				return App.GetKey("Account", nameof(AccountPassword), "");
			}
		}

		public static bool AccountOverrideServerData {
			set {
				App.SetKey("Account", nameof(AccountOverrideServerData), value);
			}
			get {
				return App.GetKey("Account", nameof(AccountOverrideServerData), false);
			}
		}

		public static string NativeSubShortName {
			get {
				return CloudStreamCore.subtitleShortNames[NativeSubtitles];
			}
		}

		public static string NativeSubLongName {
			get {
				return CloudStreamCore.subtitleNames[NativeSubtitles];
			}
		}

		public static int NativeSubtitles {
			set {
				App.SetKey("Settings", nameof(NativeSubtitles), value);
			}
			get {
				return App.GetKey("Settings", nameof(NativeSubtitles), CloudStreamCore.subtitleNames.IndexOf("English"));
			}
		}

		public static int ChromecastSkipTime {
			set {
				App.SetKey("Settings", nameof(ChromecastSkipTime), value);
			}
			get {
				return App.GetKey("Settings", nameof(ChromecastSkipTime), 30);
			}
		}

		public static int VideoPlayerSkipTime {
			set {
				App.SetKey("Settings", nameof(VideoPlayerSkipTime), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoPlayerSkipTime), 10);
			}
		}

		public static bool ListViewPopupAnimation {
			set {
				App.SetKey("Settings", nameof(ListViewPopupAnimation), value);
				FastListViewPopupAnimation = value;
			}
			get {
				return App.GetKey("Settings", nameof(ListViewPopupAnimation), false);
			}
		}

		public static bool FastListViewPopupAnimation { get; private set; }

		public static bool DefaultDub {
			set {
				App.SetKey("Settings", nameof(DefaultDub), value);
			}
			get {
				return App.GetKey("Settings", nameof(DefaultDub), true);
			}
		}

		public static bool SubtitlesEnabled {
			set {
				App.SetKey("Settings", nameof(SubtitlesEnabled), value);
			}
			get {
				return App.GetKey("Settings", nameof(SubtitlesEnabled), true);
			}
		}

		public static bool HasStatusBar {
			set {
				App.SetKey("Settings", nameof(HasStatusBar), value);
			}
			get {
				return App.GetKey("Settings", nameof(HasStatusBar), false);
			}
		}

		public static readonly string[] BlackBgNames = {
			"Black", "Dark", "Netflix", "YouTube"
		};

		/// <summary>
		/// 0 = Almond black, 1 = Dark, 2 = Netflix, 3 = YouTube
		/// </summary>
		public static int BlackBgType {
			set {
				App.SetKey("Settings", nameof(BlackBgType), value);
				UpdateItemBgColor();
			}
			get {
				return App.GetKey("Settings", nameof(BlackBgType), 1);
			}
		}

		/// <summary>
		/// Almond black = 0, Dark = 17, Netflix = 26, YouTube = 40
		/// </summary>
		public static int BlackColor {
			get {
				int[] colors = { 0, 17, 26, 40 };
				return colors[BlackBgType];
			}
		}

		public static Color BlackRBGColor {
			get {
				return Color.FromRgb(BlackColor, BlackColor, BlackColor);
			}
		}


		public static bool ViewHistory {
			set {
				CachedPauseHis = !value;
				App.SetKey("Settings", nameof(ViewHistory), value);
			}
			get {
				return App.GetKey("Settings", nameof(ViewHistory), true);
			}
		}

		public static bool PauseHistory {
			set {
				ViewHistory = !value;
			}
			get {
				return !ViewHistory;
			}
		}

		public static bool CachedPauseHis;

		public static bool EpDecEnabled {
			set {
				App.SetKey("Settings", nameof(EpDecEnabled), value);
			}
			get {
				return App.GetKey("Settings", nameof(EpDecEnabled), true);
			}
		}
		public static bool MovieDecEnabled {
			set {
				App.SetKey("Settings", nameof(MovieDecEnabled), value);
			}
			get {
				return true;
				//return App.GetKey("Settings", nameof(MovieDecEnabled), true);
			}
		}

		public static bool SearchEveryCharEnabled {
			set {
				App.SetKey("Settings", nameof(SearchEveryCharEnabled), value);
			}
			get {
				return App.GetKey("Settings", nameof(SearchEveryCharEnabled), true);
			}
		}

		public static bool CacheData {
			set {
				App.SetKey("Settings", nameof(CacheData), value);
			}
			get {
				return App.GetKey("Settings", nameof(CacheData), true);
			}
		}

		public static bool PictureInPicture {
			set {
				App.SetKey("Settings", nameof(PictureInPicture), value);
			}
			get {
				return App.GetKey("Settings", nameof(PictureInPicture), true);
			}
		}

		public static bool UseAniList {
			set {
				App.SetKey("Settings", nameof(UseAniList), value);
			}
			get {
				return App.GetKey("Settings", nameof(UseAniList), false);
			}
		}

		public static bool UseVideoPlayer {
			set {
				App.SetKey("Settings", nameof(UseVideoPlayer), value);
			}
			get {
				return App.GetKey("Settings", nameof(UseVideoPlayer), true);
			}
		}

		public static bool LazyLoadNextLink {
			set {
				App.SetKey("Settings", nameof(LazyLoadNextLink), value);
			}
			get {
				return App.GetKey("Settings", nameof(LazyLoadNextLink), true);
			}
		}

		public static bool TapjackProtectionSearch {
			set {
				App.SetKey("Settings", nameof(TapjackProtectionSearch), value);
			}
			get {
				return App.GetKey("Settings", nameof(TapjackProtectionSearch), false);
			}
		}
		public static bool TapjackProtectionButton {
			set {
				App.SetKey("Settings", nameof(TapjackProtectionButton), value);
			}
			get {
				return App.GetKey("Settings", nameof(TapjackProtectionButton), false);
			}
		}
		public static bool TapjackProtectionPicker {
			set {
				App.SetKey("Settings", nameof(TapjackProtectionPicker), value);
			}
			get {
				return App.GetKey("Settings", nameof(TapjackProtectionPicker), false);
			}
		}

		public static bool IgnoreSSLCert {
			set {
				_IgnoreSSLCert = value;
				App.SetKey("Settings", nameof(IgnoreSSLCert), value);
			}
			get {
				return App.GetKey("Settings", nameof(IgnoreSSLCert), true);
			}
		}

		public static bool _IgnoreSSLCert = true;

		public static bool Top100Enabled {
			set {
				App.SetKey("Settings", nameof(Top100Enabled), value);
			}
			get {
				return App.GetKey("Settings", nameof(Top100Enabled), true);
			}
		}

		public static bool Top100Anime {
			set {
				App.SetKey("Settings", nameof(Top100Anime), value);
			}
			get {
				return App.GetKey("Settings", nameof(Top100Anime), false);
			}
		}

		public static int VideoCropType {
			set {
				App.SetKey("Settings", nameof(VideoCropType), value);
			}
			get {
				return App.GetKey("Settings", nameof(VideoCropType), (int)VideoPage.AspectRatio.BestFit); // FUCK Original
			}
		}

#if DEBUG
		public static string PublishDatabaseServerIp {
			set {
				App.SetKey("Settings", nameof(PublishDatabaseServerIp), value);
			}
			get {
				return App.GetKey("Settings", nameof(PublishDatabaseServerIp), "");
			}
		}

		public static bool IsDatabasePublisher {
			set {
				App.SetKey("Settings", nameof(IsDatabasePublisher), value);
			}
			get {
				return App.GetKey("Settings", nameof(IsDatabasePublisher), false);
			}
		}
#endif

		public static Color ItemBackGroundColor = GetItemBgColor();

		static Color GetItemBgColor()
		{
			var _color = Settings.BlackColor + 5;
			return Color.FromRgb(_color, _color, _color);
		}

		static void UpdateItemBgColor()
		{
			ItemBackGroundColor = GetItemBgColor();
		}

		public string MainColor { get { return Device.RuntimePlatform == Device.UWP ? "#303F9F" : "#515467"; } }
		private static String HexConverter(Color c)
		{
			return c.ToHex(); //"#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
		}
		public static string MainBackgroundColor {
			get {
				if (Device.RuntimePlatform == Device.UWP) {
					return "#000000";
					// color = "#000811";
				}

				return HexConverter(Color.FromRgba(BlackColor, BlackColor, BlackColor, 255));
			}
		}

		public static bool HasScrollBar {
			get {
				return Device.RuntimePlatform == Device.UWP;
			}
		}

		public static ScrollBarVisibility ScrollBarVisibility {
			get { return HasScrollBar ? ScrollBarVisibility.Default : ScrollBarVisibility.Never; }
		}

		public static string[] AppIcons = new string[]
		{
			"Adaptive",
			"Hexagon"
		};

		public static int AppIcon
		{
			set {
				App.SetKey("Settings", nameof(AppIcons), value);
			}
			get {
				return App.GetKey("Settings", nameof(AppIcons), 0);
			}
		}

		public struct GlobalFont
		{
			public string Name;
			public float FontSpacing;
			public int FontSize;
			public string FontStyle { get { var s = App.GetFont(Name); return s == "" ? null : s; } }
			public string FontStylePath { get { var s = App.GetFont(Name, false); return s == "" ? null : s; } }
		}

		// "Trebuchet MS", "Open Sans", "Google Sans", "Lucida Grande", "Verdana", "Futura", "STIXGeneral", "Times New Roman", "App default" 
		// "Gotham","Trebuchet MS", "Open Sans", "Google Sans", "Lucida Grande", "Verdana", "Futura", "STIXGeneral", "Times New Roman", "App default"
		public static GlobalFont[] CurrentGlobalFonts = {
			new GlobalFont() {
				Name = "App default",
				FontSpacing = 1,
				FontSize = -1,
			},
			new GlobalFont() {
				Name = "Gotham",
				FontSpacing = 1.2f,
             //   FontSize = 14,
            },
			new GlobalFont() {
				Name = "Trebuchet MS",
				FontSpacing = 1f,
				FontSize = -1,
			},
			new GlobalFont() {
				Name = "Open Sans",
				FontSpacing = 1f,
              //  FontSize = 14,
            },
			new GlobalFont() {
				Name = "Google Sans",
				FontSpacing = 1f,
              //  FontSize = 14,
            },
			new GlobalFont() {
				Name = "Lucida Grande",
				FontSpacing = 1f,
              //  FontSize = 14,
            },
			new GlobalFont() {
				Name = "Futura",
				FontSpacing = 1f,
              //  FontSize = 14,
            },
			new GlobalFont() {
				Name = "STIXGeneral",
				FontSpacing = 1f,
             //   FontSize = 15,
            },
			new GlobalFont() {
				Name = "Times New Roman",
				FontSpacing = 1f,
               // FontSize = 14,
            },
		};

		public static int BackCurrentGlobalFont {
			set {
				App.SetKey("Settings", nameof(BackCurrentGlobalFont), value);
				//CurrentUpdatedFont = CurrentGlobalFonts[value < 0 ? CurrentGlobalFont : value];
			}
			get {
				return App.GetKey("Settings", nameof(BackCurrentGlobalFont), 1);
			}
		}

		public static int CurrentGlobalFont {
			/*set {
				App.SetKey("Settings", nameof(CurrentGlobalFont), value);
				CurrentFont = CurrentGlobalFonts[value];
			}*/
			get {
				return BackCurrentGlobalFont;// App.GetKey("Settings", nameof(CurrentGlobalFont), 1); // BY DEAFULT Gotham
			}
		}

		public static GlobalFont CurrentFont;
	//	public static GlobalFont CurrentUpdatedFont = CurrentGlobalFonts[CurrentGlobalFont];

		public static bool CacheImdb { get { return CacheData; } }
		public static bool CacheMAL { get { return CacheData; } }

		static bool initVideoPlayer = true;
		readonly VisualElement[] displayElements;

		public static void OnInit()
		{
			CachedPauseHis = PauseHistory;
			_IgnoreSSLCert = IgnoreSSLCert;
			UpdateItemBgColor();
		}

		public Settings()
		{
			InitializeComponent();
			OnInit();

			displayElements = new VisualElement[] {
				G_GeneralTxt,
				G_InstatSearch,
				G_Subtitles,
				G_CacheData,
				G_AniList,
				G_Dubbed,
				G_Lazyload,
				G_PauseHistory,

				SubtitleStyle,
				SubtitleLang,
				SubtitleFont,

				G_UITxt,
				G_Statusbar,
				G_Top100,
				G_VideoPlayer,
				G_Descript,
				G_Animation,

				G_ClearData,
				ClearHistoryTap,
				ClearBookmarksTap,
				ClearCachedTap,
				ResetallTap,
				G_TimeTxt,
				G_CastTime,
				G_SkipTime,
				G_BuildTxt,
				BuildNumber,
				G_GithubTxt,
				StarMe,
				FeedbackBtt,
				ManageAccount,
				VideoplayerOption,
				SetTheme,
               // UpdateBtt,
            };

			for (int i = 0; i < displayElements.Length; i++) {
				Grid.SetRow(displayElements[i], i);
			}

			InstantSearchImg.Source = App.GetImageSource("searchIcon.png");
			SubtitlesImg.Source = App.GetImageSource("outline_subtitles_white_48dp.png");
			CacheImg.Source = App.GetImageSource("outline_cached_white_48dp.png");
			AniListImg.Source = App.GetImageSource("outline_cached_white_48dp.png");
			DubbedImg.Source = App.GetImageSource("outline_record_voice_over_white_48dp.png");
			HistoryImg.Source = App.GetImageSource("outline_history_white_48dp.png");

			StatusbarImg.Source = App.GetImageSource("outline_aspect_ratio_white_48dp.png");
			TopImg.Source = App.GetImageSource("outline_reorder_white_48dp.png");
			DescriptImg.Source = App.GetImageSource("outline_description_white_48dp.png");

			VideoImg.Source = App.GetImageSource("baseline_ondemand_video_white_48dp.png");
			LazyLoadImg.Source = App.GetImageSource("baseline_ondemand_video_white_48dp.png");

			ListAniImg.Source = App.GetImageSource("animation.png");

			var ClearSource = App.GetImageSource("outline_delete_white_48dp.png");
			ClearImg1.Source = ClearSource;
			ClearImg2.Source = ClearSource;
			ClearImg3.Source = ClearSource;

			ResetImg.Source = App.GetImageSource("baseline_refresh_white_48dp.png");


			ColorPicker = new LabelList(SetTheme, new List<string> { "Black", "Dark", "Netflix", "YouTube" }, "Select Theme");
			SubLangPicker = new LabelList(SubtitleLang, CloudStreamCore.subtitleNames.ToList(), "Default Subtitle Language");
			SubStylePicker = new LabelList(SubtitleStyle, new List<string>() { "None", "Dropshadow", "Outline", "Outline + dropshadow" }, "Subtitle Style");
			SubFontPicker = new LabelList(SubtitleFont, GlobalFonts, "Subtitle Font", true);

			//outline_reorder_white_48dp.png

			BackgroundColor = Settings.BlackRBGColor;


			if (Device.RuntimePlatform == Device.UWP) {
				OffBar.IsVisible = false;
				OffBar.IsEnabled = false;
			}
			else {
				OffBar.Source = App.GetImageSource("gradient.png"); OffBar.HeightRequest = 3; OffBar.HorizontalOptions = LayoutOptions.Fill; OffBar.ScaleX = 100; OffBar.Opacity = 0.3; OffBar.TranslationY = 9;
			}

			//Main.print("COLOR: "+ BlackBgToggle.OnColor);
			//  if (Device.RuntimePlatform == Device.UWP) {
			BindingContext = this;
			// }
			StarMe.Clicked += async (o, e) => {
				await App.OpenBrowser("https://github.com/LagradOst/CloudStream-2");
			};
			BuildNumber.Text = "Build Version: " + App.GetBuildNumber();
			Apper();

			HistoryToggle.Toggled += (o, e) => {
				ViewHistory = !e.Value;
			};

			DubbedToggle.Toggled += (o, e) => {
				DefaultDub = e.Value;
			};

			VideoToggle.Toggled += (o, e) => {
				UseVideoPlayer = e.Value;
			};

			LazyLoadToggle.Toggled += (o, e) => {
				LazyLoadNextLink = e.Value;
			};

			//  BlackBgToggle.OnChanged += (o, e) => {
			//     BlackBg = e.Value;
			//  };
			SubtitlesToggle.Toggled += (o, e) => {
				SubtitlesEnabled = e.Value;
			};
			/*
            EpsDecToggle.OnChanged += (o, e) => {
                EpDecEnabled = e.Value;
            };*/
			DescriptToggle.Toggled += (o, e) => {
				EpDecEnabled = e.Value;
			};
			InstantSearchToggle.Toggled += (o, e) => {
				SearchEveryCharEnabled = e.Value;
			};
			CacheDataToggle.Toggled += (o, e) => {
				CacheData = e.Value;
			};
			TopToggle.Toggled += (o, e) => {
				Top100Enabled = e.Value;
			};
			StatusbarToggle.Toggled += (o, e) => {
				HasStatusBar = e.Value;
				App.UpdateStatusBar();
			};
			ListAniToggle.Toggled += (o, e) => {
				ListViewPopupAnimation = e.Value;
			};
			AniListToggle.Toggled += (o, e) => {
				UseAniList = e.Value;
			};

			Commands.SetTap(ClearHistoryTap, new Command((o) => {
				ClearHistory();
			}));

			Commands.SetTap(ClearCachedTap, new Command((o) => {
				ClearCache();
			}));
			Commands.SetTap(ClearBookmarksTap, new Command((o) => {
				ClearBookmarks();
			}));
			Commands.SetTap(ResetallTap, new Command((o) => {
				ResetToDef();
			}));

			SubStylePicker.SelectedIndexChanged += (o, e) => {
				if (SubStylePicker.SelectedIndex != -1) {
					SubtitleType = SubStylePicker.SelectedIndex;
					SubStylePicker.button.Text = "Subtitle Style: " + SubStylePicker.button.Text;
				}
			};

			SubFontPicker.SelectedIndexChanged += (o, e) => {
				if (SubFontPicker.SelectedIndex != -1) {
					GlobalSubtitleFont = SubFontPicker.ItemsSource[SubFontPicker.SelectedIndex];
					SubFontPicker.button.Text = "Subtitle Font: " + SubFontPicker.button.Text;
				}
			};


			SubLangPicker.SelectedIndexChanged += (o, e) => {
				if (SubLangPicker.SelectedIndex != -1) {
					NativeSubtitles = SubLangPicker.SelectedIndex;
					SubLangPicker.button.Text = "Default subtitle language: " + SubLangPicker.button.Text;
				}
			};

			ColorPicker.SelectedIndexChanged += (o, e) => {
				if (ColorPicker.SelectedIndex != -1) {
					BlackBgType = ColorPicker.SelectedIndex;
					ColorPicker.button.Text = "Current Theme: " + ColorPicker.button.Text;
					App.UpdateBackground();
				}
			};

			UpdateBtt.Clicked += (o, e) => {
				if (NewGithubUpdate) {
					App.DownloadNewGithubUpdate(githubUpdateTag, App.AndroidVersionArchitecture.Universal);
				}
			};


			ManageAccount.Clicked += async (o, e) => {
				await ManageAccountClicked(() => Apper());
			};
			OnInitAfter();

			/*
            if (Device.RuntimePlatform != Device.Android) {
                for (int i = 0; i < SettingsTable.Count; i++) {
                    if (i >= SettingsTable.Count) break;
                    if (SettingsTable[i][0] is TextCell) {
                        if (((TextCell)(SettingsTable[i][0])).DetailColor == Color.Transparent) {
                            SettingsTable.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }*/
		}

		public static async void OnInitAfter()
		{
			if (HasAniListLogin) {
				await AniListSyncApi.CheckToken();

				if (!CurrentAniListUser.HasValue) {
					_ = AniListSyncApi.GetUser();
				}
			}

			if (HasMalAccountLogin) {
				if (!CurrentMalUser.HasValue) {
					_ = GetUser();
				}
				_ = OnLoginOrStart();
			}
			if (AccountOverrideServerData) { // If get account dident work and you have changed then override server data
				PublishSyncAccount(5000);
			}
			else { // Get account data, will override current data 
				GetSyncAccount(5000);
			}

			AccountOverrideServerData = true;

			App.OnAppNotInForground += (o, e) => {
				AccountOverrideServerData = false;
				PublishSyncAccount();
			};
		}

		public static async Task ManageAccountClicked(Action appear)
		{
			List<string> actions = new List<string>() { };

			if (HasAccountLogin) {
				actions.Add("Logout from CloudSync - " + AccountUsername);
			}
			else {
				actions.Add("Create CloudSync account");
				actions.Add("Login to CloudSync");
			}
			if (!HasMalAccountLogin) {
				actions.Add("Login to MAL account");
			}
			else {
				string currentMalUsername = "";
				var user = CurrentMalUser;
				if (user.HasValue) {
					currentMalUsername = user.Value.name;
				}
				actions.Add($"Logout from MAL{(currentMalUsername == "" ? "" : $" - {currentMalUsername}")}");
			}

			if (!HasAniListLogin) {
				actions.Add("Login to AniList account");
			}
			else {
				string currentAniListUsername = "";
				var user = CurrentAniListUser;
				if (user.HasValue) {
					currentAniListUsername = user.Value.name;
				}
				actions.Add($"Logout from AniList {(currentAniListUsername == "" ? "" : $" - {currentAniListUsername}")}");
			}

			actions.AddRange(new string[] {
#if DEBUG
				"TEST",
#endif               
				"Export data", "Export Everything", "Import data", });

			string action = await ActionPopup.DisplayActionSheet("Manage account", actions.ToArray());
#if DEBUG
			if (action == "TEST") {
				//App.AddShortcut("Iron Man", "tt0371746", "https://m.media-amazon.com/images/M/MV5BMTczNTI2ODUwOF5BMl5BanBnXkFtZTcwMTU0NTIzMw@@._V1_UX182_CR0,0,182,268_AL_.jpg");
			}
#endif

			if (action == "Login to AniList account") {
				CloudStreamForms.Script.AniListSyncApi.Authenticate();
			}
			else if (action == "Login to MAL account") {
				CloudStreamForms.Script.MALSyncApi.Authenticate();
			}
			else if (action.StartsWith("Logout from AniList")) {
				App.RemoveFolder("AniListAccount");
				App.ShowToast("Logout complete");
			}
			else if (action.StartsWith("Logout from MAL")) {
				App.RemoveFolder("MalAccount");
				App.ShowToast("Logout complete");
			}
			else if (action == "Export data" || action == "Export Everything") {
				string subaction = await ActionPopup.DisplayEntry(InputPopupPage.InputPopupResult.password, "Password", "Encrypt data", autoPaste: false, confirmText: "Encrypt");

				if (subaction != "Cancel") {
					await Task.Delay(200);
					await ActionPopup.StartIndeterminateLoadinbar("Encrypting data");

					string text = "";
					string fileloc = "";
					bool done = false;
					Thread t = new Thread(() => {
						try {
							text = Script.SyncWrapper.GenerateTextFile(action == "Export Everything");
							if (subaction != "") {
								text = CloudStreamForms.Cryptography.StringCipher.Encrypt(text, subaction);
							}
							fileloc = App.DownloadFile(text, App.DATA_FILENAME, true);
						}
						finally {
							done = true;
						}
					});
					t.Start();

					while (!done) {
						await Task.Delay(100);
					}

					if (fileloc != "") {
						App.ShowToast($"Saved data to {fileloc}");
					}

					await ActionPopup.StopIndeterminateLoadinbar();
				}
			}
			else if (action == "Import data") {
				string file = App.ReadFile(App.DATA_FILENAME, true, "");
				if (file == "") {
					App.ShowToast("No file found");
				}
				else {
					if (file.StartsWith(Script.SyncWrapper.header)) {
						string import = await ActionPopup.DisplayActionSheet("Override Current Data", "Yes, import data and override current", "No, dont override current data");
						if (import.StartsWith("Y")) {
							Script.SyncWrapper.SetKeysFromTextFile(file);
							Home.UpdateIsRequired = true;
							App.ShowToast("File loaded");
							appear?.Invoke();
						}
					}
					else {
						bool success = false;
						while (!success) {
							string password = await ActionPopup.DisplayEntry(InputPopupPage.InputPopupResult.password, "Password", "Decrypt data", autoPaste: false, confirmText: "Decrypt");
							CloudStreamCore.print("PASSWORDD:D::: " + password);
							if (password != "Cancel" && password != "") {
								string subFile = CloudStreamForms.Cryptography.StringCipher.Decrypt(file, password);
								CloudStreamCore.print("SUBFILE::: " + subFile);
								success = subFile.StartsWith(Script.SyncWrapper.header);
								if (success) {
									await System.Threading.Tasks.Task.Delay(200);
									string import = await ActionPopup.DisplayActionSheet("Override Current Data", "Yes, import data and override current", "No, dont override current data");
									if (import.StartsWith("Y")) {
										Script.SyncWrapper.SetKeysFromTextFile(subFile);
										Home.UpdateIsRequired = true;
										App.ShowToast("File dectypted and loaded");
										appear?.Invoke();
									}
								}
								else {
									App.ShowToast("Failed to dectypted file");
									await System.Threading.Tasks.Task.Delay(200);
								}
							}
							else {
								success = true;
							}
						}

					}
				}
			}
			else if (action.StartsWith("Logout from CloudSync")) {
				await ActionPopup.StartIndeterminateLoadinbar("Backing up data...");
				GetAccountResponse(AccountSite, AccountUsername, AccountPassword, Logintype.EditAccount, out LoginErrorType error, Script.SyncWrapper.GenerateTextFile(false));
				await ActionPopup.StopIndeterminateLoadinbar();
				App.RemoveFolder("Account");
				App.ShowToast("Logout complete");
			}
			else if (action == "Login to CloudSync") {
				bool tryLogin = true;
				string password = "";
				string username = "";
				string site = "";
				while (tryLogin) {
					List<string> data = await ActionPopup.DisplayLogin("Login", "Cancel", "Login to account", new LoginPopupPage.PopupFeildsDatas() { placeholder = "Server Url", setText = site }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Username", setText = username }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Password", isPassword = true, setText = password });
					if (data.Count == 3) {
						site = data[0];
						username = data[1];
						password = data[2];
						try {
							await ActionPopup.StartIndeterminateLoadinbar("Trying to login...");
							string logindata = GetAccountResponse(site, username, password, Logintype.LoginAccount, out LoginErrorType error);
							await ActionPopup.StopIndeterminateLoadinbar();

							if (error == LoginErrorType.InternetError) {
								App.ShowToast("Could not connect to the server");
							}
							else if (error == LoginErrorType.UsernameTaken) {
								App.ShowToast("Username taken");
							}
							else if (error == LoginErrorType.WrongPassword) {
								App.ShowToast("Wrong password");
							}
							else if (error == LoginErrorType.Ok) {
								App.ShowToast("Login complete");
								tryLogin = false;
								AccountSite = site;
								AccountPassword = password;
								AccountUsername = username;
								HasAccountLogin = true;
								GetSyncAccount(0, logindata);
							}
							else if (error == LoginErrorType.ClientError) {
								App.ShowToast("Too short username or password");
							}
						}
						catch {
							App.ShowToast("Internal server error");
						}
					}
					else {
						tryLogin = false;
					}
				}
			}
			else if (action == "Create CloudSync account") {
				bool tryCreate = true;
				string password = "";
				string username = "";
				string site = "";
				while (tryCreate) {
					List<string> data = await ActionPopup.DisplayLogin("Create", "Cancel", "Create account", new LoginPopupPage.PopupFeildsDatas() { placeholder = "Server Url", setText = site }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Username", setText = username }, new LoginPopupPage.PopupFeildsDatas() { placeholder = "Password", isPassword = true, setText = password });
					if (data.Count == 3) {
						site = data[0];
						username = data[1];
						password = data[2];
						try {
							await ActionPopup.StartIndeterminateLoadinbar("Creating Account...");
							string logindata = GetAccountResponse(site, username, password, Logintype.CreateAccount, out LoginErrorType error, Script.SyncWrapper.GenerateTextFile(false));
							await ActionPopup.StopIndeterminateLoadinbar();

							if (error == LoginErrorType.InternetError) {
								App.ShowToast("Could not connect to the server");
							}
							else if (error == LoginErrorType.UsernameTaken) {
								App.ShowToast("Username taken");
							}
							else if (error == LoginErrorType.Ok) {
								App.ShowToast("Account created");
								tryCreate = false;
								AccountSite = site;
								AccountPassword = password;
								AccountUsername = username;
								HasAccountLogin = true;
							}
							else if (error == LoginErrorType.WrongPassword) {
								App.ShowToast("Internal server error");
							}
							else if (error == LoginErrorType.ClientError) {
								App.ShowToast("Too short username or password");
							}
						}
						catch {
							App.ShowToast("Internal server error");
						}
					}
					else {
						tryCreate = false;
					}
				}
			}
		}

		const double MAX_LOADING_TIME = 30000;
		const double MIN_LOADING_TIME = 1000;
		const double MIN_LOADING_CHROME = 5;
		const double MAX_LOADING_CHROME = 60;

		const int ROUND_LOADING_DECIMALES = 2;
		const int ROUND_LOADING_CHROME_DECIMALES = 0;
		void Apper()
		{
			Device.BeginInvokeOnMainThread(() => {
				SetSliderTime();
				SetSliderChromeTime();
				LoadingSlider.Value = ((LoadingMiliSec - MIN_LOADING_TIME) / (MAX_LOADING_TIME - MIN_LOADING_TIME));
				CastSlider.Value = ((ChromecastSkipTime - MIN_LOADING_CHROME) / (MAX_LOADING_CHROME - MIN_LOADING_CHROME));
				SubtitlesToggle.IsToggled = SubtitlesEnabled;
				DubbedToggle.IsToggled = DefaultDub;
				VideoToggle.IsToggled = UseVideoPlayer;
				HistoryToggle.IsToggled = !ViewHistory;
				DescriptToggle.IsToggled = EpDecEnabled;
				InstantSearchToggle.IsToggled = SearchEveryCharEnabled;
				CacheDataToggle.IsToggled = CacheData;
				StatusbarToggle.IsToggled = HasStatusBar;
				TopToggle.IsToggled = Top100Enabled;
				AniListToggle.IsToggled = UseAniList;
				ColorPicker.SelectedIndex = BlackBgType;
				SubLangPicker.SelectedIndex = NativeSubtitles;
				SubStylePicker.SelectedIndex = SubtitleType;
				SubFontPicker.SelectedIndex = GlobalFonts.IndexOf(GlobalSubtitleFont);
				LazyLoadToggle.IsToggled = LazyLoadNextLink;

				ListAniToggle.IsToggled = ListViewPopupAnimation;
				FastListViewPopupAnimation = ListViewPopupAnimation;

			});
		}

		protected override void OnDisappearing()
		{
			//  OnIconEnd(3); 
			base.OnDisappearing();
		}

		void InitVideoPlayer()
		{
			// VIDEOPLAYER Option
			List<string> videoOptions = new List<string>() { App.GetVideoPlayerName(App.VideoPlayer.None) };
			List<App.VideoPlayer> avalibePlayers = new List<App.VideoPlayer>();
			foreach (var player in App.GetEnumList<App.VideoPlayer>()) {
				if (App.GetVideoPlayerInstalled(player)) {
					avalibePlayers.Add(player);
					videoOptions.Add(App.GetVideoPlayerName(player));
				}
			}
			initVideoPlayer = false;
			VideoplayerOptionPicker = new LabelList(VideoplayerOption, videoOptions, "External video player");
			int index = videoOptions.IndexOf(App.GetVideoPlayerName((App.VideoPlayer)Settings.PreferedVideoPlayer));
			if (index == -1) {
				if (avalibePlayers.Count > 0) {
					index = 1;
					// PreferedVideoPlayer = (int)avalibePlayers[0];
				}
			}

			VideoplayerOptionPicker.SelectedIndexChanged += (o, e) => {
				if (e != -1) {
					Settings.PreferedVideoPlayer = e - 1; // 0 = no videoplayer
					VideoplayerOptionPicker.button.Text = "Current Videoplayer: " + VideoplayerOptionPicker.button.Text;
				}
			};

			VideoplayerOptionPicker.SelectedIndex = index == -1 ? 0 : index;
		}

		protected override void OnAppearing()
		{
			//  OnIconStart(3);
			base.OnAppearing();
			Apper();

			if (initVideoPlayer) {
				InitVideoPlayer();
			}
			if (Device.RuntimePlatform == Device.Android) {
				if (NewGithubUpdate) {
					Grid.SetRow(UpdateBtt, displayElements.Length);
				}
				UpdateBtt.IsEnabled = NewGithubUpdate;
				UpdateBtt.IsVisible = NewGithubUpdate;
				UpdateBtt.Text = "Update " + App.GetBuildNumber() + " to " + githubUpdateTag.Replace("v", "") + " · " + githubUpdateText;
				BackgroundColor = Settings.BlackRBGColor;
			}
		}


		private void Slider_DragCompleted(object sender, EventArgs e)
		{
			LoadingMiliSec = (int)(Math.Round((Math.Round(((Slider)sender).Value * (MAX_LOADING_TIME - MIN_LOADING_TIME)) + MIN_LOADING_TIME) / Math.Pow(10, ROUND_LOADING_DECIMALES)) * Math.Pow(10, ROUND_LOADING_DECIMALES));
			SetSliderTime();
		}

		private void Slider_DragCompleted2(object sender, EventArgs e)
		{
			ChromecastSkipTime = (int)(Math.Round((Math.Round(((Slider)sender).Value * (MAX_LOADING_CHROME - MIN_LOADING_CHROME)) + MIN_LOADING_CHROME) / Math.Pow(10, ROUND_LOADING_CHROME_DECIMALES)) * Math.Pow(10, ROUND_LOADING_CHROME_DECIMALES));
			SetSliderChromeTime();
		}

		void SetSliderTime()
		{
			string s = Math.Round(LoadingMiliSec / 1000.0, 1).ToString();
			if (!s.Contains(".")) {
				s += ".0";
			}
			LoadingTime.Text = "Loading Time: " + s + "s";
		}

		void SetSliderChromeTime()
		{
			CastTime.Text = "Skip time: " + ChromecastSkipTime + "s";
		}

		private void TextCell_Tapped(object sender, EventArgs e)
		{
			ClearHistory();
		}
		private void TextCell_Tapped2(object sender, EventArgs e)
		{
			ClearBookmarks();
		}
		private void TextCell_Tapped3(object sender, EventArgs e)
		{
			ClearCache();
		}
		private void TextCell_Tapped4(object sender, EventArgs e)
		{
			ResetToDef();
		}

		async void ClearBookmarks()
		{
			bool action = await DisplayAlert("Clear bookmarks", "Are you sure that you want to remove all bookmarks" + " (" + App.GetKeyCount("BookmarkData") + " items)", "Yes", "Cancel");
			if (action) {
				App.RemoveFolder(App.BOOKMARK_DATA);
				Home.UpdateIsRequired = true;
			}
		}

		async void ClearHistory()
		{
			bool action = await DisplayAlert("Clear watch history", "Are you sure that you want to clear all watch history" + " (" + App.GetKeyCount("ViewHistory") + " items)", "Yes", "Cancel");
			if (action) {
				App.RemoveFolder(App.VIEW_HISTORY);
			}
		}

		async void ClearCache()
		{
			bool action = await DisplayAlert("Clear cached data", "Are you sure that you want to clear all cached data" + " (" + (App.GetKeyCount("CacheMAL") + App.GetKeyCount("CacheImdb")) + " items)", "Yes", "Cancel");
			if (action) {
				App.RemoveFolder("CacheMAL");
				App.RemoveFolder("CacheImdb");
			}
		}

		async void ResetToDef()
		{
			bool action = await DisplayAlert("Reset settings to default", "Are you sure that you want to reset settings to default", "Yes", "Cancel");//await ActionPopup.DisplayActionSheet("Reset settings to default", "Clear settings") == "Clear settings"; //
			if (action) {
				App.RemoveFolder("Settings");
				Apper();
			}
		}

		private void FeedbackBtt_Clicked(object sender, EventArgs e)
		{
			Navigation.PushModalAsync(new Feedback());
		}


		public enum Logintype
		{
			CreateAccount = 0,
			LoginAccount = 1,
			EditAccount = 2,
		}

		public enum LoginErrorType
		{
			Ok = 0,
			InternetError = 1,
			WrongPassword = 2,
			UsernameTaken = 3,
			ClientError = 10,
		}

		// DO NOT CHANGE, WILL BREAK ALL CURRENT ACCOUNTS!
		const string BEFORE_SALT_PASSWORD = "BeforeSalt";
		const string AFTER_SALT_PASSWORD = "AfterSalt";

		readonly static Random rng = new Random();

		public static void GetSyncAccount(int delay = 0, string _data = null)
		{
			if (AccountPassword != "" && AccountUsername != "" && AccountPassword != "") {
				Thread t = new Thread(() => {
					Thread.Sleep(delay);
					try {
						LoginErrorType error = LoginErrorType.Ok;
						string data = _data ?? GetAccountResponse(AccountSite, AccountUsername, AccountPassword, Logintype.LoginAccount, out error);
						if (error != LoginErrorType.Ok) {
							App.ShowToast("Account Server error");
							AccountOverrideServerData = true;
						}
						else if (data != "") {
							if (data.StartsWith(Script.SyncWrapper.header)) {
								Script.SyncWrapper.SetKeysFromTextFile(data);
								Home.UpdateIsRequired = true;
							}
							else {
								App.ShowToast("Account data corrupted");
								PublishSyncAccount();
							}
						}
					}
					catch {
						App.ShowToast("Account error");
						AccountOverrideServerData = true;
					}
				});
				t.Start();
			}
		}

		public static void PublishSyncAccount(int delay = 0)
		{
			if (AccountPassword != "" && AccountUsername != "" && AccountPassword != "") {
				Thread t = new Thread(() => {
					Thread.Sleep(delay);

					try {
						string data = GetAccountResponse(AccountSite, AccountUsername, AccountPassword, Logintype.EditAccount, out LoginErrorType error, Script.SyncWrapper.GenerateTextFile(false));
						if (error != LoginErrorType.Ok) {
							App.ShowToast("Account Server error");
						}
						else if (data != "") {
							Script.SyncWrapper.SetKeysFromTextFile(data);
							Home.UpdateIsRequired = true;
						}
					}
					catch {
						App.ShowToast("Account error");
					}
				});
				t.Start();
			}
		}

		public static string GetAccountResponse(string url, string name, string password, Logintype logintype, out LoginErrorType result, string data = "")
		{
			name = name.Replace(" ", "");

			if (name.Length < 4 || password.Length < 4) {
				result = LoginErrorType.ClientError;
				return "";
			}

			password = BEFORE_SALT_PASSWORD + password + AFTER_SALT_PASSWORD + name; // This is so the server cant use hash tables to look up password
			result = LoginErrorType.InternetError;


			try {
				int waitTime = 400;
				HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.Create(url);
				if (CloudStreamCore.GetRequireCert(url)) { webRequest.ServerCertificateValidationCallback = delegate { return true; }; }
				webRequest.Method = "POST";
				webRequest.UserAgent = "CLOUDSTREAM APP v" + App.GetBuildNumber();
				webRequest.Timeout = waitTime * 10;
				webRequest.ReadWriteTimeout = waitTime * 10;
				webRequest.ContinueTimeout = waitTime * 10;
				webRequest.Headers.Add("LOGINTYPE", ((int)logintype).ToString());
				int ff = webRequest.MaximumResponseHeadersLength;
				webRequest.Headers.Add("NAME", StringCipher.HashData(name));
				if (logintype == Logintype.CreateAccount) {
					webRequest.Headers.Add("HASHPASSWORD", StringCipher.HashData(password));
				}
				else {
					webRequest.Headers.Add("ONETIMEPASSWORD", StringCipher.Encrypt(rng.Next(0, int.MaxValue) + "CORRECTPASS[" + DateTime.UtcNow.ToBinary() + "]", StringCipher.HashData(password)));
				}
				if (logintype != Logintype.LoginAccount) {
					//    webRequest.Headers.Add("DATA", StringCipher.Encrypt(data, password));
				}

				try {
					HttpWebRequest _webRequest = webRequest;
					Stream postStream = _webRequest.GetRequestStream();

					string requestBody = logintype != Logintype.LoginAccount ? StringCipher.Encrypt(data, password) : "";// --- RequestHeaders ---

					byte[] byteArray = Encoding.UTF8.GetBytes(requestBody);

					postStream.Write(byteArray, 0, byteArray.Length);
					postStream.Close();


					// BEGIN RESPONSE

					try {
						HttpWebRequest request = webRequest;
						HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();

						using StreamReader httpWebStreamReader = new StreamReader(response.GetResponseStream());
						try {
							string s = httpWebStreamReader.ReadToEnd();
							if (s.StartsWith("OKDATA")) {
								result = LoginErrorType.Ok;
								if (logintype == Logintype.LoginAccount) {
									string _s = s.Split('\n')[1];
									return StringCipher.Decrypt(_s, password); // THE DATA IS OKDATA\nUSERDATA 
								}
								else {
									return "";
								}
							}
							else {
								result = (LoginErrorType)int.Parse(CloudStreamCore.FindHTML(s, "ERRORCODE[", "]"));
								return "";
							}
						}
						catch (Exception) {
							return "";
						}

					}
					catch (Exception _ex) {
						CloudStreamCore.error("FATAL EX IN POST2: " + _ex);
					}

				}
				catch (Exception _ex) {
					CloudStreamCore.error("FATAL EX IN POSTREQUEST" + _ex);
				}
				return "";
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}

		}

	}
}
