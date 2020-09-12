using Android.App;
using Android.Content;
using Android.OS;
using Android.Webkit;
using CloudStreamForms;
using CloudStreamForms.Core;
using CloudStreamForms.Droid;
using Java.IO;
using Java.Net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static CloudStreamForms.App;
using static CloudStreamForms.Core.CloudStreamCore;
using static CloudStreamForms.Droid.LocalNot;
using static CloudStreamForms.Droid.MainActivity;
using Application = Android.App.Application;
namespace CloudStreamForms.Droid
{
	public static class DownloadHandle
	{
		readonly static Dictionary<int, long> progressDownloads = new Dictionary<int, long>();
		const string DOWNLOAD_KEY = "DownloadProgress";
		const string DOWNLOAD_KEY_INTENT = "DownloadProgressIntent";

		public static void OnKilled()
		{
			try {
				foreach (var id in DownloadHandle.activeIds) {
					var manager = Application.Context.GetSystemService(Context.NotificationService) as NotificationManager;
					manager.Cancel(id);
					//  App.CancelNotifaction(id);
				}
				foreach (var key in outputStreams.Keys) {
					var outp = outputStreams[key];
					var inpp = inputStreams[key];
					outp.Flush();
					outp.Close();
					inpp.Close();
				}
				foreach (var key in progressDownloads.Keys) {
					print("SAVED KEY:" + key);
					App.SetKey(DOWNLOAD_KEY, key.ToString(), progressDownloads[key]);
				}
			}
			catch (Exception _ex) {
				print("EXEPTION WHEN DESTROYED: " + _ex);
			}
		}

		public static void ResumeIntents()
		{
			activity.LogFile("Resuming intents");

			try {
				int downloadResumes = 0;
				foreach (var path in App.GetKeysPath(DOWNLOAD_KEY_INTENT)) {
					//  App.GetKey<long>(DOWNLOAD_KEY, path, 0);
					downloadResumes++;
					try {
						string data = App.GetKey<string>(path, null);
						HandleIntent(data);
					}
					catch (Exception) { }
				}

				if (downloadResumes == 1) {
					App.ShowToast("Resumed Download");
				}
				else if (downloadResumes != 0) {
					App.ShowToast($"Resumed {downloadResumes} downloads");
				}
			}
			catch (Exception _ex) {
				activity.LogFile("Resume Intents Failed: " + _ex);
				App.ShowToast("Error resuming download");
			}
			finally {
				activity.LogFile("Resuming intents done");
			}
		}

		readonly static Dictionary<int, OutputStream> outputStreams = new Dictionary<int, OutputStream>();
		readonly static Dictionary<int, InputStream> inputStreams = new Dictionary<int, InputStream>();
		public static List<int> activeIds = new List<int>();
		/// <summary>
		/// 0 = download, 1 = Pause, 2 = remove
		/// </summary>
		public static Dictionary<int, int> isPaused = new Dictionary<int, int>();

		public static Dictionary<int, bool> isStartProgress = new Dictionary<int, bool>();

		public static EventHandler<int> changedPause;

		[System.Serializable]
		public struct DownloadHandleNot
		{
			public int id;
			public List<BasicMirrorInfo> mirrors;
			public int mirror;
			public string title;
			public string path;
			public string poster;
			public string beforeTxt;
			public bool openWhenDone;
			public bool showNotificaion;
			public bool showDoneNotificaion;
			public bool showDoneAsToast;
		}

		public static void HandleIntent(string data)
		{
			try {
				DownloadHandleNot d = JsonConvert.DeserializeObject<DownloadHandleNot>(data);
				HandleIntent(d.id, d.mirrors, d.mirror, d.title, d.path, d.poster, d.beforeTxt, d.openWhenDone, d.showNotificaion, d.showDoneNotificaion, d.showDoneAsToast, true);
			}
			catch (Exception) { }
		}

		public static bool ExecuteWithTimeLimit(TimeSpan timeSpan, Action codeBlock)
		{
			try {
				Task task = Task.Factory.StartNew(() => codeBlock());
				task.Wait(timeSpan);
				return task.IsCompleted;
			}
			catch (AggregateException ae) {
				return false;
			}
		}


