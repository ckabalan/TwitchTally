using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TwitchTallyShared;

namespace TwitchTallyWorker.Communication {
	public class WorkerConnection {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private TcpClient m_TcpClient;
		private SslStream m_SslStream;
		private byte[] m_ByteBuffer = new byte[Properties.Settings.Default.MasterCommBufferSize];
		private String m_StringBuffer;
		private MasterServer m_MasterServer;
		private AsyncCallback m_AsyncMasterCallback;

		public void Connect() {
			if ((m_TcpClient != null) && (m_TcpClient.Connected)) {
				//Close(m_TcpClient);
			}
			int i = 0;
			IPAddress IPAddr;
			if (int.TryParse(Properties.Settings.Default.MasterHostname.Substring(0, 1), out i)) {
				IPAddr = IPAddress.Parse(Properties.Settings.Default.MasterHostname);
			} else {
				IPHostEntry ipHostInfo = Dns.GetHostEntry(Properties.Settings.Default.MasterHostname);
				IPAddr = ipHostInfo.AddressList[0];
			}
			m_TcpClient = new TcpClient();
			Logger.Debug("Initiating TCP Connection.");
			m_TcpClient.BeginConnect(IPAddr, Properties.Settings.Default.MasterPort, new AsyncCallback(OnConnect), null);
		}

		private void OnConnect(IAsyncResult i_AsyncResult) {
			if (m_AsyncMasterCallback == null) { m_AsyncMasterCallback = new AsyncCallback(OnDataReceived); }
			m_MasterServer = new MasterServer(this);
			m_SslStream = new SslStream(m_TcpClient.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
			m_SslStream.AuthenticateAsClient("TwitchTally Master Server");
			Logger.Info("Connected.");
			m_SslStream.BeginRead(
				m_ByteBuffer,
				0,
				m_ByteBuffer.Length,
				m_AsyncMasterCallback,
				null
			);
			// Do authentication here
		}

		private void OnDataReceived(IAsyncResult i_AsyncResult) {
			int receiveLen = 0;
			receiveLen = m_SslStream.EndRead(i_AsyncResult);
			if (receiveLen == 0) {
				//CloseClientInfoConnection(tempSslStreamEventArgs.WorkerIndex);
			} else {
				char[] receiveCharsOld = new char[receiveLen];
				int charLength = Encoding.UTF8.GetChars(m_ByteBuffer, 0, receiveLen, receiveCharsOld, 0);
				char[] receiveChars = new char[charLength];
				Array.Copy(receiveCharsOld, receiveChars, charLength);
				String receiveData = new String(receiveChars);
				m_StringBuffer += receiveData;
				// See if multiple messages were sent in the same buffer space, if so call OnReceiveData for all of them.
				// Note: \x4 is ASCII 4 (EOT/End of Transmission). This is not part of SSL, it is what we use to detect the end of a message.
				if (Functions.OccurancesInString(m_StringBuffer, "\x4") >= 1) {
					String[] splitIncommingData = m_StringBuffer.Split(("\x4").ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < splitIncommingData.Length; i++) {
						m_StringBuffer = m_StringBuffer.Remove(0, splitIncommingData[i].Length + 1);
						m_MasterServer.OnReceiveData(splitIncommingData[i]);
						//ClientInfoIndexToClientInfo(SslStreamEventArgs.WorkerIndex).OnReceiveData(splitIncommingData[i]);
					}
				}
				//if (SslStreamEventArgs.WorkerClient.SSLStream..Connected == true) {
				m_SslStream.BeginRead(
					m_ByteBuffer,
					0,
					m_ByteBuffer.Length,
					m_AsyncMasterCallback,
					null
				);
				//}
			}
		}

		public void Send(String Data) {
			// Write a message to the client. 
			Logger.Trace("SSL Send: {0}", Data);
			byte[] byteData = Encoding.UTF8.GetBytes(Data + "\x4");
			m_SslStream.Write(byteData);
			m_SslStream.Flush();
		}



		private static Hashtable certificateErrors = new Hashtable();
		public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;

			Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

			// Do not allow this client to communicate with unauthenticated servers when false.
			//return false;
			//Force ssl certificates as correct
			return true;
		}
	}

}
