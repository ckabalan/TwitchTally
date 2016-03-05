// <copyright file="TwitchStreams.cs" company="SpectralCoding.com">
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
using System.Net;
using Newtonsoft.Json.Linq;
using NLog;

namespace TwitchTally.TwitchAPI {
	public static class TwitchStreams {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static List<TwitchStream> GetStreamsByMinViewers(Int32 viewers) {
			try {
				List<TwitchStream> topStreams = new List<TwitchStream>();
				Int32 offset = 0;
				while (true) {
					HttpWebRequest apiRequest =
						(HttpWebRequest)
						WebRequest.Create(@"https://api.twitch.tv/kraken/streams?stream_type=live&limit=100&offset=" + offset);
					apiRequest.Accept = "application/vnd.twitchtv.v3+json";
					HttpWebResponse apiResponse = (HttpWebResponse)apiRequest.GetResponse();
					Stream apiResponseStream = apiResponse.GetResponseStream();
					if (apiResponseStream != null) {
						StreamReader sr = new StreamReader(apiResponseStream);
						JObject test = JObject.Parse(sr.ReadToEnd().Trim());
						foreach (JToken curStream in test["streams"]) {
							if (Convert.ToInt32(curStream["viewers"]) > viewers) {
								TwitchStream temp = new TwitchStream {
									Name = curStream["channel"]["name"].ToString(),
									BroadcasterLanguage = curStream["channel"]["name"].ToString(),
									Language = curStream["channel"]["name"].ToString(),
									Game = curStream["channel"]["name"].ToString(),
									Delay = Convert.ToInt32(curStream["delay"]),
									VideoHeight = Convert.ToInt32(curStream["video_height"]),
									VideoFps = Convert.ToDouble(curStream["average_fps"]),
									Viewers = Convert.ToInt32(curStream["viewers"]),
									IsMature = curStream["channel"]["mature"].ToString().ToUpper() == "TRUE",
									IsDelayed = curStream["channel"]["delay"].ToString().ToUpper() == "TRUE"
								};
								topStreams.Add(temp);
							} else {
								break;
							}
						}
						if (offset >= 500) {
							// Just incase we get to some kind of weird loop.
							break;
						}
						offset += 100;
					} else {
						Logger.Warn("Unable to get response stream. Returning Empty List.");
						return new List<TwitchStream>();
					}
				}
				return topStreams.OrderByDescending(s => s.Viewers).ToList();
			}
			catch (Exception) {
				Logger.Warn("Caught exception trying to process Twitch.tv streams. Returning Empty List.");
				return new List<TwitchStream>();
			}
		}
	}
}
