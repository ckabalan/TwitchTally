using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TwitchTally.Logging;
using TwitchTallyShared;

namespace TwitchTally.IRC {
	public class Server {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private String m_ServerHost;
		private int m_ServerPort;
		private String m_ServerPass;
		private String m_RealName;
		private String m_Nick;
		private String m_AltNick;
		private ServerComm m_ServerComm;
		private List<String> m_ChannelList;

		public String Hostname { get { return m_ServerHost; } set { m_ServerHost = value; } }
		public int Port { get { return m_ServerPort; } set { m_ServerPort = value; } }
		public String Pass { get { return m_ServerPass; } set { m_ServerPass = value; } }
		public String RealName { get { return m_RealName; } set { m_RealName = value; } }
		public String Nick { get { return m_Nick; } set { m_Nick = value; } }
		public String AltNick { get { return m_AltNick; } set { m_AltNick = value; } }
		public List<String> ChannelList { get { return m_ChannelList; } set { m_ChannelList = value; } }

		public Server() {
			
		}

		public void Connect() {
			Logger.Info("Connecting to {0}:{1} as {2}...", Hostname, Port, Nick);
			m_ServerComm = new ServerComm();
			m_ServerComm.StartClient(this);
		}

		public void ParseRawLine(String LineToParse) {
			Logger.Trace("Incomming Data: {0}", LineToParse);
			IRCLog.WriteLine(LineToParse);
			if (LineToParse.Substring(0, 1) == ":") {
				LineToParse = LineToParse.Substring(1);
				String[] ParameterSplit = LineToParse.Split(" ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);
				String Sender = ParameterSplit[0];
				String Command = ParameterSplit[1];
				String Parameters = ParameterSplit[2];
				// Even though we've logged it, we still need to send it down
				// the line for stuff like PING, CTCP, joining channels, etc.
				Parse(Sender, Command, Parameters);
			} else {
				String[] Explode = LineToParse.Split(" ".ToCharArray());
				switch (Explode[0].ToUpper()) {
					case "PING":
						m_ServerComm.Send("PONG " + Explode[1]);
						break;
				}
			}
		}

		public void Parse(String Sender, String Command, String Parameters) {
			//Logger.Trace("   Sender: {0}", Sender);
			//Logger.Trace("   Command: {0}", Command);
			//Logger.Trace("   Parameters: {0}", Parameters);
			String[] ParamSplit = Parameters.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			switch (Command.ToUpper()) {
				case "376":
					// RAW: 376 - RPL_ENDOFMOTD - ":End of /MOTD command"
					Logger.Info("Logged in.");
					JoinChannels();
					break;
				case "353":
					// 353 - RPL_NAMREPLY - "<channel> :[[@|+]<nick> [[@|+]<nick> [...]]]"
					break;
				case "433":
					//433 - ERR_NICKNAMEINUSE - "<nick> :Nickname is already in use"
					Logger.Info("Nick already in use. Attempting change to {0}.", m_AltNick);
					Send("NICK " + m_AltNick);
					m_Nick = m_AltNick;
					break;
				case "470":
					// Channel Forward
					// :adams.freenode.net 470 SomethingKewl #windows ##windows :Forwarding to another channel
					//Channel OldChannel;
					//if (Channels.ContainsKey(ParamSplit[1])) {
					//	OldChannel = Channels[ParamSplit[1]];
					//	OldChannel.Name = ParamSplit[2];
					//	// Should we really remove the old channel? Does it hurt?
					//	//m_Channels.Remove(ParamSplit[1]);
					//	Channels.Add(OldChannel.Name, OldChannel);
					//	// TODO: add code here to check for old channel and rename it.
					//} else {
					//	// Conceivably this could happen if you were forcejoined to a channel which then got moved.
					//	OldChannel = new Channel(this);
					//	OldChannel.Name = ParamSplit[2];
					//	OldChannel.StatsEnabled = true;
					//	throw new Exception("This should never happen. How is this happening? Case 470: Else");
					//}
					break;
				case "JOIN":
					if (ParamSplit[0].Contains(":")) {
						// Fix because some IRCds send "JOIN :#channel" instead of "JOIN #channel"
						ParamSplit[0] = ParamSplit[0].Substring(1);
					}
					//Channels[ParamSplit[0]].Join(Sender);
					break;
				case "PART":
					if (ParamSplit.Length >= 2) {
						string PartMsg = Parameters.Substring(Parameters.IndexOf(":") + 1);
						if (PartMsg.Length == 0) {
							//Channels[ParamSplit[0]].Part(Sender, String.Empty);
						} else {
							if ((PartMsg.Substring(0, 1) == "\"") && (PartMsg.Substring(PartMsg.Length - 1, 1) == "\"")) {
								PartMsg = PartMsg.Substring(1, PartMsg.Length - 2);
							}
						}
						//Channels[ParamSplit[0]].Part(Sender, PartMsg);
					} else {
						//Channels[ParamSplit[0]].Part(Sender, String.Empty);
					}
					break;
				case "KICK":
					//Channels[ParamSplit[0]].Kick(Sender, ParamSplit[1], Functions.CombineAfterIndex(ParamSplit, " ", 2).Substring(1));
					break;
				case "INVITE":
					// TODO: Not sure how we want to handle this.
					break;
				case "NICK":
					if (IRCFunctions.GetNickFromHostString(Sender) == m_Nick) {
						m_Nick = Parameters.Substring(1);
					}
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
					if (ParamSplit[0].Substring(0, 1) == "#") {
						// Is a channel mode
						//Channels[ParamSplit[0]].Mode(Sender, Functions.CombineAfterIndex(ParamSplit, " ", 1));
					} else {
						// Is not going to a channel. Probably me?
					}
					break;
				case "PRIVMSG":
					String MsgText = Parameters.Substring(Parameters.IndexOf(":") + 1);
					if (ParamSplit[0].Substring(0, 1) == "#") {
						// Is going to a channel
						//if (MsgText.Substring(0, 1) == "\x1") {
						//	// If this is a special PRIVMSG, like an action or CTCP
						//	MsgText = MsgText.Substring(1, MsgText.Length - 2);
						//	String[] PrivMsgSplit = MsgText.Split(" ".ToCharArray(), 2);
						//	switch (PrivMsgSplit[0].ToUpper()) {
						//		case "ACTION":
						//			//Channels[ParamSplit[0]].Action(Sender, PrivMsgSplit[1]);
						//			break;
						//			// Maybe other stuff goes here like channel wide CTCPs?
						//	}
						//} else {
						//	// If this is just a normal PRIVMSG.
						//	//Channels[ParamSplit[0]].Message(Sender, MsgText);
						//}
					} else {
						// Is not going to a channel. Probably just me?
						if (MsgText.Substring(0, 1) == "\x1") {
							// If this is a special PRIVMSG, like an action or CTCP
							MsgText = MsgText.Substring(1, MsgText.Length - 2);
							String[] PrivMsgSplit = MsgText.Split(" ".ToCharArray(), 2);
							switch (PrivMsgSplit[0].ToUpper()) {
								case "ACTION":
									// Not sure what to do here...
									break;
								case "VERSION":
									Send(IRCFunctions.CTCPVersionReply(IRCFunctions.GetNickFromHostString(Sender)));
									break;
								case "TIME":
									Send(IRCFunctions.CTCPTimeReply(IRCFunctions.GetNickFromHostString(Sender)));
									break;
								case "PING":
									Send(IRCFunctions.CTCPPingReply(IRCFunctions.GetNickFromHostString(Sender), PrivMsgSplit[1]));
									break;
							}
						} else {
							// Private Message directly to me.
							//String[] MsgSplitPrv = MsgText.Split(" ".ToCharArray());
							//BotCommands.HandlePM(Sender, MsgSplitPrv);
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

		public bool Send(string i_DataToSend) {
			Logger.Trace("Outgoing Data: {0}", i_DataToSend);
			return m_ServerComm.Send(i_DataToSend);
		}

		public void JoinChannels() {
			Logger.Info("Channel List: {0}", Properties.Settings.Default.ChannelList);
			String[] channelSplit = Properties.Settings.Default.ChannelList.Split(',');
			foreach (String curChannel in channelSplit) {
				Logger.Info("Joining Channel: #{0}", curChannel);
				Send(IRCFunctions.Join('#' + curChannel));
			}
		}
	}
}
