using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace TwitchTallyWorker.DataManagement {
	public static class DataStore {
		private static ConnectionMultiplexer _redis;
		public static List<Task> Tasks = new List<Task>();

		public static ConnectionMultiplexer Redis {
			get { return _redis; }
			set { _redis = value; }
		}

		public static void Connect(String connectString) {
			_redis = ConnectionMultiplexer.Connect(connectString);
		}

		public static void WaitTasks() {
			//Stopwatch stopWatch = new Stopwatch();
			//stopWatch.Start();
			Task.WaitAll(Tasks.ToArray());
			Tasks.Clear();
			//stopWatch.Stop();
			//TimeSpan ts = stopWatch.Elapsed;
		}
	}
}
