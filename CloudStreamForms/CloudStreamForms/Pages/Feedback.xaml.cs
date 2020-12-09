using System;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CloudStreamForms
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class Feedback : ContentPage
	{
		public static readonly string[] headers = { "Bug", "Feature+Request", "UI+Request", "Website+Request", "Other" };
		bool pending = false;
		bool PostDataRequest(string title, string feedback, int headerId)
		{
			string entry = $"entry.1053489500={headers[headerId]}&entry.307565363={title}&entry.1502962940={feedback}&fvv=1&draftResponse=%5Bnull%2Cnull%2C%221883422455443503652%22%5D%0D%0A&pageHistory=0&fbzx=1883422455443503652";
			const string resp = "https://docs.google.com/forms/d/e/1FAIpQLSeWxFCeR7jm2iP-I8BOxa5saATb4jOPBbl3OU-oBUwBXE4G7Q/formResponse";
			string d = CloudStreamForms.Core.CloudStreamCore.mainCore.PostRequest(resp, resp, entry);
			return d != "";
		}

		readonly LabelList requestType;

		bool Submit()
		{
			pending = true;
			Device.BeginInvokeOnMainThread(() => {
				FormsLoading.IsVisible = true;
			});
			bool succ = false;
			try {
				succ = PostDataRequest(TitleEntry.Text, EditorEntry.Text, requestType.SelectedIndex);
			}
			catch (Exception) {
			}
			pending = false;
			Device.BeginInvokeOnMainThread(() => {
				FormsLoading.IsVisible = false;
			});
			string msg = succ ? "Posted feedback" : "Failed to post feedback";
			App.ShowToast(msg);
			return succ;
		}

		const int minTitleChars = 4;
		const int minTextChars = 4;
		string IsCorrect()
		{
			if (TitleEntry.Text.Length < minTitleChars) {
				return $"Title must be at least {minTitleChars} characters";
			}
			else if (EditorEntry.Text.Length < minTextChars) {
				return $"Feedback must be at least {minTextChars} characters";
			}
			return "";
		}

		public Feedback()
		{
			InitializeComponent();
			requestType = new LabelList(RequestType, headers.Select(t => t.Replace("+", " ")).ToList(), "Feedback Type") {
				SelectedIndex = 0
			};
			BackgroundColor = Settings.BlackRBGColor;

			SubmitBtt.Clicked += async (o, e) => {
				await Task.Run(() => {
					string cor = IsCorrect();
					if (cor == "") {
						if (!pending) {
							if (Submit()) {
								Navigation.PopModalAsync();
							}
						}
						else {
							App.ShowToast("Sending...");
						}
					}
					else {
						Device.BeginInvokeOnMainThread(() => {
							ErrorTxt.Text = cor;
						});
					}
				});
			};
		}
	}
}