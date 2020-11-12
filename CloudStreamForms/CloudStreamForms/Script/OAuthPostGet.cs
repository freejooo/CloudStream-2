using CloudStreamForms.Core;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CloudStreamForms.Script
{
	class OAuthPostGet
	{
		public static async Task<string> GetRequest(string url, string auth = null)
		{
			try {
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

				request.Method = "GET";
				if (auth.IsClean()) {
					request.Headers["Authorization"] = auth;
				}
				request.ContentType = "text/html; charset=UTF-8";
				request.UserAgent = CloudStreamCore.USERAGENT;
				request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
				request.Headers.Add("Accept-Encoding", "gzip, deflate");

				request.Headers.Add("TE", "Trailers");

				using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

				using Stream stream = response.GetResponseStream();
				using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
				string result = await reader.ReadToEndAsync();
				return result;
			}
			catch (WebException e) {
				using (WebResponse response = e.Response) {
					HttpWebResponse httpResponse = (HttpWebResponse)response;
					Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
					using (Stream data = response.GetResponseStream())
					using (var reader = new StreamReader(data)) {
						string text = reader.ReadToEnd();
						Console.WriteLine(text);
					}
				}
				return "";
			}
			catch (Exception _ex) {
				CloudStreamCore.error(_ex);
				return "";
			}
		}

		public static async Task<string> PostRequest(string url, string postdata, string rtype = "POST", string auth = null, string content = "application/x-www-form-urlencoded")
		{
			try {
				HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

				request.Method = rtype;
				if (auth.IsClean()) {
					request.Headers["Authorization"] = auth;
				}
				byte[] byteArray = Encoding.UTF8.GetBytes(postdata);
				request.ContentType = content;
				request.ContentLength = byteArray.Length;
				Stream dataStream = await request.GetRequestStreamAsync();
				dataStream.Write(byteArray, 0, byteArray.Length);
				dataStream.Close();
				WebResponse response = await request.GetResponseAsync();

				dataStream = response.GetResponseStream();
				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = await reader.ReadToEndAsync();
				reader.Close();
				dataStream.Close();
				response.Close();
				return responseFromServer;
			}
			catch (WebException e) {
				using (WebResponse response = e.Response) {
					HttpWebResponse httpResponse = (HttpWebResponse)response;
					Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
					using (Stream data = response.GetResponseStream())
					using (var reader = new StreamReader(data)) {
						string text = reader.ReadToEnd();
						Console.WriteLine(text);
					}
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
