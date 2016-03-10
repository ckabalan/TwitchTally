using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using StackExchange.Redis;
using TwitchTallyWorker.DataManagement;

namespace TwitchTallyWorker.Emotes {
	public static class EmoteManager {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public static List<string> EmoteList { get; set; } = new List<String>();
		public static String[] EmoteArr { get; set; }
		public static HashSet<String> EmoteHashSet { get; set; }

		public static void Download(Boolean globalEmotesOnly) {
			if (globalEmotesOnly) {
				GetGlobalOfficialEmotes();
				GetGlobalBttvEmotes();
			} else {
				//UpdateAllEmotes();
			}
			////GetAllEmotes();
			DumpEmotes();
		}

		private static void DumpEmotes() {
			var db = DataStore.Redis.GetDatabase();
			RedisValue[] emoteArr = db.SetMembers("emotelist");
			foreach (RedisValue curEmote in emoteArr) {
				EmoteList.Add(DataStore.KeyDecode(curEmote));
			}
			EmoteArr = EmoteList.ToArray();
			EmoteHashSet = new HashSet<String>(EmoteArr);
		}

		private static void GetGlobalOfficialEmotes() {
			Logger.Info("Downloading Global Twitch.tv Emotes.");
			IDatabase db = DataStore.Redis.GetDatabase();
			String rawJson = new WebClient().DownloadString(@"http://twitchemotes.com/api_cache/v2/global.json");
			dynamic dynamicObj = JsonConvert.DeserializeObject(rawJson);
			JObject jsonObj = (JObject)dynamicObj;
			foreach (JToken jsonTopToken in jsonObj.Children()) {
				if (jsonTopToken is JProperty) {
					JProperty jsonTopProperty = jsonTopToken as JProperty;
					if (jsonTopProperty.Name == "emotes") {
						List<Task> taskList = new List<Task>();
						foreach (JToken jsonEmoteToken in jsonTopProperty.Value.Children()) {
							JProperty jsonEmoteProperty = jsonEmoteToken as JProperty;
							String emoteName = DataStore.KeyEncode(jsonEmoteProperty.Name);
							Logger.Debug($"Updating Twitch Emote Code {jsonEmoteProperty.Name} (Base64: {emoteName}).");
							HashEntry[] emoteHash = new HashEntry[] {
								new HashEntry("id", (jsonEmoteProperty.Value["image_id"] ?? String.Empty).ToString()),
								new HashEntry("description", (jsonEmoteProperty.Value["description"] ?? String.Empty).ToString()),
								new HashEntry("animated", false),
							};
							taskList.Add(db.HashSetAsync($"emote{DataStore.Delimiter}{emoteName}", emoteHash));
							taskList.Add(db.SetAddAsync("emotelist", emoteName.ToString(), CommandFlags.FireAndForget));
							if (taskList.Count > 100) {
								DataStore.WaitTasks();
							}
						}
					}
				}
			}
		}

		private static void GetGlobalBttvEmotes() {
			Logger.Info("Downloading Global BetterTTV Emotes.");
			IDatabase db = DataStore.Redis.GetDatabase();
			String rawJson = new WebClient().DownloadString(@"https://api.betterttv.net/2/emotes");
			dynamic dynamicObj = JsonConvert.DeserializeObject(rawJson);
			JObject jsonObj = (JObject)dynamicObj;
			foreach (JToken jsonTopToken in jsonObj.Children()) {
				if (jsonTopToken is JProperty) {
					JProperty jsonTopProperty = jsonTopToken as JProperty;
					if (jsonTopProperty.Name == "emotes") {
						List<Task> taskList = new List<Task>();
						foreach (JToken jsonEmoteToken in jsonTopProperty.Value.Children()) {
							String emoteName = DataStore.KeyEncode(jsonEmoteToken["code"].ToString());
							Logger.Debug($"Updating BTTV Emote Code {jsonEmoteToken["code"].ToString()} (Base64: {emoteName}).");
							HashEntry[] emoteHash = new HashEntry[] {
								new HashEntry("id", (jsonEmoteToken["id"] ?? String.Empty).ToString()),
								new HashEntry("description", (jsonEmoteToken["description"] ?? String.Empty).ToString()),
								new HashEntry("animated", (jsonEmoteToken["imageType"].ToString().ToLower() == "gif" ? true : false).ToString()),
							};
							taskList.Add(db.HashSetAsync($"emote{DataStore.Delimiter}{emoteName}", emoteHash));
							taskList.Add(db.SetAddAsync("emotelist", emoteName.ToString(), CommandFlags.FireAndForget));
							if (taskList.Count > 100) {
								DataStore.WaitTasks();
							}
						}
					}
				}
			}
		}

	}
}
