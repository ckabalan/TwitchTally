// <copyright file="ServerComm.cs" company="SpectralCoding.com">
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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TwitchTallyShared;
using NLog;

namespace TwitchTally.IRC {
	public class ServerComm {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		private readonly ManualResetEvent _connectDone = new ManualResetEvent(false);
		private readonly ManualResetEvent _sendDone = new ManualResetEvent(false);
		private Socket _clientSock;
		private Server _parentServer;

		public Boolean Connected => _clientSock.Connected;

		public void StartClient(Server parentServer) {
			if ((_clientSock != null) && _clientSock.Connected) {
				OnDisconnect(_clientSock);
			}
			// ReSharper disable once RedundantAssignment
			Int32 i = 0;
			_parentServer = parentServer;
			IPAddress ipAddr;
			if (Int32.TryParse(_parentServer.Hostname.Substring(0, 1), out i)) {
				ipAddr = IPAddress.Parse(_parentServer.Hostname);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(_parentServer.Hostname);
				ipAddr = ipHostInfo.AddressList[0];
			}
			IPEndPoint connectSock = new IPEndPoint(ipAddr, _parentServer.Port);
			_clientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Logger.Debug("Initiating Socket Connection.");
			_clientSock.BeginConnect(connectSock, OnConnect, _clientSock);
		}

		private void OnConnect(IAsyncResult asyncResult) {
			Socket serverSock = (Socket)asyncResult.AsyncState;
			serverSock.EndConnect(asyncResult);
			Logger.Info("Connected.");
			_connectDone.Set();
			SocketComm serverComm = new SocketComm {
				ParentComm = this,
				WorkSocket = serverSock
			};
			//AppLog.WriteLine(5, "STATUS", "Waiting for data...");
			Logger.Debug("Starting Recieve Buffer.");
			serverComm.WorkSocket.BeginReceive(serverComm.Buffer, 0, SocketComm.BufferSize, 0, OnDataReceived, serverComm);
			Logger.Debug("Negotiating IRC Logon.");
			if (_parentServer.Pass != "") {
				_parentServer.QueueSend("PASS " + _parentServer.Pass, true);
				_parentServer.QueueSend("USER " + _parentServer.Nick + " 8 * : " + _parentServer.RealName, true);
				_parentServer.QueueSend("NICK " + _parentServer.Nick, true);
			} else {
				_parentServer.QueueSend("USER " + _parentServer.Nick + " 8 * : " + _parentServer.RealName, true);
				_parentServer.QueueSend("NICK " + _parentServer.Nick, true);
			}
			_parentServer.StartSendQueueConsumer();
		}

		private void OnDataReceived(IAsyncResult asyncResult) {
			SocketComm serverComm = (SocketComm)asyncResult.AsyncState;
			Socket sockHandler = serverComm.WorkSocket;
			try {
				Int32 bytesRead = sockHandler.EndReceive(asyncResult);
				if (bytesRead > 0) {
					Char[] tempByteArr = new Char[bytesRead];
					Int32 receivedLen = Encoding.UTF8.GetChars(serverComm.Buffer, 0, bytesRead, tempByteArr, 0);
					Char[] receivedCharArr = new Char[receivedLen];
					Array.Copy(tempByteArr, receivedCharArr, receivedLen);
					String receivedData = new String(receivedCharArr);
					serverComm.StringBuffer += receivedData;
					// Per RFC1459:
					//    The protocol messages must be extracted from the contiguous stream of octets. The current solution
					//    is to designate two characters, CR and LF, as message separators. Empty messages are silently ignored,
					//    which permits use of the sequence CR-LF between messages without extra problems.
					Int32 indexOfEndLine = serverComm.StringBuffer.IndexOfAny(new[] {'\r', '\n'});
					while (indexOfEndLine > -1) {
						if (indexOfEndLine == 0) {
							// If CR or LF is the first character of the line, remove it.
							serverComm.StringBuffer = serverComm.StringBuffer.Remove(0, 1);
						} else {
							// If a CR or LF is not the first character
							// Send it off to the parser. Passing current time as the "master time" the message was received.
							// Could this be a bottle neck? Implement some sort of queue to free the socket?
							_parentServer.ParseRawLine(serverComm.StringBuffer.Substring(0, indexOfEndLine), DateTime.UtcNow);
							// Remove the line from the beginning of the buffer
							serverComm.StringBuffer = serverComm.StringBuffer.Remove(0, indexOfEndLine);
						}
						// Seed the next value
						indexOfEndLine = serverComm.StringBuffer.IndexOfAny(new[] {'\r', '\n'});
					}
					// Begin receiving data on the socket again.
					sockHandler.BeginReceive(serverComm.Buffer, 0, SocketComm.BufferSize, 0, OnDataReceived, serverComm);
				} else {
					OnDisconnect(sockHandler);
				}
			}
			catch (SocketException se) {
				if (se.ErrorCode == 10054) {
					OnDisconnect(sockHandler);
				}
			}
			catch (ObjectDisposedException) {
				// The socket was closed previously and then came back, so just exit gracefully.
				//return;
			}
		}

		public Boolean Send(String dataToSend) {
			if (_clientSock.Connected) {
				dataToSend += "\n";
				_clientSock.BeginSend(Encoding.UTF8.GetBytes(dataToSend), 0, dataToSend.Length, 0, OnSendComplete, _clientSock);
				return true;
			}
			_parentServer.Connect();
			return false;
		}

		private void OnSendComplete(IAsyncResult asyncResult) {
			Socket serverSock = (Socket)asyncResult.AsyncState;
			serverSock.EndSend(asyncResult);
			_sendDone.Set();
		}

		public void OnDisconnect(Socket sockHandler) {
			Logger.Info("IRC Connection Severed.");
			sockHandler.Shutdown(SocketShutdown.Both);
			sockHandler.Close();
			_parentServer.Disconnected();
		}

		public void CloseConnection() {
			_clientSock.Shutdown(SocketShutdown.Both);
			_clientSock.Close();
		}
	}
}
