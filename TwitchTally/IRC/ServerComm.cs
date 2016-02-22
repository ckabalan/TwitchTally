using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchTallyShared;
using NLog;

namespace TwitchTally.IRC {
	public class ServerComm {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private ManualResetEvent m_ConnectDone = new ManualResetEvent(false);
		private ManualResetEvent m_SendDone = new ManualResetEvent(false);
		private ManualResetEvent m_ReceiveDone = new ManualResetEvent(false);
		private String m_Response = String.Empty;
		private Socket m_ClientSock;
		private Server m_ParentServer;

		public void StartClient(Server i_ParentServer) {
			if ((m_ClientSock != null) && (m_ClientSock.Connected)) {
				Close(m_ClientSock);
			}
			m_ParentServer = i_ParentServer;
			int i = 0;
			IPAddress IPAddr;
			if (int.TryParse(m_ParentServer.Hostname.Substring(0, 1), out i)) {
				IPAddr = IPAddress.Parse(m_ParentServer.Hostname);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(m_ParentServer.Hostname);
				IPAddr = ipHostInfo.AddressList[0];
			}
			IPEndPoint ConnectSock = new IPEndPoint(IPAddr, m_ParentServer.Port);
			m_ClientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Logger.Debug("Initiating Socket Connection.");
			m_ClientSock.BeginConnect(ConnectSock, new AsyncCallback(OnConnect), m_ClientSock);
		}

		private void OnConnect(IAsyncResult i_AsyncResult) {
			Socket ServerSock = (Socket)i_AsyncResult.AsyncState;
			ServerSock.EndConnect(i_AsyncResult);
			Logger.Info("Connected.");
			m_ConnectDone.Set();
			IRCComm ServerComm = new IRCComm();
			ServerComm.ParentServerComm = this;
			ServerComm.WorkSocket = ServerSock;
			//AppLog.WriteLine(5, "STATUS", "Waiting for data...");
			Logger.Debug("Starting Recieve Buffer.");
			ServerComm.WorkSocket.BeginReceive(ServerComm.Buffer, 0, IRCComm.BufferSize, 0, new AsyncCallback(OnDataReceived), ServerComm);
			Logger.Debug("Negotiating IRC Logon.");
			if (m_ParentServer.Pass != "") {
				m_ParentServer.Send("PASS " + m_ParentServer.Pass);
				m_ParentServer.Send("USER " + m_ParentServer.Nick + " 8 * : " + m_ParentServer.RealName);
				m_ParentServer.Send("NICK " + m_ParentServer.Nick);
			} else {
				m_ParentServer.Send("USER " + m_ParentServer.Nick + " 8 * : " + m_ParentServer.RealName);
				m_ParentServer.Send("NICK " + m_ParentServer.Nick);
			}
		}

		private void OnDataReceived(IAsyncResult i_AsyncResult) {
			IRCComm ServerComm = (IRCComm)i_AsyncResult.AsyncState;
			Socket SockHandler = ServerComm.WorkSocket;
			try {
				int BytesRead = SockHandler.EndReceive(i_AsyncResult);
				if (BytesRead > 0) {
					char[] TempByteArr = new char[BytesRead];
					int ReceivedLen = Encoding.UTF8.GetChars(ServerComm.Buffer, 0, BytesRead, TempByteArr, 0);
					char[] ReceivedCharArr = new char[ReceivedLen];
					Array.Copy(TempByteArr, ReceivedCharArr, ReceivedLen);
					String ReceivedData = new String(ReceivedCharArr);
					ServerComm.StringBuffer += ReceivedData;
					// Per RFC1459:
					//    The protocol messages must be extracted from the contiguous stream of octets. The current solution
					//    is to designate two characters, CR and LF, as message separators. Empty messages are silently ignored,
					//    which permits use of the sequence CR-LF between messages without extra problems.
					int IndexOfEndLine = ServerComm.StringBuffer.IndexOfAny(new Char[] {'\r', '\n'});
                    while (IndexOfEndLine > -1) {
	                    if (IndexOfEndLine == 0) {
							// If CR or LF is the first character of the line, remove it.
							ServerComm.StringBuffer = ServerComm.StringBuffer.Remove(0, 1);
						} else {
							// If a CR or LF is not the first character
							// Send it off to the parser. Could this be a bottle neck? Implement some sort of queue to free the socket?
							m_ParentServer.ParseRawLine(ServerComm.StringBuffer.Substring(0, IndexOfEndLine));
							// Remove the line from the beginning of the buffer
							ServerComm.StringBuffer = ServerComm.StringBuffer.Remove(0, IndexOfEndLine);
						}
						// Seed the next value
						IndexOfEndLine = ServerComm.StringBuffer.IndexOfAny(new Char[] { '\r', '\n' });
					}
					// Begin receiving data on the socket again.
					SockHandler.BeginReceive(ServerComm.Buffer, 0, IRCComm.BufferSize, 0, new AsyncCallback(OnDataReceived), ServerComm);
				} else {
					Close(SockHandler);
				}
			} catch (SocketException Se) {
				if (Se.ErrorCode == 10054) {
					Close(SockHandler);
				}
			}
		}

		public bool Send(string i_DataToSend) {
			if (m_ClientSock.Connected) {
				i_DataToSend += "\n";
				byte[] NewData = new byte[i_DataToSend.Length];
				m_ClientSock.BeginSend(Encoding.UTF8.GetBytes(i_DataToSend), 0, i_DataToSend.Length, 0, new AsyncCallback(OnSendComplete), m_ClientSock);
				return true;
			} else {
				m_ParentServer.Connect();
				return false;
			}
		}

		private void OnSendComplete(IAsyncResult i_AsyncResult) {
			Socket ServerSock = (Socket)i_AsyncResult.AsyncState;
			int BytesSent = ServerSock.EndSend(i_AsyncResult);
			m_SendDone.Set();
		}

		public void Close(Socket i_SockHandler) {
			try {
				Logger.Info("IRC Connection Closed.");
				i_SockHandler.Shutdown(SocketShutdown.Both);
				i_SockHandler.Close();
				//m_ParentServer.NetworkLog.CloseLog();
				//m_ParentServer.Channels = null;
				//m_ParentServer.BotCommands = null;
				//m_ParentServer.ConnectionWatchdog.Destroy();
				//m_ParentServer.ConnectionWatchdog = null;
				//AppLog.WriteLine(5, "CONN", "Everything cleaned up, sleeping for 5 sec until reconnect attempt...");
				//Thread.Sleep(5000);
				//m_ParentServer.Connect();
			} catch (Exception) { }
		}
	}
}
