using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TwitchTally.Logging;
using TwitchTallyShared;

namespace TwitchTally.IRC {
	public class Server {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private ServerComm _serverComm;
		private readonly BlockingCollection<Action> _sendQueue = new BlockingCollection<Action>();
		private Int32 _sendRate;

		public String Hostname { get; set; } = String.Empty;
		public Int32 Port { get; set; } = -1;
		public String Pass { get; set; } = String.Empty;
		public String RealName { get; set; } = String.Empty;
		public String Nick { get; set; } = String.Empty;
		public String AltNick { get; set; } = String.Empty;
		public List<String> ChannelList { get; set; } = new List<String>();
		public String ExtendedUser { get; set; } = String.Empty;

		public void Connect() {
			Logger.Info("Connecting to {0}:{1} as {2}...", Hostname, Port, Nick);
			ExtendedUser = String.Format("{0}!{0}@{0}.tmi.twitch.tv", Nick.ToLower());
            _serverComm = new ServerComm();
			_serverComm.StartClient(this);
		}

		public void ParseRawLine(String lineToParse) {
			Logger.Trace("Incomming Data: {0}", lineToParse);
			IrcLog.WriteLine(lineToParse);
			if (lineToParse.Substring(0, 1) == ":") {
				lineToParse = lineToParse.Substring(1);
				String[] parameterSplit = lineToParse.Split(" ".ToCharArray(), 3, StringSplitOptions.RemoveEmptyEntries);
				String sender = parameterSplit[0];
				String command = parameterSplit[1];
				String parameters = parameterSplit[2];
				// Even though we've logged it, we still need to send it down
				// the line for stuff like PING, CTCP, joining channels, etc.
				Parse(sender, command, parameters);
			} else {
				String[] explode = lineToParse.Split(" ".ToCharArray());
				switch (explode[0].ToUpper()) {
					case "PING":
						_serverComm.Send("PONG " + explode[1]);
						break;
				}
			}
		}

