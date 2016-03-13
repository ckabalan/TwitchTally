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
using System.Collections.Generic;
using NLog;
using TwitchTallyShared;

namespace TwitchTallyWorker.Processing {
	public static class IrcParser {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public static void Parse(String message, DateTime dateTime) {
			//Logger.Trace($"Incomming Message:{message}");
			// Sample Data:
			//    PING :tmi.twitch.tv
			//    :jtv MODE #sodapoppin -o zondagshow
			//    :tmi.twitch.tv CLEARCHAT #forsenlol :buddyger
			//    :knastomten!knastomten@knastomten.tmi.twitch.tv JOIN #sodapoppin
			//    :agarkingalex!agarkingalex@agarkingalex.tmi.twitch.tv PART #riotgames
			//    @color=#FF0000;display-name=Kevinmetalmusic;emotes=41:0-7,9-16;mod=0;room-id=105458682;subscriber=0;turbo=0;user-id=82348179;user-type= :kevinmetalmusic!kevinmetalmusic@kevinmetalmusic.tmi.twitch.tv PRIVMSG #bobross :Kreygasm Kreygasm
			// Details:
			//    https://github.com/justintv/Twitch-API/blob/master/IRC.md
			Dictionary<String, String> options = new Dictionary<String, String>();
			// Cut out and parse the IRCv3 extended options and re-set message to just the static IRC content.
			if (message.Substring(0, 1) == "@") {
				String tempExtended = message.Substring(1, message.IndexOf(" :", StringComparison.Ordinal)).Trim();
				foreach (String curPair in tempExtended.Split(';')) {
					String[] curSplit = curPair.Split('=');
					options.Add(curSplit[0], curSplit[1]);
				}
				message = message.Substring(tempExtended.Length + 2).Trim();
			}
			if (message.Substring(0, 1) == ":") {
				String[] parameterSplit = message.Split(" ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);
				String sender = parameterSplit[0].Substring(1);
				String command = parameterSplit[1];
				String parameters = parameterSplit[2];
				// Even though we've logged it, we still need to send it down
				// the line for stuff like PING, CTCP, joining channels, etc.
				Process(sender, command, parameters, dateTime, options);
			}
		}

