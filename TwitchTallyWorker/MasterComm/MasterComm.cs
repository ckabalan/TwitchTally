using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TwitchTallyShared;

namespace TwitchTallyWorker.MasterComm {
	public class MasterComm {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private ManualResetEvent m_ConnectDone = new ManualResetEvent(false);
		private ManualResetEvent m_SendDone = new ManualResetEvent(false);
		private ManualResetEvent m_ReceiveDone = new ManualResetEvent(false);
		private String m_Response = String.Empty;
		private Socket m_ClientSock;
		private Master m_ParentMaster;

		public bool Connected { get { return m_ClientSock.Connected; } }

		public void StartClient(Master i_ParentMaster) {
			if ((m_ClientSock != null) && (m_ClientSock.Connected)) {
				Close(m_ClientSock);
			}
			m_ParentMaster = i_ParentMaster;
			int i = 0;
			IPAddress IPAddr;
			if (int.TryParse(m_ParentMaster.Hostname.Substring(0, 1), out i)) {
				IPAddr = IPAddress.Parse(m_ParentMaster.Hostname);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(m_ParentMaster.Hostname);
				IPAddr = ipHostInfo.AddressList[0];
			}
			IPEndPoint ConnectSock = new IPEndPoint(IPAddr, m_ParentMaster.Port);
			m_ClientSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Logger.Debug("Initiating Socket Connection.");
			m_ClientSock.BeginConnect(ConnectSock, new AsyncCallback(OnConnect), m_ClientSock);
		}

		private void OnConnect(IAsyncResult i_AsyncResult) {
			Socket ServerSock = (Socket)i_AsyncResult.AsyncState;
			ServerSock.EndConnect(i_AsyncResult);
			Logger.Info("Connected.");
			m_ConnectDone.Set();
			SocketComm MasterComm = new SocketComm();
			MasterComm.ParentComm = this;
			MasterComm.WorkSocket = ServerSock;
			//AppLog.WriteLine(5, "STATUS", "Waiting for data...");
			Logger.Debug("Starting Recieve Buffer.");
			MasterComm.WorkSocket.BeginReceive(MasterComm.Buffer, 0, SocketComm.BufferSize, 0, new AsyncCallback(OnDataReceived), MasterComm);
			Logger.Debug("Negotiating Worker Logon.");
			// Send inital login here.
		}

		private void OnDataReceived(IAsyncResult i_AsyncResult) {
			SocketComm MasterComm = (SocketComm)i_AsyncResult.AsyncState;
			Socket SockHandler = MasterComm.WorkSocket;
			try {
				int BytesRead = SockHandler.EndReceive(i_AsyncResult);
				if (BytesRead > 0) {
					char[] TempByteArr = new char[BytesRead];
					int ReceivedLen = Encoding.UTF8.GetChars(MasterComm.Buffer, 0, BytesRead, TempByteArr, 0);
					char[] ReceivedCharArr = new char[ReceivedLen];
					Array.Copy(TempByteArr, ReceivedCharArr, ReceivedLen);
					String ReceivedData = new String(ReceivedCharArr);
					MasterComm.StringBuffer += ReceivedData;
					// Per RFC1459:
					//    The protocol messages must be extracted from the contiguous stream of octets. The current solution
					//    is to designate two characters, CR and LF, as message separators. Empty messages are silently ignored,
					//    which permits use of the sequence CR-LF between messages without extra problems.
					int IndexOfEndLine = MasterComm.StringBuffer.IndexOfAny(new Char[] { '\r', '\n' });
					while (IndexOfEndLine > -1) {
						if (IndexOfEndLine == 0) {
							// If CR or LF is the first character of the line, remove it.
							MasterComm.StringBuffer = MasterComm.StringBuffer.Remove(0, 1);
						} else {
							// If a CR or LF is not the first character
							// Send it off to the parser. Could this be a bottle neck? Implement some sort of queue to free the socket?
							m_ParentMaster.ParseRawLine(MasterComm.StringBuffer.Substring(0, IndexOfEndLine));
							// Remove the line from the beginning of the buffer
							MasterComm.StringBuffer = MasterComm.StringBuffer.Remove(0, IndexOfEndLine);
						}
						// Seed the next value
						IndexOfEndLine = MasterComm.StringBuffer.IndexOfAny(new Char[] { '\r', '\n' });
					}
					// Begin receiving data on the socket again.
					SockHandler.BeginReceive(MasterComm.Buffer, 0, SocketComm.BufferSize, 0, new AsyncCallback(OnDataReceived), MasterComm);
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
				m_ParentMaster.Connect();
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
			} catch (Exception) { }
		}

	}
}
