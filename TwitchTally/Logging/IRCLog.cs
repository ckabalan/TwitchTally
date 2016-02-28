// <copyright file="IRCLog.cs" company="SpectralCoding.com">
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
using System.IO;
using System.Reflection;

namespace TwitchTally.Logging {
	public static class IrcLog {
		private static FileStream _logFile;
		private static StreamWriter _logStream;

		private static void OpenLog() {
			String logDir = Properties.Settings.Default.LogDirectory.TrimEnd('/', '\\');
			if (!Directory.Exists(Properties.Settings.Default.LogDirectory)) {
				Directory.CreateDirectory(Properties.Settings.Default.LogDirectory);
			}
			String logname = $"{logDir}/{DateTime.UtcNow:yyyyMMdd-HH}.log";
			// ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
			if (File.Exists(logname)) {
				_logFile = new FileStream($"{logDir}/{DateTime.UtcNow:yyyyMMdd-HH}.log", FileMode.Append, FileAccess.Write);
			} else {
				_logFile = new FileStream($"{logDir}/{DateTime.UtcNow:yyyyMMdd-HH}.log", FileMode.Create, FileAccess.Write);
			}
			_logStream = new StreamWriter(_logFile) {AutoFlush = true};
			WriteLine($"Opened. TwitchTally v{Assembly.GetExecutingAssembly().GetName().Version}", true);
		}

		public static void CloseLog() {
			WriteLine("Closed.", true);
			_logStream.Close();
			_logFile.Close();
		}

		public static void WriteLine(String lineToAdd, Boolean meta = false) {
			//TimeSpan TimeSinceMidnight = DateTime.UtcNow.TimeOfDay;
			//double MSOfDay = Math.Floor(TimeSinceMidnight.TotalMilliseconds);
			//Output = String.Format("{0} {1}", MSOfDay, LineToAdd);
			if (_logFile == null) {
				OpenLog();
			} else {
				if (Path.GetFileName(_logFile.Name) != $"{DateTime.UtcNow:yyyyMMdd-HH}.log") {
					_logStream.WriteLine("{0:O}#Closed.", DateTime.UtcNow);
					_logStream.Close();
					_logFile.Close();
					OpenLog();
				}
			}
			_logStream.WriteLine(meta
									? $"{DateTime.UtcNow:O}#{lineToAdd}"
									: $"{DateTime.UtcNow:O}|{lineToAdd}");
		}
	}
}
