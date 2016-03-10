// <copyright file="Server.cs" company="SpectralCoding.com">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TwitchTally.Logging;
using TwitchTallyShared;
using TwitchTally.Queueing;
using TwitchTally.TwitchAPI;
using Timer = System.Threading.Timer;

namespace TwitchTally.IRC {
	public class Server {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		//private ConcurrentBag<String> _channels = new ConcurrentBag<String>();
		private readonly ConcurrentDictionary<String, Int32> _channelStatus = new ConcurrentDictionary<String, Int32>();
		private readonly BlockingCollection<String>[] _sendQueue;
		private readonly BlockingCollection<String> _sendQueueHigh = new BlockingCollection<String>();
		//private readonly BlockingCollection<Action> _sendQueue = new BlockingCollection<Action>();
		private readonly BlockingCollection<String> _sendQueueNorm = new BlockingCollection<String>();
		private Timer _joinTimer;

		private Int32 _sendRate;
		private ServerComm _serverComm;

		public Server() {
			// Order is important as BlockingCollection<T>.TakeFromAny() will use the order of array elements.
			_sendQueue = new[] {_sendQueueHigh, _sendQueueNorm};
		}

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

		public void ParseRawLine(String lineToParse, DateTime dateTime) {
			Logger.Trace("Incomming Data: {0}", lineToParse);
			IrcLog.WriteLine(lineToParse, dateTime);
			//OutgoingQueue.QueueIrc(lineToParse, dateTime);
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
						QueueSend("PONG " + explode[1], true);
						//_serverComm.Send("PONG " + explode[1]);
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
					QueueSend(IrcFunctions.CapabilityLs(), true);
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
					QueueSend("NICK " + AltNick, true);
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
								if (Properties.Settings.Default.TwitchMembershipCapabilityEnable == false) {
									Logger.Debug("Ignoring twitch.tv/membership IRCv3 capability");
									capabilities = capabilities.Replace("twitch.tv/membership", "").Trim();
								}
								Logger.Debug("Requesting Capabilities: {0}", capabilities);
								QueueSend(IrcFunctions.CapabilityReq(capabilities), true);
								break;
							case "LIST":
								// Response to LIST (list of active capabilities)
								break;
							case "ACK":
								// Response to REQ command (approved).
								// Would send "CAP END" here, but twitch doesn't respond.
								Logger.Info("Successfully Negotiated Capabilities: {0}",
											parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1));
								JoinOrPartChannels();
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
						// Add it as zero, or if it exists, set it to 0. Substring portion cuts off #
						_channelStatus.AddOrUpdate(paramSplit[0].ToLower().Substring(1), 0, (name, count) => 0);
						//_channels.Add(paramSplit[0].ToLower());
						Logger.Debug("Channel List: {0}", String.Join(", ", _channelStatus.Keys));
					}
					break;
				case "PART":
					if (paramSplit.Length >= 2) {
						String partMsg = parameters.Substring(parameters.IndexOf(":", StringComparison.Ordinal) + 1);
						if (partMsg.Length == 0) {
							//Channels[ParamSplit[0]].Part(Sender, String.Empty);
						} else {
							if ((partMsg.Substring(0, 1) == "\"") && (partMsg.Substring(partMsg.Length - 1, 1) == "\"")) {
								// ReSharper disable once RedundantAssignment
								partMsg = partMsg.Substring(1, partMsg.Length - 2);
							}
						}
						if (sender.ToLower() == ExtendedUser) {
							Int32 temp;
							_channelStatus.TryRemove(paramSplit[0].ToLower().Substring(1), out temp);
							//_channels.TryTake(paramSplit[0].ToLower());
							Logger.Debug("Channel List: {0}", String.Join(", ", _channelStatus.Keys));
						}
						//Channels[ParamSplit[0]].Part(Sender, PartMsg);
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

		public void QueueSend(String dataToSend, Boolean highPriority = false) {
			if (highPriority) {
				Logger.Trace("Queueing Send (High): {0}", dataToSend);
				_sendQueueHigh.Add(dataToSend);
			} else {
				Logger.Trace("Queueing Send (Norm): {0}", dataToSend);
				_sendQueueNorm.Add(dataToSend);
			}
		}

		public void Send(String dataToSend) {
			// No queuing here, just override it.
			Logger.Trace("Outgoing Data: {0}", dataToSend);
			_serverComm.Send(dataToSend);
		}

		public void JoinOrPartChannels() {
			// Channel Status:
			//    -2 = Queued Part
			//    -1 = Queued Join
			//    0+ = Joined (Value represents strike count)
			List<String> forcedChannels = new List<String>(Properties.Settings.Default.ChannelList.Split(','));
			List<TwitchStream> streams =
				TwitchStreams.GetStreamsByMinViewers(Convert.ToInt32(Properties.Settings.Default.AutojoinViewerMinimum * 0.8));
			if (streams.Count == 0) {
				Logger.Warn("No Twitch.tv streams found meeting viewer minimums. Skipping Join/Part process.");
			} else {
				// Join channels which meet the minimum viewer requirement
				foreach (TwitchStream curStream in streams) {
					if (curStream.Viewers >= Properties.Settings.Default.AutojoinViewerMinimum) {
						// We're above the Viewer Minimum so join
						if (_channelStatus.ContainsKey(curStream.Name.ToLower())) {
							// We have some sort of status for this channel
							Int32 status;
							_channelStatus.TryGetValue(curStream.Name.ToLower(), out status);
							if (status == -2) {
								// We're already set to part this channel
								// We're above the Viewer Minimum so queue a join even though we're already set to part
								Logger.Info($"Stream {curStream.Name} has {curStream.Viewers} viewers. Joining...");
								QueueSend(IrcFunctions.Join('#' + curStream.Name.ToLower()));
								_channelStatus.AddOrUpdate(curStream.Name.ToLower(), -1, (name, count) => -1);
							} else if (status > 0) {
								// We're in the channel, so reset it's strike counter since it it's in the "OK" list
								_channelStatus.AddOrUpdate(curStream.Name.ToLower(), 0, (name, count) => 0);
							}
						} else {
							// We've never done anything with this channel before so queue a join.
							Logger.Info($"Stream {curStream.Name} has {curStream.Viewers} viewers. Joining...");
							QueueSend(IrcFunctions.Join('#' + curStream.Name.ToLower()));
							_channelStatus.AddOrUpdate(curStream.Name.ToLower(), -1, (name, count) => -1);
						}
					}
				}
				foreach (String curChannel in forcedChannels) {
					// This channel is a forced channel
					if (_channelStatus.ContainsKey(curChannel.ToLower())) {
						// We have some sort of status for this channel
						Int32 status;
						_channelStatus.TryGetValue(curChannel.ToLower(), out status);
						if (status == -2) {
							// We're already set to part this channel (this should only happen if we're kicked since we don't volunteer to part forced channels)
							// We're above the Viewer Minimum so join even though we're already set to part
							Logger.Info($"Stream {curChannel} is in the forced list. Joining...");
							QueueSend(IrcFunctions.Join('#' + curChannel.ToLower()));
							_channelStatus.AddOrUpdate(curChannel.ToLower(), -1, (name, count) => -1);
						}
					} else {
						// We've never done anything with this channel before
						Logger.Info($"Stream {curChannel} is in the forced list. Joining...");
						QueueSend(IrcFunctions.Join('#' + curChannel.ToLower()));
						_channelStatus.AddOrUpdate(curChannel.ToLower(), -1, (name, count) => -1);
					}
				}
				// Leave channels which don't meet 80% of the minimum viewer requirement
				foreach (KeyValuePair<String, Int32> curChanKVP in _channelStatus) {
					if (!forcedChannels.Contains(curChanKVP.Key.ToLower())) {
						// If this is not a forced channel
						Int32 findIndex =
							streams.FindIndex(f => String.Equals(f.Name, curChanKVP.Key, StringComparison.CurrentCultureIgnoreCase));
						if (findIndex == -1) {
							// Channel didn't come back in our twitch query (meaning it is offline or below min viewer count)
							if (_channelStatus.ContainsKey(curChanKVP.Key.ToLower())) {
								// We have some sort of status for this channel
								Int32 status;
								_channelStatus.TryGetValue(curChanKVP.Key.ToLower(), out status);
								if (status >= 0) {
									// We're already in the channel (don't give strikes until we're in the channel)
									// Increment the number of strikes
									_channelStatus[curChanKVP.Key.ToLower()]++;
									Logger.Info(
												 $"Stream {curChanKVP.Key} has less than {Properties.Settings.Default.AutojoinViewerMinimum * 0.8} viewers. Strike {_channelStatus[curChanKVP.Key.ToLower()]}/{Properties.Settings.Default.AutojoinStrikeLimit}...");
									if (_channelStatus[curChanKVP.Key.ToLower()] >= Properties.Settings.Default.AutojoinStrikeLimit) {
										// We're at max strikes, queue a leave
										Logger.Info(
													 $"Strike {_channelStatus[curChanKVP.Key.ToLower()]}/{Properties.Settings.Default.AutojoinStrikeLimit} for {curChanKVP.Key}... Leaving...");
										QueueSend(IrcFunctions.Part('#' + curChanKVP.Key.ToLower()));
										_channelStatus.AddOrUpdate(curChanKVP.Key.ToLower(), -2, (name, count) => -2);
									}
								}
							}
						}
					}
				}
			}
			if (_joinTimer == null) {
				Logger.Info($"Starting Channel Maintainer ({Properties.Settings.Default.AutojoinCheckFrequencyMS}ms)");
				_joinTimer = new Timer(_ => JoinOrPartChannels(), null, Properties.Settings.Default.AutojoinCheckFrequencyMS,
										Timeout.Infinite);
			} else {
				_joinTimer.Change(Properties.Settings.Default.AutojoinCheckFrequencyMS, Timeout.Infinite);
			}
		}

		public void StartSendQueueConsumer() {
			// Reference:
			//    http://help.twitch.tv/customer/portal/articles/1302780-twitch-irc
			//    If you send more than 20 commands or messages to the server within a 30 second period, you will get
			//    locked out for 8 hours automatically. These are not lifted so please be careful when working with IRC!
			Logger.Debug(
						 $"Starting Send Queue Consumer Thread (Send Throttling = {Properties.Settings.Default.SendLimitNum} per {Properties.Settings.Default.SendLimitTimeMS}ms)");
			Task.Factory.StartNew(() => {
				while (true) {
					// Make sure we're still connected.
					if (_serverComm.Connected) {
						// Make sure we're not passing SendLimitNum.
						Int32 tempSendRate = Interlocked.CompareExchange(ref _sendRate, 0, 0);
						if (tempSendRate < Properties.Settings.Default.SendLimitNum) {
							Logger.Trace("Triggering Send at SR {0} ({1}T/{2}H/{3}N in Queue).", tempSendRate,
										_sendQueueHigh.Count + _sendQueueNorm.Count, _sendQueueHigh.Count, _sendQueueNorm.Count);
							// Remove the message from the queue into curMsg;
							String curMsg;
							BlockingCollection<String>.TakeFromAny(_sendQueue, out curMsg);
							// Increase the send counter by 1
							Interlocked.Increment(ref _sendRate);
							// Send the data (for real)
							Send(curMsg);
							// Schedule the send counter to be decreased after SendLimitTimeMS ms.
							Functions.PauseAndExecute(() => { Interlocked.Decrement(ref _sendRate); },
													Properties.Settings.Default.SendLimitTimeMS);
						}
					}
				}
				// ReSharper disable once FunctionNeverReturns
			});
		}

		public void Disconnect() {
			Logger.Info("Disconnecting from IRC.");
			_serverComm.CloseConnection();
		}

		public void Disconnected() {
			Logger.Info($"Emptying High Priority Send Queue ({_sendQueueHigh.Count} Messages)...");
			while (_sendQueueHigh.Count > 0) {
				String item;
				_sendQueueHigh.TryTake(out item);
			}
			Logger.Info($"Emptying Normal Priority Send Queue ({_sendQueueNorm.Count} Messages)...");
			while (_sendQueueNorm.Count > 0) {
				String item;
				_sendQueueHigh.TryTake(out item);
			}
			Logger.Debug($"Cleanning Channel List ({_channelStatus.Count} Channels)...");
			_channelStatus.Clear();
			Logger.Info("Waiting 30sec to reconnect...");
			// Wait 30sec and queue again.
			Functions.PauseAndExecute(Connect, 30000);
		}
	}
}
