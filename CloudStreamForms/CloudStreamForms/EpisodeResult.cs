using System;
using System.Collections.Generic;
using Xamarin.Forms;

namespace CloudStreamForms.Models
{
    public class EpisodeResult : ICloneable
    {
        public int Id { set; get; }
        public int Episode { set; get; } = -1;
        public int Season { set; get; } = -1;
        public string Title { set; get; }
        public string Rating { set; get; }
        public string RatingStar { get { return (Rating.Replace(" ", "") == "" ? "Rating Unavailable" : "★ " + Rating); } } // ★
        public string PosterUrl { set; get; }
        public bool IsPosterFromStorage { get { return PosterUrl == CloudStreamCore.VIDEO_IMDB_IMAGE_NOT_FOUND; } }
        public ImageSource ImageSource { get {  return IsPosterFromStorage ? App.GetImageSource(PosterUrl) : PosterUrl; } }
        public ImageSource VideoSource { get { return App.GetImageSource("nexflixPlayBtt.png");  } }
        public ImageSource DownloadSource { get { return "NetflixDownload1.png"; } }
        public ImageSource DownloadPlayBttSource { get; set; }
        public Command TapCom { set; get; }
        public bool IsDownloading { get { return downloadState == 2; } }
        public bool IsDownloaded { get { return downloadState == 1; } } 
        public bool IsSearchingDownloadResults { get { return downloadState == 3; } } 
        public bool IsNotSearchingDownloadResults { get { return !IsSearchingDownloadResults; } } 

        /// <summary>
        /// 0 = Nothing, 1 = Downloaded, 1 = Downloading, 2 = Paused, 3 = Searching 
        /// </summary>
        public int downloadState { set; get; } = 0;
        public string extraInfo { set; get; }
        public string ExtraText { set; get; }
        public double ExtraProgress { set; get; }
        public bool DownloadNotDone { set; get; }

        string _Description = "";
        string _ExtraDescription = "";
        public string Description { set { _Description = Settings.EpDecEnabled ? value : ""; } get { return _Description; } }
        public string ExtraDescription { set { _ExtraDescription = Settings.EpDecEnabled ? value : ""; } get { return _ExtraDescription; } }
        public double Progress { set; get; }
        public List<string> Mirros { set; get; }
        public List<string> mirrosUrls { set; get; }
        public List<string> subtitles { set; get; }
        public List<string> subtitlesUrls { set; get; }
        public bool epVis { set; get; }
        // public LoadResult loadResult { set; get; }
        public bool LoadedLinks { set; get; }
        public string MainTextColor { set; get; } = "#e7e7e7";
        public string MainDarkTextColor { get; set; } = "#a4a4a4"; //"#808080";
        public string ExtraColor { get; set; } = "#a4a4a4"; //"#808080";
        public string OgTitle { set; get; }
        public double TranslateYOffset
        {
            get {
                if (Device.RuntimePlatform == Device.UWP) {
                    return -20;
                }
                else {
                    return 0;
                }
            }
        }
        public double TranslateYOffsetVertical
        {
            get {
                if (Device.RuntimePlatform == Device.UWP) {
                    return 0;
                }
                else {
                    return 0;
                }
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    /*
    public enum LoadSelection { Play,Download,CopyLink,CopySubtitleLink }

    public struct LoadResult
    {
        public string url;
        public string subtitleUrl;
        public LoadSelection loadSelection;
    }*/
}