		public void Parse(String sender, String command, String parameters) {
			//Logger.Trace("   Sender: {0}", Sender);
			//Logger.Trace("   Command: {0}", Command);
			//Logger.Trace("   Parameters: {0}", Parameters);
			String[] paramSplit = parameters.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			switch (command.ToUpper()) {
				case "376":
					// RAW: 376 - RPL_ENDOFMOTD - ":End of /MOTD command"
					Logger.Info("Logged in. Negotiating Capabilities...");
					// Query capabilities for later registration
					QueueSend(IrcFunctions.CapabilityLs());
					break;
				case "353":
					// 353 - RPL_NAMREPLY - "<channel> :[[@|+]<nick> [[@|+]<nick> [...]]]"
					break;
				case "366":
					// 366 - RPL_ENDOFNAMES - "<channel> :End of /NAMES list"
					break;
				case "433":
					//433 - ERR_NICKNAMEINUSE - "<nick> :Nickname is already in use"
					Logger.Info("Nick already in use. Attempting change to {0}.", Nick);
					QueueSend("NICK " + AltNick);
					Nick = AltNick;
					ExtendedUser = String.Format("{0}!{0}@{0}.tmi.twitch.tv", Nick.ToLower());
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
				case "CAP":
					// See IRCv3 Specification:
					//    http://ircv3.net/specs/core/capability-negotiation-3.1.html
					//    http://ircv3.net/specs/core/capability-negotiation-3.2.html
					// Note: This may not be 100% compliant, but it works for Twitch.tv.
					if (paramSplit[0] == "*") {
						// Not sure what the * denotes
						switch (paramSplit[1].ToUpper()) {
							case "LS":
								// Response to LS (list of capabilities supported by the server)
								String capabilities = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
								Logger.Debug("Requesting Capabilities: {0}", capabilities);
								QueueSend(IrcFunctions.CapabilityReq(capabilities));
								break;
							case "LIST":
								// Response to LIST (list of active capabilities)
								break;
							case "ACK":
								// Response to REQ command (approved).
								// Would send "CAP END" here, but twitch doesn't respond.
								Logger.Info("Successfully Negotiated Capabilities: {0}", parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1));
								JoinChannels();
                                break;
							case "NAK":
								// Response to REQ command (rejected).
								break;
							case "END":
								// Response to END command.
								break;
						}
					}
                    break;
				case "JOIN":
					if (paramSplit[0].Contains(":")) {
						// Fix because some IRCds send "JOIN :#channel" instead of "JOIN #channel"
						paramSplit[0] = paramSplit[0].Substring(1);
					}
					if (sender.ToLower() == ExtendedUser) {
						ChannelList.Add(paramSplit[0].ToLower());
					}
					break;
				case "PART":
					if (paramSplit.Length >= 2) {
						string partMsg = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
						if (partMsg.Length == 0) {
							//Channels[ParamSplit[0]].Part(Sender, String.Empty);
						} else {
							if ((partMsg.Substring(0, 1) == "\"") && (partMsg.Substring(partMsg.Length - 1, 1) == "\"")) {
								// ReSharper disable once RedundantAssignment
								partMsg = partMsg.Substring(1, partMsg.Length - 2);
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
					if (IrcFunctions.GetNickFromHostString(sender) == Nick) {
						Nick = parameters.Substring(1);
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
					if (paramSplit[0].Substring(0, 1) == "#") {
						// Is a channel mode
						//Channels[ParamSplit[0]].Mode(Sender, Functions.CombineAfterIndex(ParamSplit, " ", 1));
					} else {
						// Is not going to a channel. Probably me?
					}
					break;
				case "PRIVMSG":
					String msgText = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
					if (paramSplit[0].Substring(0, 1) == "#") {
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
						if (msgText.Substring(0, 1) == "\x1") {
							// If this is a special PRIVMSG, like an action or CTCP
							msgText = msgText.Substring(1, msgText.Length - 2);
							String[] privMsgSplit = msgText.Split(" ".ToCharArray(), 2);
							switch (privMsgSplit[0].ToUpper()) {
								case "ACTION":
									// Not sure what to do here...
									break;
								case "VERSION":
									QueueSend(IrcFunctions.CtcpVersionReply(IrcFunctions.GetNickFromHostString(sender)));
									break;
								case "TIME":
									QueueSend(IrcFunctions.CtcpTimeReply(IrcFunctions.GetNickFromHostString(sender)));
									break;
								case "PING":
									QueueSend(IrcFunctions.CtcpPingReply(IrcFunctions.GetNickFromHostString(sender), privMsgSplit[1]));
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

		public void QueueSend(string dataToSend) {
			// Reference:
			//    http://help.twitch.tv/customer/portal/articles/1302780-twitch-irc
			//    If you send more than 20 commands or messages to the server within a 30 second period, you will get
			//    locked out for 8 hours automatically. These are not lifted so please be careful when working with IRC!
			Logger.Trace("Queueing Send: {0}", dataToSend);
			_sendQueue.Add(() => {
				// Increase the send counter by 1
				Interlocked.Increment(ref _sendRate);
				// Send the data (for real)
				Send(dataToSend);
				// Schedule the send counter to be decreased after SendLimitTimeMS ms.
				Functions.PauseAndExecute(() => { Interlocked.Decrement(ref _sendRate); }, Properties.Settings.Default.SendLimitTimeMS);
			});
		}

		public void Send(string dataToSend) {
			// No queuing here, just override it.
			Logger.Trace("Outgoing Data: {0}", dataToSend);
			_serverComm.Send(dataToSend);
		}

		public void JoinChannels() {
			Logger.Info("Channel List: {0}", Properties.Settings.Default.ChannelList);
			String[] channelSplit = Properties.Settings.Default.ChannelList.Split(',');
			foreach (String curChannel in channelSplit) {
				Logger.Info("Joining Channel: #{0}", curChannel);
				QueueSend(IrcFunctions.Join('#' + curChannel));
			}
		}

		public void StartSendQueueConsumer() {
			Logger.Debug("Starting Send Queue Consumer Thread (Send Throttling = {0} per {1}ms)", Properties.Settings.Default.SendLimitNum, Properties.Settings.Default.SendLimitTimeMS);
			Task.Factory.StartNew(() => {
				while (true) {
					// Make sure we're still connected.
					if (_serverComm.Connected) {
						// Make sure we're not passing SendLimitNum.
						int tempSendRate = Interlocked.CompareExchange(ref _sendRate, 0, 0);
						if (tempSendRate < Properties.Settings.Default.SendLimitNum) {
							Logger.Trace("Triggering Send at SR {0} ({1} in Queue).", tempSendRate, _sendQueue.Count);
							// Remove the action from the queue into curAction;
							Action curAction = _sendQueue.Take();
							// Execute the queued action
							curAction();
						}
					}
				}
			});
		}
	}
}
