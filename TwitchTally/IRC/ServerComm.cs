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

		public bool Connected => _clientSock.Connected;

		public void StartClient(Server parentServer) {
			if ((_clientSock != null) && _clientSock.Connected) {
				Close(_clientSock);
			}
			// ReSharper disable once RedundantAssignment
			int i = 0;
			_parentServer = parentServer;
			IPAddress ipAddr;
			if (int.TryParse(_parentServer.Hostname.Substring(0, 1), out i)) {
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
				_parentServer.QueueSend("PASS " + _parentServer.Pass);
				_parentServer.QueueSend("USER " + _parentServer.Nick + " 8 * : " + _parentServer.RealName);
				_parentServer.QueueSend("NICK " + _parentServer.Nick);
			} else {
				_parentServer.QueueSend("USER " + _parentServer.Nick + " 8 * : " + _parentServer.RealName);
				_parentServer.QueueSend("NICK " + _parentServer.Nick);
			}
			_parentServer.StartSendQueueConsumer();
		}

		private void OnDataReceived(IAsyncResult asyncResult) {
			SocketComm serverComm = (SocketComm)asyncResult.AsyncState;
			Socket sockHandler = serverComm.WorkSocket;
			try {
				int bytesRead = sockHandler.EndReceive(asyncResult);
				if (bytesRead > 0) {
					char[] tempByteArr = new char[bytesRead];
					int receivedLen = Encoding.UTF8.GetChars(serverComm.Buffer, 0, bytesRead, tempByteArr, 0);
					char[] receivedCharArr = new char[receivedLen];
					Array.Copy(tempByteArr, receivedCharArr, receivedLen);
					String receivedData = new String(receivedCharArr);
					serverComm.StringBuffer += receivedData;
					// Per RFC1459:
					//    The protocol messages must be extracted from the contiguous stream of octets. The current solution
					//    is to designate two characters, CR and LF, as message separators. Empty messages are silently ignored,
					//    which permits use of the sequence CR-LF between messages without extra problems.
					int indexOfEndLine = serverComm.StringBuffer.IndexOfAny(new[] {'\r', '\n'});
                    while (indexOfEndLine > -1) {
	                    if (indexOfEndLine == 0) {
							// If CR or LF is the first character of the line, remove it.
							serverComm.StringBuffer = serverComm.StringBuffer.Remove(0, 1);
						} else {
							// If a CR or LF is not the first character
							// Send it off to the parser. Could this be a bottle neck? Implement some sort of queue to free the socket?
							_parentServer.ParseRawLine(serverComm.StringBuffer.Substring(0, indexOfEndLine));
							// Remove the line from the beginning of the buffer
							serverComm.StringBuffer = serverComm.StringBuffer.Remove(0, indexOfEndLine);
						}
						// Seed the next value
						indexOfEndLine = serverComm.StringBuffer.IndexOfAny(new[] { '\r', '\n' });
					}
					// Begin receiving data on the socket again.
					sockHandler.BeginReceive(serverComm.Buffer, 0, SocketComm.BufferSize, 0, OnDataReceived, serverComm);
				} else {
					Close(sockHandler);
				}
			} catch (SocketException se) {
				if (se.ErrorCode == 10054) {
					Close(sockHandler);
				}
			}
		}

		public bool Send(string dataToSend) {
			if (_clientSock.Connected) {
				dataToSend += "\n";
				_clientSock.BeginSend(Encoding.UTF8.GetBytes(dataToSend), 0, dataToSend.Length, 0, OnSendComplete, _clientSock);
				return true;
			} else {
				_parentServer.Connect();
				return false;
			}
		}

		private void OnSendComplete(IAsyncResult asyncResult) {
			Socket serverSock = (Socket)asyncResult.AsyncState;
			serverSock.EndSend(asyncResult);
			_sendDone.Set();
		}

		public void Close(Socket sockHandler) {
			Logger.Info("IRC Connection Closed.");
			sockHandler.Shutdown(SocketShutdown.Both);
			sockHandler.Close();
			// Should do some logic here to reconnect the socket.
		}
	}
}
