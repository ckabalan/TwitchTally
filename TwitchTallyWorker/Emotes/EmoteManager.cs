using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
		private static readonly Dictionary<String, DateTime> EmoteQueryTimes = new Dictionary<String, DateTime>();

		public static void ImportFromRedis() {
			Logger.Info($"Importing Emotes from Redis... (Current: {EmoteList.Count})");
			IDatabase db = DataStore.Redis.GetDatabase();
			RedisValue[] emoteArr = db.SetMembers("emotelist");
			foreach (RedisValue curEmote in emoteArr) {
				String decoded = DataStore.KeyDecode(curEmote);
				if (!EmoteList.Contains(decoded)) {
					EmoteList.Add(DataStore.KeyDecode(curEmote));
				}
			}
			EmoteArr = EmoteList.ToArray();
			EmoteHashSet = new HashSet<String>(EmoteArr);
			GetDownloadTimes();
		}

		public static void GetDownloadTimes() {
			IDatabase db = DataStore.Redis.GetDatabase();
			RedisValue[] emoteSourceArr = db.SetMembers("emotesourcelist");
			foreach (RedisValue curEmoteSource in emoteSourceArr) {
				String key = curEmoteSource.ToString().Substring(12);
				DateTime value = DateTime.Parse(db.StringGet(curEmoteSource.ToString()));
				if (EmoteQueryTimes.ContainsKey(key)) {
					EmoteQueryTimes[key] = value;
				} else {
					EmoteQueryTimes.Add(key, value);
				}
				
			}
		}

		public static void Download() {
			GetGlobalOfficialEmotes();
			GetSubOfficialEmotes();
			GetGlobalBttvEmotes();
		}

		private static String GetHttpsAgnostic(String url) {
			if (Environment.OSVersion.Platform == PlatformID.Unix) {
				Process proc = new System.Diagnostics.Process {
					StartInfo = {
						FileName = "/bin/bash",
						Arguments = $"-c 'curl -s {url}'",
						UseShellExecute = false,
						RedirectStandardOutput = true
					}
				};
				proc.Start();
				return proc.StandardOutput.ReadToEnd();
				//proc.WaitForExit();
			} else {
				System.Security.Cryptography.AesCryptoServiceProvider b = new System.Security.Cryptography.AesCryptoServiceProvider();
				ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
				using (HttpClient client = new HttpClient())
				using (HttpResponseMessage response = client.GetAsync(url).Result)
				using (HttpContent content = response.Content) {
					return content.ReadAsStringAsync().Result;
				}
			}
		}

		public static void GetGlobalOfficialEmotes() {
			if (!ShouldQueryAgain($"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}global")) {
				//Logger.Info("Skipping Global Twitch.tv Emote Download (Too Soon)...");
				return;
			}
			Logger.Info("Downloading Global Twitch.tv Emotes.");
			IDatabase db = DataStore.Redis.GetDatabase();
			List<Task> taskList = new List<Task>();
			String rawJson = GetHttpsAgnostic(@"https://twitchemotes.com/api_cache/v2/global.json");
			if (rawJson.Length > 0) {
				JObject emoteObj = JObject.Parse(rawJson);
				foreach (KeyValuePair<String, JToken> curKVP in ((JObject)emoteObj["emotes"])) {
					JObject curEmoteJson = curKVP.Value as JObject;
					Emote curEmote = new Emote {
						Code = curKVP.Key,
						CodeSafe = DataStore.KeyEncode(curKVP.Key),
						IsGlobal = true,
						IsOfficial = true,
						IsAnimated = false,
						Description = curEmoteJson?["description"].ToString(),
						ImageId = curEmoteJson?["image_id"].ToString(),
						SetId = "-1",
						Channel = String.Empty,
						Link = String.Empty,
						BadgeUrl = String.Empty
					};
					taskList.AddRange(SubmitEmote(curEmote));
					if (taskList.Count > 100) {
						DataStore.WaitTasks();
					}
				}
			}
			DataStore.WaitTasks();
			db.StringSet($"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}global", DateTime.UtcNow.ToString("O"));
			db.SetAdd("emotesourcelist", $"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}global");
			ImportFromRedis();
		}

		public static void GetSubOfficialEmotes() {
			if (!ShouldQueryAgain($"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}sub")) {
				//Logger.Info("Skipping Subscriber Twitch.tv Emote Download (Too Soon)...");
				return;
			}
			Logger.Info("Downloading Subscriber Twitch.tv Emotes.");
			IDatabase db = DataStore.Redis.GetDatabase();
			List<Task> taskList = new List<Task>();
			String rawJson = GetHttpsAgnostic(@"https://twitchemotes.com/api_cache/v2/subscriber.json");
			if (rawJson.Length > 0) {
				JObject emoteObj = JObject.Parse(rawJson);
				foreach (KeyValuePair<String, JToken> curKVP in ((JObject)emoteObj["channels"])) {
					String channelName = curKVP.Key;
					JObject curChannelJson = curKVP.Value as JObject;
					if (curChannelJson != null) {
						Emote tempEmote = new Emote {
							IsGlobal = false,
							IsOfficial = true,
							IsAnimated = false,
							Channel = curChannelJson["title"].ToString(),
							Link = curChannelJson["link"].ToString(),
							Description = curChannelJson["desc"].ToString(),
							BadgeUrl = curChannelJson["badge"].ToString(),
							SetId = curChannelJson["set"].ToString()
						};
						foreach (JToken jToken in ((JArray)curChannelJson["emotes"])) {
							JObject jsonEmoteObj = (JObject)jToken;
							if (jsonEmoteObj != null) {
								Emote curEmote = tempEmote;
								curEmote.Code = jsonEmoteObj["code"].ToString();
								curEmote.CodeSafe = DataStore.KeyEncode(curEmote.Code);
								curEmote.ImageId = jsonEmoteObj["image_id"].ToString();
								taskList.AddRange(SubmitEmote(curEmote));
								if (taskList.Count > 100) {
									DataStore.WaitTasks();
								}
							}
						}
					}
				}
				foreach (JToken jToken in ((JArray)emoteObj["unknown_emotes"]["emotes"])) {
					JObject jsonEmoteObj = (JObject)jToken;
					Emote tempEmote = new Emote {
						IsGlobal = false,
						IsOfficial = true,
						IsAnimated = false,
						Channel = String.Empty,
						Link = String.Empty,
						Description = String.Empty,
						BadgeUrl = String.Empty,
						SetId = jsonEmoteObj["set"].ToString(),
						Code = jsonEmoteObj["code"].ToString(),
						CodeSafe = DataStore.KeyEncode(jsonEmoteObj["code"].ToString()),
						ImageId = jsonEmoteObj["image_id"].ToString()
					};
					taskList.AddRange(SubmitEmote(tempEmote));
					if (taskList.Count > 100) {
						DataStore.WaitTasks();
					}
				}
			}
			DataStore.WaitTasks();
			db.StringSet($"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}sub", DateTime.UtcNow.ToString("O"));
			db.SetAdd("emotesourcelist", $"emotesource{DataStore.Delimiter}twitch{DataStore.Delimiter}sub");
			ImportFromRedis();
		}

		public static void GetGlobalBttvEmotes() {
			if (!ShouldQueryAgain($"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}global")) {
				//Logger.Info("Skipping Global BetterTTV Emote Download (Too Soon)...");
				return;
			}
			Logger.Info("Downloading Global BetterTTV Emotes.");
			List<Task> taskList = new List<Task>();
			IDatabase db = DataStore.Redis.GetDatabase();
			String rawJson = GetHttpsAgnostic(@"https://api.betterttv.net/2/emotes");
			if (rawJson.Length > 0) {
				JObject emoteObj = JObject.Parse(rawJson);
				foreach (JToken jToken in ((JArray)emoteObj["emotes"])) {
					JObject jsonEmoteObj = (JObject)jToken;
					Emote tempEmote = new Emote {
						IsGlobal = true,
						IsOfficial = false,
						IsAnimated = (jsonEmoteObj["imageType"].ToString().ToLower() == "gif" ? true : false),
						Channel = (jsonEmoteObj["channel"].ToString().ToLower() == "null" ? String.Empty : jsonEmoteObj["channel"].ToString()),
						Link = String.Empty,
						Description = String.Empty,
						BadgeUrl = String.Empty,
						SetId = String.Empty,
						Code = jsonEmoteObj["code"].ToString(),
						CodeSafe = DataStore.KeyEncode(jsonEmoteObj["code"].ToString()),
						ImageId = jsonEmoteObj["id"].ToString()
					};
					taskList.AddRange(SubmitEmote(tempEmote));
					if (taskList.Count > 100) {
						DataStore.WaitTasks();
					}
				}
			}
			DataStore.WaitTasks();
			db.StringSet($"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}global", DateTime.UtcNow.ToString("O"));
			db.SetAdd("emotesourcelist", $"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}global");
			ImportFromRedis();
		}

		public static void GetChannelBttvEmotes(String channel) {
			channel = channel.ToLower().Replace("#", "");
			if (!ShouldQueryAgain($"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}{channel}")) {
				//Logger.Info($"Skipping '{channel}' BetterTTV Emote Download (Too Soon)...");
				return;
			}
			Logger.Info($"Downloading '{channel}' BetterTTV Emotes.");
			List<Task> taskList = new List<Task>();
			IDatabase db = DataStore.Redis.GetDatabase();
			String rawJson = GetHttpsAgnostic(@"https://api.betterttv.net/2/channels/" + channel);
			if (rawJson.Length > 0) {
				JObject emoteObj = JObject.Parse(rawJson);
				if (emoteObj["status"].ToString() == "200") {
					foreach (JToken jToken in ((JArray)emoteObj["emotes"])) {
						JObject jsonEmoteObj = (JObject)jToken;
						Emote tempEmote = new Emote {
							IsGlobal = false,
							IsOfficial = false,
							IsAnimated = (jsonEmoteObj["imageType"].ToString().ToLower() == "gif" ? true : false),
							Channel = (jsonEmoteObj["channel"].ToString().ToLower() == "null" ? String.Empty : jsonEmoteObj["channel"].ToString()),
							Link = String.Empty,
							Description = String.Empty,
							BadgeUrl = String.Empty,
							SetId = String.Empty,
							Code = jsonEmoteObj["code"].ToString(),
							CodeSafe = DataStore.KeyEncode(jsonEmoteObj["code"].ToString()),
							ImageId = jsonEmoteObj["id"].ToString()
						};
						taskList.AddRange(SubmitEmote(tempEmote));
						if (taskList.Count > 100) {
							DataStore.WaitTasks();
						}
					}
				}
			}
			DataStore.WaitTasks();
			db.StringSet($"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}{channel}", DateTime.UtcNow.ToString("O"));
			db.SetAdd("emotesourcelist", $"emotesource{DataStore.Delimiter}bttv{DataStore.Delimiter}{channel}");
			ImportFromRedis();
		}


		private static List<Task> SubmitEmote(Emote emote) {
			List<Task> taskList = new List<Task>();
			IDatabase db = DataStore.Redis.GetDatabase();
			Logger.Debug($"Updating Emote Code {emote.Code} (Base64: {emote.CodeSafe}).");
			HashEntry[] emoteHash = new HashEntry[] {
				new HashEntry("isofficial", emote.IsOfficial),
				new HashEntry("isglobal", emote.IsGlobal),
				new HashEntry("isanimated", emote.IsAnimated),
				new HashEntry("channel", (emote.Channel ?? String.Empty).ToString()),
				new HashEntry("link", (emote.Link ?? String.Empty).ToString()),
				new HashEntry("description", (emote.Description ?? String.Empty).ToString()),
				new HashEntry("badgeurl", (emote.BadgeUrl ?? String.Empty).ToString()),
				new HashEntry("setid", emote.SetId),
				new HashEntry("code", (emote.Code ?? String.Empty).ToString()),
				new HashEntry("codesafe", (emote.CodeSafe ?? String.Empty).ToString()),
				new HashEntry("imageid", emote.ImageId),
			};
			taskList.Add(db.HashSetAsync($"emote{DataStore.Delimiter}{emote.CodeSafe}", emoteHash));
			taskList.Add(db.SetAddAsync("emotelist", emote.CodeSafe, CommandFlags.FireAndForget));
			return taskList;
		}

		private static Boolean ShouldQueryAgain(String key, Boolean live = false) {
			DateTime lastQuery;
			if (live) {
				IDatabase db = DataStore.Redis.GetDatabase();
				RedisValue returnValue = db.StringGet(key);
				if (!returnValue.IsNull) {
					lastQuery = DateTime.Parse(returnValue.ToString());
				} else {
					return true;
				}
			} else {
				key = key.Substring(12);
				if (EmoteQueryTimes.ContainsKey(key)) {
					lastQuery = EmoteQueryTimes[key];
				} else {
					return true;
				}
			}
			TimeSpan elapsed = DateTime.Now.Subtract(lastQuery);
			if (elapsed.TotalMinutes < 30) {
				// We queried more than 30min ago, query again.
				return false;
			}
			return true;
		}
	}
}