		public static void Process(String sender, String command, String parameters, DateTime dateTime,
			Dictionary<String, String> options) {
			String[] paramSplit = parameters.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			switch (command.ToUpper()) {
				case "376":
					// RAW: 376 - RPL_ENDOFMOTD - ":End of /MOTD command"
					break;
				case "353":
					// 353 - RPL_NAMREPLY - "<channel> :[[@|+]<nick> [[@|+]<nick> [...]]]"
					break;
				case "366":
					// 366 - RPL_ENDOFNAMES - "<channel> :End of /NAMES list"
					break;
				case "433":
					//433 - ERR_NICKNAMEINUSE - "<nick> :Nickname is already in use"
					break;
				case "470":
					// :adams.freenode.net 470 SomethingKewl #windows ##windows :Forwarding to another channel
					break;
				case "CAP":
					// See IRCv3 Specification:
					//    http://ircv3.net/specs/core/capability-negotiation-3.1.html
					//    http://ircv3.net/specs/core/capability-negotiation-3.2.html
					// Note: This may not be 100% compliant, but it works for Twitch.tv.
					break;
				case "JOIN":
					// We don't care about joins/parts at this point
					//if (paramSplit[0].Contains(":")) {
					//	// Fix because some IRCds send "JOIN :#channel" instead of "JOIN #channel"
					//	paramSplit[0] = paramSplit[0].Substring(1);
					//}
					//LineParser.Join(dateTime, options, paramSplit[0], IrcFunctions.GetNickFromHostString(sender));
					break;
				case "PART":
					// We don't care about joins/parts at this point
					//if (paramSplit.Length >= 2) {
					//	String partMsg = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
					//	// I don't remember what the logic is behind most of this block. Investigate later.
					//	if (partMsg.Length == 0) {
					//		//Channels[ParamSplit[0]].Part(Sender, String.Empty);
					//	} else {
					//		if ((partMsg.Substring(0, 1) == "\"") && (partMsg.Substring(partMsg.Length - 1, 1) == "\"")) {
					//			// ReSharper disable once RedundantAssignment
					//			partMsg = partMsg.Substring(1, partMsg.Length - 2);
					//		}
					//	}
					//	LineParser.Part(dateTime, options, paramSplit[0], IrcFunctions.GetNickFromHostString(sender), partMsg);
					//}
					break;
				case "KICK":
					//Channels[ParamSplit[0]].Kick(Sender, ParamSplit[1], Functions.CombineAfterIndex(ParamSplit, " ", 2).Substring(1));
					break;
				case "INVITE":
					// TODO: Not sure how we want to handle this.
					break;
				case "NICK":
					//foreach (KeyValuePair<string, Channel> CurKVP in Channels) {
					//	Channels[CurKVP.Key].Nick(Sender, Parameters.Substring(1));
					//}
					//BotCommands.CheckAdminChange(Sender, Parameters.Substring(1));
					break;
				case "QUIT":
					//foreach (KeyValuePair<string, Channel> CurKVP in Channels) {
					//	Channels[CurKVP.Key].Quit(Sender, Parameters.Substring(1));
					//}
					break;
				case "MODE":
					if (paramSplit[0].Substring(0, 1) == "#") {
						// Is a channel mode
						//Channels[ParamSplit[0]].Mode(Sender, Functions.CombineAfterIndex(ParamSplit, " ", 1));
					}
					break;
				case "PRIVMSG":
					String msgText = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
					if (paramSplit[0].Substring(0, 1) == "#") {
						// Is going to a channel
						if (msgText.Substring(0, 1) == "\x1") {
							// If this is a special PRIVMSG, like an action or CTCP
							msgText = msgText.Substring(1, msgText.Length - 2);
							String[] privMsgSplit = msgText.Split(" ".ToCharArray(), 2);
							switch (privMsgSplit[0].ToUpper()) {
								case "ACTION":
									//Channels[ParamSplit[0]].Action(Sender, PrivMsgSplit[1]);
									LineParser.Action(dateTime, options, paramSplit[0], IrcFunctions.GetNickFromHostString(sender), privMsgSplit[1]);
									break;
							}
						} else {
							// If this is just a normal PRIVMSG.
							LineParser.Message(dateTime, options, paramSplit[0], IrcFunctions.GetNickFromHostString(sender), msgText);
						}
					} else {
						// Is not going to a channel. Probably just me?
						if (msgText.Substring(0, 1) == "\x1") {
							// If this is a special PRIVMSG, like an action or CTCP
							msgText = msgText.Substring(1, msgText.Length - 2);
							String[] privMsgSplit = msgText.Split(" ".ToCharArray(), 2);
							switch (privMsgSplit[0].ToUpper()) {
								case "ACTION":
									// Not sure what to do here...
									break;
								case "VERSION":
									//QueueSend(IrcFunctions.CtcpVersionReply(IrcFunctions.GetNickFromHostString(sender)));
									break;
								case "TIME":
									//QueueSend(IrcFunctions.CtcpTimeReply(IrcFunctions.GetNickFromHostString(sender)));
									break;
								case "PING":
									//QueueSend(IrcFunctions.CtcpPingReply(IrcFunctions.GetNickFromHostString(sender), privMsgSplit[1]));
									break;
							}
						}
					}
					break;
				case "NOTICE":
					// Needed for NickServ stuff
					//string[] MsgSplitNtc = Parameters.Substring(Parameters.IndexOf(":") + 1).Split(" ".ToCharArray());
					//BotCommands.HandleNotice(Sender, MsgSplitNtc);
					break;
			}
		}
	}
}
