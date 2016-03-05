// <copyright file="IRCFunctions.cs" company="SpectralCoding.com">
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
using System.Reflection;

namespace TwitchTallyShared {
	public static class IrcFunctions {
		public static String Join(String channel) {
			return "JOIN " + channel;
		}

		public static String Part(String channel) {
			return "PART " + channel;
		}

		public static String Join(String channel, String password) {
			if (password == "") {
				return "JOIN " + channel;
			}
			return "JOIN " + channel + " " + password;
		}

		public static String PrivMsg(String to, String message) {
			return "PRIVMSG " + to + " :" + message;
		}

		public static String CapabilityLs() {
			return "CAP LS";
		}

		public static String CapabilityReq(String listOfCabilities) {
			return "CAP REQ :" + listOfCabilities;
		}

		public static String CapabilityReq(List<String> listOfCabilities) {
			return "CAP REQ :" + String.Join(" ", listOfCabilities);
		}

		public static String CapabilityEnd() {
			return "CAP END";
		}

		public static String CtcpVersionReply(String username) {
			return "NOTICE " + username + " :\x01VERSION Twitch Tally Bot v"
					+ Assembly.GetExecutingAssembly().GetName().Version + "\x01";
		}

		public static String CtcpTimeReply(String username) {
			// Not UTC because it should reflect local time.
			return "NOTICE " + username + " :\x01TIME " + DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy") + "\x01";
		}

		public static String CtcpPingReply(String username, String timestamp) {
			return "NOTICE " + username + " :\x01PING " + timestamp + "\x01";
		}

		public static String GetNickFromHostString(String hostString) {
			if (hostString.Contains("!") && hostString.Contains("@")) {
				return hostString.Split("!".ToCharArray())[0];
			}
			return hostString;
		}

		public static Sender ParseSender(String senderStr) {
			Sender tempSender = new Sender {SenderStr = senderStr};
			if (senderStr.Contains("!") && senderStr.Contains("@")) {
				String[] tempSenderStr = senderStr.Split('!');
				tempSender.Nick = tempSenderStr[0];
				tempSenderStr = tempSenderStr[1].Split('@');
				tempSender.Ident = tempSenderStr[0];
				tempSender.Host = tempSenderStr[1];
			} else {
				tempSender.Server = senderStr;
			}
			return tempSender;
		}

		public static String GetUserFromHostString(String hostString, Boolean removeTilde = true) {
			if (hostString.Contains("!") && hostString.Contains("@")) {
				String[] tempSplit = hostString.Split("!".ToCharArray());
				tempSplit = tempSplit[1].Split("@".ToCharArray());
				if (removeTilde) {
					if (tempSplit[0].Substring(0, 1) == "~") { tempSplit[0] = tempSplit[0].Substring(1); }
				}
				return tempSplit[0];
			}
			return String.Empty;
		}

		public static String GetHostFromHostString(String hostString) {
			if (hostString.Contains("!") && hostString.Contains("@")) {
				return hostString.Split("@".ToCharArray())[1];
			}
			return String.Empty;
		}
	}
}
