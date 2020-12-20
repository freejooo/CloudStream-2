using CloudStreamForms.Core;
using System;
using System.Collections.Generic;

namespace CloudStreamForms.Script
{
	static class SyncWrapper
	{
		public const string header = "CLOUDSTREAM USERDATA";

		static string AddFolder<T>(string folder, string[] keys)
		{
			string s = $"FOLDER[{folder}]RESULTS[{keys.Length}]TYPE[{typeof(T).Name}]\n";
			for (int i = 0; i < keys.Length; i++) {
				s += $"KEY[{keys[i]}]DATA[{App.GetRawKey(keys[i])}]\n";
			}
			return s;
		}

		static string AddFolder<T>(string folder)
		{
			string[] viewTimePosKeys = App.GetKeysPath(folder);
			return AddFolder<T>(folder, viewTimePosKeys);
		}

		public static string GenerateTextFile(bool everything)
		{
			string text = header + "\n";
			if (everything) {
				text += "CLEAREVERYTHING\n";
				var keys = App.GetAllKeys();
				foreach (var key in keys) {
					var _k = App.GetRawKey(key, null);
					if (_k != null) {
						text += $"KEY[{key}]DATA[{_k}]\n";
					}
				}
			}
			else {
				text += AddFolder<long>(App.VIEW_TIME_POS);
				text += AddFolder<long>(App.VIEW_TIME_DUR);
				text += AddFolder<string>(App.BOOKMARK_DATA);
				text += AddFolder<bool>(App.VIEW_HISTORY);
			}

			return text;
		}

		static string FindString(string line, string data)
		{
			return CloudStreamCore.FindHTML(line, $"{data}[", "]");
		}

		public static bool SetKeysFromTextFile(string text)
		{
			try {
				if (text.StartsWith(header)) {
					string[] lines = text.Split('\n');
					int count = 0;
					while (count < lines.Length) {
						count++;
						var line = lines[count];
						if (line.StartsWith("#")) continue; // METADATA
						if (line.StartsWith("CLEAREVERYTHING")) {
							App.ClearEveryKey();
						}
						if (line.StartsWith("KEY")) {
							App.SetRawKey(FindString(line, "KEY"), FindString(line, "DATA"));
						}
						if (line.StartsWith("FOLDER")) {
							App.RemoveFolder(FindString(line, "FOLDER"));

							int results = int.Parse(FindString(line, "RESULTS"));
							//string dataType = FindString(line, "TYPE");
							//  var dt = types.Where(t => t.Name == dataType).ToList()[0];
							for (int i = 0; i < results; i++) {
								count++;
								var subline = lines[count];
								App.SetRawKey(FindString(subline, "KEY"), FindString(subline, "DATA"));
							}
						}
					}

					return true;
				}
				else {
					return false; // FAILED TO PARSE THE FILE
				}
			}
			catch (Exception) {
				return false;
			}
		}
	}
}