		public static bool CheckIfMaster(string data)
		{
			return data.Contains("#EXT-X-STREAM-INF:");
		}

		struct M3U8MasterLink
		{
			public int bandWidth;
			public string link;
		}

		public static string[] ParseM3u8(string url, string referer)
		{
			string data = DownloadULRasText(url, referer);
			if (!data.StartsWith("#EXTM3U")) return null;
			bool masterplay = CheckIfMaster(data); // JUST CHECKS IF IT CONTAINS SEVERAL M3U8 SUBFILES
			var split = data.Split('\n');

			string endData = masterplay ? "" : data;
			string endLink = masterplay ? "" : url;
			if (masterplay) {
				List<M3U8MasterLink> masterLinks = new List<M3U8MasterLink>();
				bool nextIsUrl = false;
				for (int i = 0; i < split.Length; i++) {
					var line = split[i];
					if (line.StartsWith("#EXT-X-STREAM-INF:")) {
						int.TryParse(FindHTML(line + ",", "BANDWIDTH=", ","), out int res);
						if (res != 0) {
							nextIsUrl = true;
							masterLinks.Add(new M3U8MasterLink() { bandWidth = res, link = "" });
						}
					}
					else {
						if (nextIsUrl) {
							masterLinks[masterLinks.Count - 1] = new M3U8MasterLink() { link = line.StartsWith("http") ? line : endLink + line, bandWidth = masterLinks[masterLinks.Count - 1].bandWidth };
						}
						nextIsUrl = false;
					}

				}
				masterLinks = masterLinks.OrderBy(t => -t.bandWidth).ToList();
				for (int i = 0; i < masterLinks.Count; i++) {
					endData = DownloadULRasText(masterLinks[i].link, referer);
					if (endData != "") { endLink = masterLinks[i].link; break; };
				}
			}
			if (endData == "") return null;

			var endSplit = endData.Split('\n');
			bool linkNext = false;
			List<string> ends = new List<string>();
			for (int i = 0; i < endSplit.Length; i++) {
				var line = endSplit[i];
				if (line.StartsWith("#EXTINF:")) {
					linkNext = true;
				}
				else if (linkNext) {
					var endLine = line.StartsWith("http") ? line : endLink + line;
					ends.Add(endLine);
					linkNext = false;
				}
			}
			return ends.ToArray();
		}



