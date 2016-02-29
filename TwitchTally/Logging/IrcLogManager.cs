// <copyright file="IrcLogManager.cs" company="SpectralCoding.com">
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using NLog;
using TwitchTally.Queueing;

namespace TwitchTally.Logging {
	public static class IrcLogManager {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static List<FileInfo> List(String wildcard) {
			String[] filePathList = Directory.GetFiles(Properties.Settings.Default.LogDirectory, wildcard);
			return filePathList.Select(curPath => new FileInfo(curPath)).ToList();
		}

		public static void Replay(String fileName) {
			Logger.Info("Replaying Chat Log: {0}", new FileInfo(fileName).Name);
			Int64 ircCounter = 0;
			Int64 metaCounter = 0;
			Stopwatch stopwatch;
			using (StreamReader logReader = File.OpenText(fileName)) {
				String curLine;
				stopwatch = Stopwatch.StartNew();
				while ((curLine = logReader.ReadLine()) != null) {
					if (curLine.Length >= 29) {
						if (curLine.Substring(28, 1) == "|") {
							// Do we want to change this to QueueLog just in case we do something
							// special later?
							OutgoingQueue.QueueRaw(curLine);
							ircCounter++;
						} else if (curLine.Substring(28, 1) == "#") {
							// Do we do anything here?
							metaCounter++;
						}
					}
				}
				stopwatch.Stop();
			}
			Int64 totalLines = ircCounter + metaCounter;
			Double speed = (Double)totalLines / stopwatch.ElapsedMilliseconds * 1000.0;
			Logger.Info("\tDone! Took {0:n0} milliseconds.", stopwatch.ElapsedMilliseconds);
			Logger.Info("\t   IRC Lines: {0:n0}", ircCounter);
			Logger.Info("\t  Meta Lines: {0:n0}", metaCounter);
			Logger.Info("\t       Speed: {0:n0} Lines/Sec", speed);
		}
	}
}
