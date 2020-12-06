using Xamarin.Forms;

namespace CloudStreamForms.Models
{
	public class SearchResult
	{
		public int Id { set; get; }
		public string Title { set; get; }
		public string Extra { set; get; }
		public string Poster { set; get; }
		public string ExtraColor { set; get; }
		public bool IsBookmarked { set; get; }
		public Command OnClick { set; get; }
	}
}
