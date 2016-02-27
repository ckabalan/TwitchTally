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
using System.Threading;
using NLog;
using TwitchTallyShared;

namespace TwitchTally.Communication {
	public class MasterListener {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private X509Certificate m_ServerCertificate = new X509Certificate2(@"TwithTallyMaster.pfx", "TwithTallyMaster");
		private ManualResetEvent m_TCPClientConnected = new ManualResetEvent(false);
		private ArrayList m_WorkerList = ArrayList.Synchronized(new ArrayList());
		private TcpListener m_Listener;
		private AsyncCallback m_AsyncWorkerCallback;

		public void StartListening() {
			m_Listener = new TcpListener(new IPEndPoint(IPAddress.Any, Properties.Settings.Default.MinionCommPort));
			m_Listener.Start();
			m_Listener.BeginAcceptTcpClient(new AsyncCallback(OnWorkerConnect), null);
		}

		void OnWorkerConnect(IAsyncResult i_AsyncResult) {
			// Create WorkerClient object containing TcpClient, SslStream, and Buffer.
			Worker tempWorker = new Worker(m_Listener.EndAcceptTcpClient(i_AsyncResult));
			// Validate Certificate
			tempWorker.SSLStream.AuthenticateAsServer(m_ServerCertificate, false, SslProtocols.Tls, true);
			// Begin waiting for incomming data
			WaitForData(tempWorker);
			// Start accepting connections again.
			m_Listener.BeginAcceptTcpClient(new AsyncCallback(OnWorkerConnect), null);
			tempWorker.Send("PING?");
		}

		private void WaitForData(Worker i_Worker) {
			// Set up Async call back for when BeginRead comes back.
			if (m_AsyncWorkerCallback == null) { m_AsyncWorkerCallback = new AsyncCallback(OnDataReceived); }
			SslStreamEventArgs tempSslStreamEventArgs = new SslStreamEventArgs(i_Worker);
			i_Worker.SSLStream.BeginRead(
				tempSslStreamEventArgs.DataBuffer,
				0,
				tempSslStreamEventArgs.DataBuffer.Length,
				m_AsyncWorkerCallback,
				tempSslStreamEventArgs
			);
		}

		private void OnDataReceived(IAsyncResult i_AsyncResult) {
			SslStreamEventArgs SslStreamEventArgs = (SslStreamEventArgs)i_AsyncResult.AsyncState;
			int receiveLen = 0;
			receiveLen = SslStreamEventArgs.Worker.SSLStream.EndRead(i_AsyncResult);
			if (receiveLen == 0) {
				//CloseClientInfoConnection(tempSslStreamEventArgs.WorkerIndex);
			} else {
				char[] receiveCharsOld = new char[receiveLen];
				int charLength = Encoding.UTF8.GetChars(SslStreamEventArgs.DataBuffer, 0, receiveLen, receiveCharsOld, 0);
				char[] receiveChars = new char[charLength];
				Array.Copy(receiveCharsOld, receiveChars, charLength);
				String receiveData = new String(receiveChars);
				SslStreamEventArgs.Worker.DataBuffer += receiveData;
				// See if multiple messages were sent in the same buffer space, if so call OnReceiveData for all of them.
				// Note: \x4 is ASCII 4 (EOT/End of Transmission). This is not part of SSL, it is what we use to detect the end of a message.
				if (Functions.OccurancesInString(SslStreamEventArgs.Worker.DataBuffer, "\x4") >= 1) {
					String[] splitIncommingData = SslStreamEventArgs.Worker.DataBuffer.Split(("\x4").ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < splitIncommingData.Length; i++) {
						SslStreamEventArgs.Worker.DataBuffer = SslStreamEventArgs.Worker.DataBuffer.Remove(0, splitIncommingData[i].Length + 1);
						SslStreamEventArgs.Worker.OnReceiveData(splitIncommingData[i]);
						//ClientInfoIndexToClientInfo(SslStreamEventArgs.WorkerIndex).OnReceiveData(splitIncommingData[i]);
					}
				}
				//if (SslStreamEventArgs.WorkerClient.SSLStream..Connected == true) {
				WaitForData(SslStreamEventArgs.Worker);
				//}
			}
		}

		static void DisplayCertificateInformation(SslStream SSLStream) {
			Console.WriteLine("Certificate revocation list checked: {0}", SSLStream.CheckCertRevocationStatus);
			X509Certificate LocalCert = SSLStream.LocalCertificate;
			if (SSLStream.LocalCertificate != null) {
				Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
					LocalCert.Subject,
					LocalCert.GetEffectiveDateString(),
					LocalCert.GetExpirationDateString());
			} else {
				Console.WriteLine("Local certificate is null.");
			}
			// Display the properties of the client's certificate.
			X509Certificate remoteCertificate = SSLStream.RemoteCertificate;
			if (SSLStream.RemoteCertificate != null) {
				Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
					remoteCertificate.Subject,
					remoteCertificate.GetEffectiveDateString(),
					remoteCertificate.GetExpirationDateString());
			} else {
				Console.WriteLine("Remote certificate is null.");
			}
		}
	}
}
