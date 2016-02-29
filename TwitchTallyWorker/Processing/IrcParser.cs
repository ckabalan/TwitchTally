// <copyright file="IrcParser.cs" company="SpectralCoding.com">
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
using NLog;

namespace TwitchTallyWorker.Processing {
	public static class IrcParser {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void Parse(String message, DateTime dateTime) {
			Logger.Trace("Incomming Message:{0}", message);
			// To simulate load so that we don't empty the queue instantly.
			//Thread.Sleep(10);
		}
	}
}
