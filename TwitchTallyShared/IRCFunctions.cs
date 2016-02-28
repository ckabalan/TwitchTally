using System;
using System.Collections.Generic;
using System.Reflection;

namespace TwitchTallyShared {
	public static class IrcFunctions {

		public static string Join(string channel) {
			return "JOIN " + channel;
		}

		public static string Join(string channel, string password) {
			if (password == "") {
				return "JOIN " + channel;
			} else {
				return "JOIN " + channel + " " + password;
			}
		}

		public static string PrivMsg(string to, string message) {
			return "PRIVMSG " + to + " :" + message;
		}

		public static string CapabilityLs() {
			return "CAP LS";
		}

		public static string CapabilityReq(string listOfCabilities) {
			return "CAP REQ :" + listOfCabilities;
		}

		public static string CapabilityReq(List<string> listOfCabilities) {
			return "CAP REQ :" + String.Join(" ", listOfCabilities);
		}

		public static string CapabilityEnd() {
			return "CAP END";
		}

		public static string CtcpVersionReply(string username) {
			return "NOTICE " + username + " :\x01VERSION Twitch Tally Bot v"
					+ Assembly.GetExecutingAssembly().GetName().Version + "\x01";
		}

		public static string CtcpTimeReply(string username) {
			// Not UTC because it should reflect local time.
			return "NOTICE " + username + " :\x01TIME " + DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy") + "\x01";
		}

		public static string CtcpPingReply(string username, string timestamp) {
			return "NOTICE " + username + " :\x01PING " + timestamp + "\x01";
		}


		public static string GetNickFromHostString(string hostString) {
			if (hostString.Contains("!") && hostString.Contains("@")) {
				return hostString.Split("!".ToCharArray())[0];
			} else {
				return hostString;
			}

		}

		public static Sender ParseSender(string senderStr) {
			Sender tempSender = new Sender {SenderStr = senderStr};
			if (senderStr.Contains("!") && senderStr.Contains("@")) {
				string[] tempSenderStr = senderStr.Split('!');
				tempSender.Nick = tempSenderStr[0];
				tempSenderStr = tempSenderStr[1].Split('@');
				tempSender.Ident = tempSenderStr[0];
				tempSender.Host = tempSenderStr[1];
			} else {
				tempSender.Server = senderStr;
			}
			return tempSender;
		}

		public static string GetUserFromHostString(string hostString, bool removeTilde = true) {
			if (hostString.Contains("!") && hostString.Contains("@")) {
				string[] tempSplit = hostString.Split("!".ToCharArray());
				tempSplit = tempSplit[1].Split("@".ToCharArray());
				if (removeTilde) {
					if (tempSplit[0].Substring(0, 1) == "~") { tempSplit[0] = tempSplit[0].Substring(1); }
				}
				return tempSplit[0];
			} else {
				return string.Empty;
			}
		}

		public static string GetHostFromHostString(string hostString) {
			if (hostString.Contains("!") && hostString.Contains("@")) {

				return hostString.Split("@".ToCharArray())[1];
			} else {
				return string.Empty;
			}
		}
	}
}
