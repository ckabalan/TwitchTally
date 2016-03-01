using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using StackExchange.Redis;
using TwitchTallyWorker.DataManagement;

namespace TwitchTallyWorker.Processing {
	public static class LineParser {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static Int32[] _accuracies;

		public static void SetAccuracies(String accuracies) {
			String[] accStrings = accuracies.Split(',');
			_accuracies = new Int32[accStrings.Length];
			for (Int32 i = 0; i < accStrings.Length; i++) {
				Int32 multiplier;
				Int32 unitSize;
				String curAcc = accStrings[i];
				Int32.TryParse(curAcc.Substring(0, curAcc.Length - 1), out unitSize);
				switch (curAcc.Substring(curAcc.Length - 1).ToUpper()) {
					case "S":
						multiplier = 1;
						break;
					case "M":
						multiplier = 60;
						break;
					case "H":
						multiplier = 60*60;
						break;
					case "D":
						multiplier = 60*60*24;
						break;
					default:
						multiplier = 1;
						break;
				}
				_accuracies[i] = unitSize * multiplier;
			}
		}


		public static void Message(DateTime dateTime, Dictionary<String, String> options, String channel, String username, String message) {
			AddMessageChannel(dateTime, "_global");
			AddMessageChannel(dateTime, channel);
		}

		public static void Action(DateTime dateTime, Dictionary<String, String> options, String channel, String username, String message) {
			AddActionChannel(dateTime, "_global");
			AddActionChannel(dateTime, channel);
		}

		public static void Join(DateTime dateTime, Dictionary<String, String> options, String channel, String username) {
			AddJoinChannel(dateTime, "_global");
			AddJoinChannel(dateTime, channel);

		}

		public static void Part(DateTime dateTime, Dictionary<String, String> options, String channel, String username, String message) {
			AddPartChannel(dateTime, "_global");
			AddPartChannel(dateTime, channel);
		}



		private static void AddMessageChannel(DateTime date, String channel) {
			IDatabase db = DataStore.Redis.GetDatabase();
			foreach (Int32 curAcc in _accuracies) {
				Int32 timeId = GetTimeID(date, curAcc);
				String htName = $"Line:{channel}|{curAcc}";
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Messages"));
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Total"));
				// Leave this off until we're sure we need it
				DataStore.Tasks.Add(db.SetAddAsync("Lines", htName));
			}
		}

		private static void AddActionChannel(DateTime date, String channel) {
			IDatabase db = DataStore.Redis.GetDatabase();
			foreach (Int32 curAcc in _accuracies) {
				Int32 timeId = GetTimeID(date, curAcc);
				String htName = $"Line:{channel}|{curAcc}";
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Actions"));
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Total"));
				// Leave this off until we're sure we need it
				DataStore.Tasks.Add(db.SetAddAsync("Lines", htName));
			}
		}

		private static void AddJoinChannel(DateTime date, String channel) {
			IDatabase db = DataStore.Redis.GetDatabase();
			foreach (Int32 curAcc in _accuracies) {
				Int32 timeId = GetTimeID(date, curAcc);
				String htName = $"Line:{channel}|{curAcc}";
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Joins"));
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Total"));
				// Leave this off until we're sure we need it
				DataStore.Tasks.Add(db.SetAddAsync("Lines", htName));
			}
		}

		private static void AddPartChannel(DateTime date, String channel) {
			IDatabase db = DataStore.Redis.GetDatabase();
			foreach (Int32 curAcc in _accuracies) {
				Int32 timeId = GetTimeID(date, curAcc);
				String htName = $"Line:{channel}|{curAcc}";
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Parts"));
				DataStore.Tasks.Add(db.HashIncrementAsync(htName, $"{timeId}|Total"));
				// Leave this off until we're sure we need it
				DataStore.Tasks.Add(db.SetAddAsync("Lines", htName));
			}
		}

		private static Int32 GetTimeID(DateTime date, Int32 accuracySeconds) {
			DateTime fakeEpoch = new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt32(Math.Floor((date - fakeEpoch).TotalSeconds / accuracySeconds));
		}
	}
}
