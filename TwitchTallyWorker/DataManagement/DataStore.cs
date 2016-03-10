// <copyright file="DataStore.cs" company="SpectralCoding.com">
//     Copyright (c) 2016 SpectralCoding
// </copyright>
// <license>
// This file is part of TwitchTally.
// 
// TwitchTally is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// TwitchTally is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with TwitchTally.  If not, see <http://www.gnu.org/licenses/>.
// </license>
// <author>Caesar Kabalan</author>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace TwitchTallyWorker.DataManagement {
	public static class DataStore {
		private static ConnectionMultiplexer _redis;
		public static List<Task> Tasks = new List<Task>();
		public const String Delimiter = ":";

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

		public static String KeyEncode(String input) {
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(input));
		}
		public static String KeyDecode(String input) {
			return Encoding.UTF8.GetString(Convert.FromBase64String(input));
		}
	}
}
