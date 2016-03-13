// <copyright file="Program.cs" company="SpectralCoding.com">
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
using System.IO;
using System.Linq;
using System.Reflection;
using NLog;
using TwitchTally.IRC;
using TwitchTally.Logging;
using TwitchTally.Queueing;
using TwitchTallyShared;

namespace TwitchTally {
	class Program {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private static Server _mainConnection;

		/// <summary>
		/// The main entry function for the application.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		// ReSharper disable once UnusedParameter.Local
		static void Main(String[] args) {
			//Console.SetBufferSize(250, 20000);
			//Console.SetWindowSize(250, 50);
			Logger.Info("TwitchTally v" + Assembly.GetExecutingAssembly().GetName().Version + " started.");
			OutgoingQueue.Initialize();
			//ConnectToIrc();
			Boolean exitApplication = false;
			while (!exitApplication) {
				Console.Write("> ");
				exitApplication = ParseCommand(Console.ReadLine());
			}
			Close();
			Environment.Exit(1);
		}

		static void ConnectToIrc() {
			_mainConnection = new Server {
				Hostname = Properties.Settings.Default.IRCServer,
				Port = Properties.Settings.Default.IRCPort,
				Nick = Properties.Settings.Default.IRCUsername,
				AltNick = Properties.Settings.Default.IRCUsernameAlt,
				Pass = Properties.Settings.Default.IRCPassword
			};
			_mainConnection.Connect();
		}

		static void Close() {
			_mainConnection.Disconnect();
			IrcLog.CloseLog(DateTime.UtcNow);
		}

		static Boolean ParseCommand(String input) {
			Logger.Info("Input: {0}", input);
			String[] cmdSplit = input.Split(' ');
			switch (cmdSplit[0].ToUpper()) {
				case "EXIT":
					return true;
				case "LOG":
					if (cmdSplit.Length > 2) {
						switch (cmdSplit[1].ToUpper()) {
							case "LIST":
								Logger.Info("Log Directory: {0}", Properties.Settings.Default.LogDirectory);
								Logger.Info("");
								Logger.Info("File Filter: {0}", cmdSplit[2]);
								List<FileInfo> logList = IrcLogManager.List(cmdSplit[2]);
								Logger.Info("{0,-20} {1,-7} {2,7} {3,-20}", "Name", "Lines", "Size", "Last Modified");
								foreach (FileInfo curFileInfo in logList) {
									Logger.Info("{0,-20} {1,-7:n0} {2,7} {3,-20}", curFileInfo.Name, File.ReadLines(curFileInfo.FullName).Count(),
												Functions.BytesToHumanReadable(curFileInfo.Length, 0), curFileInfo.LastWriteTime);
								}
								// Do things here.
								break;
							case "REPLAY":
								List<FileInfo> replayList = IrcLogManager.List(cmdSplit[2]);
								foreach (FileInfo curFileInfo in replayList) {
									IrcLogManager.Replay(curFileInfo.FullName);
								}
								break;
						}
					} else {
						Logger.Info(" Invalid command. See HELP LOG for usage information.");
					}
					break;
				case "HELP":
					Logger.Info("TwitchTally Help:");
					Logger.Info("");
					Logger.Info("Prefix any command with HELP for in-depth usage information.");
					Logger.Info("");
					if (cmdSplit.Length > 1) {
						switch (cmdSplit[1].ToUpper()) {
							case "LOG":
								if (cmdSplit.Length > 2) {
									switch (cmdSplit[2].ToUpper()) {
										case "LIST":
											Logger.Info("Lists all log files matching (PATTERN). Includes filename, size,");
											Logger.Info("and format.");
											Logger.Info("");
											Logger.Info(" Usage: LOG LIST (PATTERN)");
											Logger.Info("   PATTERN - Any valid path wildcard (IE: 201602*.log)");
											break;
										case "REPLAY":
											Logger.Info("Reads and plays logs matching (PATTERN) to active worker nodes. This");
											Logger.Info("is generally only good to do after clearing the database.");
											Logger.Info("");
											Logger.Info(" Usage: LOG REPLAY (PATTERN)");
											Logger.Info("   PATTERN - Any valid path wildcard (IE: 201602*.log)");
											break;
									}
								} else {
									Logger.Info("    LOG LIST - Lists logs and current format.");
									Logger.Info("  LOG REPLAY - Replays logs to the worker nodes.");
								}
								break;
						}
					} else {
						Logger.Info("     HELP - Displays this list of commands.");
						Logger.Info("      LOG - Allows IRC log replay and manipulation.");
					}
					Logger.Info("");
					break;
			}
			return false;
		}
	}
}