		public static string DownloadULRasText(string url, string referer)
		{
			return CloudStreamCore.mainCore.DownloadString(url, referer: referer);
			/*
			try {
				URL _url = new URL(url);

				URLConnection connection = _url.OpenConnection();
				connection.ConnectTimeout = 5000; // 5 sec 
				//connection.set = "UTF-8";
				connection.Connect();
				if (connection.ContentLength > 100000) return ""; // TEXT ONLY
				InputStream input = new BufferedInputStream(connection.InputStream);
				int count = 0;
				//byte[] data = new byte[1024];

				byte[] totalData = new byte[connection.ContentLength];
				if (input.Read(totalData) > 0) {
					var data = Encoding.UTF8.GetString(totalData);
				}
				else {
					return "";
				}

			}
			catch (Exception) {
				return "";
			}*/
			//while ((count = input.Read(data)) != -1) {

			//}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="mirrorNames"></param>
		/// <param name="mirrorUrls"></param>
		/// <param name="mirror"></param>
		/// <param name="title"></param>
		/// <param name="path">FULL PATH</param>
		/// <param name="poster"></param>
		/// <param name="fileName"></param>
		/// <param name="beforeTxt"></param>
		/// <param name="openWhenDone"></param>
		/// <param name="showNotificaion"></param>
		/// <param name="showDoneNotificaion"></param>
		/// <param name="showDoneAsToast"></param>
		/// <param name="resumeIntent"></param>
		public static void HandleIntent(int id, List<BasicMirrorInfo> mirrors, int mirror, string title, string path, string poster, string beforeTxt, bool openWhenDone, bool showNotificaion, bool showDoneNotificaion, bool showDoneAsToast, bool resumeIntent)
		{
			const int UPDATE_TIME = 1;

			try {

				isStartProgress[id] = true;
				print("START DLOAD::: " + id);
				if (isPaused.ContainsKey(id)) {
					//if (isPaused[id] == 2) {
					//  isPaused.Remove(id);
					//    print("YEET DELETED KEEEE");
					return;
					//  }
				}
				var context = Application.Context;

				//$"{nameof(id)}={id}|||{nameof(title)}={title}|||{nameof(path)}={path}|||{nameof(poster)}={poster}|||{nameof(fileName)}={fileName}|||{nameof(beforeTxt)}={beforeTxt}|||{nameof(openWhenDone)}={openWhenDone}|||{nameof(showDoneNotificaion)}={showDoneNotificaion}|||{nameof(showDoneAsToast)}={showDoneAsToast}|||");

				int progress = 0;

				int bytesPerSec = 0;

				void UpdateDloadNot(string progressTxt)
				{
					//poster != ""
					if (!isPaused.ContainsKey(id)) {
						isPaused[id] = 0;
					}
					try {
						int isPause = isPaused[id];
						bool canPause = isPause == 0;
						if (isPause != 2) {
							ShowLocalNot(new LocalNot() { actions = new List<LocalAction>() { new LocalAction() { action = $"handleDownload|||id={id}|||dType={(canPause ? 1 : 0)}|||", name = canPause ? "Pause" : "Resume" }, new LocalAction() { action = $"handleDownload|||id={id}|||dType=2|||", name = "Stop" } }, mediaStyle = false, bigIcon = poster, title = $"{title} - {ConvertBytesToAny(bytesPerSec / UPDATE_TIME, 2, 2)} MB/s", autoCancel = false, showWhen = false, onGoing = canPause, id = id, smallIcon = PublicNot, progress = progress, body = progressTxt }, context); //canPause
						}
					}
					catch (Exception _ex) {
						print("ERRORLOADING PROGRESS:::" + _ex);
					}

				}

				void ShowDone(bool succ, string overrideText = null)
				{
					print("DAAAAAAAAAASHOW DONE" + succ);
					if (showDoneNotificaion) {
						print("DAAAAAAAAAASHOW DONE!!!!");
						Device.BeginInvokeOnMainThread(() => {
							try {
								print("DAAAAAAAAAASHOW DONE222");
								ShowLocalNot(new LocalNot() { mediaStyle = poster != "", bigIcon = poster, title = title, autoCancel = true, onGoing = false, id = id, smallIcon = PublicNot, body = overrideText ?? (succ ? "Download done!" : "Download Failed") }, context); // ((e.Cancelled || e.Error != null) ? "Download Failed!"
							}
							catch (Exception _ex) {
								print("SUPERFATALEX: " + _ex);
							}
						});
						//await Task.Delay(1000); // 100% sure that it is downloaded
						OnSomeDownloadFinished?.Invoke(null, EventArgs.Empty);
					}
					else {
						print("DONT SHOW WHEN DONE");
					}
					// Toast.MakeText(context, "PG DONE!!!", ToastLength.Long).Show(); 
				}



				void StartT()
				{
					isStartProgress[id] = true;
					if (isPaused.ContainsKey(id)) {
						//if (isPaused[id] == 2) {
						//    isPaused.Remove(id);
						return;
						//  }
					}

					Thread t = new Thread(() => {
						print("START:::");
						string json = JsonConvert.SerializeObject(new DownloadHandleNot() { id = id, mirrors = mirrors, showDoneAsToast = showDoneAsToast, openWhenDone = openWhenDone, showDoneNotificaion = showDoneNotificaion, beforeTxt = beforeTxt, mirror = mirror, path = path, poster = poster, showNotificaion = showNotificaion, title = title });

						App.SetKey(DOWNLOAD_KEY_INTENT, id.ToString(), json);

						var mirr = mirrors[mirror];

						string url = mirr.mirror;
						string urlName = mirr.name;
						string referer = mirr.referer ?? "";
						bool isM3u8 = url.Contains(".m3u8");

						if ((int)Android.OS.Build.VERSION.SdkInt > 9) {
							StrictMode.ThreadPolicy policy = new
							StrictMode.ThreadPolicy.Builder().PermitAll().Build();
							StrictMode.SetThreadPolicy(policy);
						}
						long total = 0;
						int fileLength = 0;

						void UpdateProgress()
						{
							UpdateDloadNot($"{beforeTxt.Replace("{name}", urlName)}{progress} % ({ConvertBytesToAny(total, 1, 2)} MB/{ConvertBytesToAny(fileLength, 1, 2)} MB)");
						}

						void UpdateFromId(object sender, int _id)
						{
							if (_id == id) {
								UpdateProgress();
							}
						}

						bool removeKeys = true;
						var rFile = new Java.IO.File(path);

						try {
							// CREATED DIRECTORY IF NEEDED
							try {
								Java.IO.File __file = new Java.IO.File(path);
								__file.Mkdirs();
							}
							catch (Exception _ex) {
								print("FAILED:::" + _ex);
							}

							URL _url = new URL(url);

							URLConnection connection = _url.OpenConnection();

							print("SET CONNECT ::");
							if (!rFile.Exists()) {
								print("FILE DOSENT EXITS");
								rFile.CreateNewFile();
							}
							else {
								if (resumeIntent) {
									total = rFile.Length();
									connection.SetRequestProperty("Range", "bytes=" + rFile.Length() + "-");
								}
								else {
									rFile.Delete();
									rFile.CreateNewFile();
								}
							}
							print("SET CONNECT ::2");
							connection.SetRequestProperty("Accept-Encoding", "identity");
							if (referer != "") {
								connection.SetRequestProperty("Referer", referer);
							}
							int clen = 0;

							if (isM3u8) {
								clen = 1;
							}
							else {
								bool Completed = ExecuteWithTimeLimit(TimeSpan.FromMilliseconds(10000), () => {
									connection.Connect();
									clen = connection.ContentLength;
									if (clen < 5000000 && !path.Contains("/YouTube/")) { // min of 5 MB 
										clen = 0;
									}
								});
								if (!Completed) {
									print("FAILED MASS");
									clen = 0;
								}
								print("SET CONNECT ::3");
							}

							print("TOTALLLLL::: " + clen);

							if (clen == 0) {
								if (isStartProgress.ContainsKey(id)) {
									isStartProgress.Remove(id);
								}
								if (mirror < mirrors.Count - 1 && progress < 2 && rFile.Length() < 1000000) { // HAVE MIRRORS LEFT
									mirror++;
									removeKeys = false;
									resumeIntent = false;
									rFile.Delete();

									print("DELETE;;;");
								}
								else {
									ShowDone(false);
								}
							}
							else {
								fileLength = clen + (int)total;
								print("FILELEN:::: " + fileLength);
								App.SetKey("dlength", "id" + id, fileLength);
								string fileExtension = MimeTypeMap.GetFileExtensionFromUrl(url);
								InputStream input = new BufferedInputStream(connection.InputStream);

								//long skip = App.GetKey<long>(DOWNLOAD_KEY, id.ToString(), 0);

								OutputStream output = new FileOutputStream(rFile, true);

								outputStreams[id] = output;
								inputStreams[id] = input;

								if (isPaused.ContainsKey(id)) {
									//if (isPaused[id] == 2) {
									//    isPaused.Remove(id);
									return;
									//  }
								}

								isPaused[id] = 0;
								activeIds.Add(id);

								int m3u8Progress = 0;

								int cProgress()
								{
									if (isM3u8) {
										return m3u8Progress;
									}
									return (int)(total * 100 / fileLength);
								}
								progress = cProgress();

								byte[] data = new byte[1024];
								// skip;
								int count;
								int previousProgress = 0;
								UpdateDloadNot(total == 0 ? "Download starting" : "Download resuming");

								System.DateTime lastUpdateTime = System.DateTime.Now;
								long lastTotal = total;

								changedPause += UpdateFromId;

								if (isStartProgress.ContainsKey(id)) {
									isStartProgress.Remove(id);
								}
								bool showDone = true;

								bool WriteDataUpdate()
								{
									progressDownloads[id] = total;
									progress = cProgress();

									if (isPaused[id] == 1) {
										print("PAUSEDOWNLOAD");
										UpdateProgress();
										while (isPaused[id] == 1) {
											Thread.Sleep(100);
										}
										if (isPaused[id] != 2) {
											UpdateProgress();
										}
									}
									if (isPaused[id] == 2) { // DELETE FILE
										print("DOWNLOAD STOPPED");
										ShowDone(false, "Download Stopped");
										output.Flush();
										output.Close();
										input.Close();
										outputStreams.Remove(id);
										inputStreams.Remove(id);
										isPaused.Remove(id);
										rFile.Delete();
										App.RemoveKey(DOWNLOAD_KEY, id.ToString());
										App.RemoveKey(DOWNLOAD_KEY_INTENT, id.ToString());
										App.RemoveKey(App.hasDownloadedFolder, id.ToString());
										App.RemoveKey("dlength", "id" + id);
										App.RemoveKey("DownloadIds", id.ToString());
										changedPause -= UpdateFromId;
										activeIds.Remove(id);
										removeKeys = true;
										OnSomeDownloadFailed?.Invoke(null, EventArgs.Empty);
										Thread.Sleep(100);
										return true;
									}

									if (DateTime.Now.Subtract(lastUpdateTime).TotalSeconds > UPDATE_TIME) {
										lastUpdateTime = DateTime.Now;
										long diff = total - lastTotal;
										//  UpdateDloadNot($"{ConvertBytesToAny(diff/UPDATE_TIME, 2,2)}MB/s | {progress}%");
										//{ConvertBytesToAny(diff / UPDATE_TIME, 2, 2)}MB/s | 
										if (progress >= 100) {
											print("DLOADPG DONE!");
											ShowDone(true);
										}
										else {
											UpdateProgress();
											// UpdateDloadNot(progress + "%");
										}
										bytesPerSec = 0;

										lastTotal = total;
									}

									if (progress >= 100 || progress > previousProgress) {
										// Only post progress event if we've made progress.
										previousProgress = progress;
										if (progress >= 100) {
											print("DLOADPG DONE!");
											ShowDone(true);
											showDone = false;
										}
										else {
											// UpdateProgress();
											// UpdateDloadNot(progress + "%");
										}
									}
									return false;
								}


								if (isM3u8) {
									var links = ParseM3u8(url, referer);
									int counter = 0;
									byte[] buffer;
									try {
										while ((buffer = CloudStreamCore.DownloadByteArrayFromUrl(links[counter], referer)) != null) {
											counter++;
											m3u8Progress = counter * 100 / links.Length;
											count = buffer.Length;
											total += count;
											bytesPerSec += count;
											output.Write(buffer, 0, count);
											if (WriteDataUpdate()) return;
										}
									}
									catch (Exception) {
										if (WriteDataUpdate()) return;
									}
								}
								else {
									try {
										while ((count = input.Read(data)) != -1) {
											total += count;
											bytesPerSec += count;

											output.Write(data, 0, count);
											if (WriteDataUpdate()) return;
										}
									}
									catch (Exception) {
										if (WriteDataUpdate()) return;
									}
								}

								if (showDone) {
									ShowDone(true);
								}
								output.Flush();
								output.Close();
								input.Close();
								outputStreams.Remove(id);
								inputStreams.Remove(id);
								activeIds.Remove(id);
							}
						}
						catch (Exception _ex) {
							print("DOWNLOADURL: " + url);
							print("DOWNLOAD FAILED BC: " + _ex);
							if (mirror < mirrors.Count - 1 && progress < 2) { // HAVE MIRRORS LEFT
								mirror++;
								removeKeys = false;
								resumeIntent = false;
								rFile.Delete();
							}
							else {
								ShowDone(false);
							}
						}
						finally {
							changedPause -= UpdateFromId;
							isPaused.Remove(id);
							if (isStartProgress.ContainsKey(id)) {
								isStartProgress.Remove(id);
							}
							if (removeKeys) {
								App.RemoveKey(DOWNLOAD_KEY, id.ToString());
								App.RemoveKey(DOWNLOAD_KEY_INTENT, id.ToString());
							}
							else {
								StartT();
							}
						}
					});
					t.Start();
				}
				StartT();
			}
			catch (Exception) {
			}
		}
	}
}