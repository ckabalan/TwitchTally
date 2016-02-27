using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TwitchTally.WorkerComm {
	public class MasterServer {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private string m_Positions = "";
		// Declare a two dimensional array 
		// users allowed 10
		private static int m_MaxSlots = 11;
		private string[,] m_PosArray = new string[m_MaxSlots + 1, 1];
		X509Certificate m_ServerCertificate = new X509Certificate2(@"TwithTallyMaster.pfx", "TwithTallyMaster");
		ManualResetEvent m_TCPClientConnected = new ManualResetEvent(false);

		static void DisplayCertificateInformation(SslStream stream) {
			Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

			X509Certificate localCertificate = stream.LocalCertificate;
			if (stream.LocalCertificate != null) {
				Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
					localCertificate.Subject,
					localCertificate.GetEffectiveDateString(),
					localCertificate.GetExpirationDateString());
			} else {
				Console.WriteLine("Local certificate is null.");
			}
			// Display the properties of the client's certificate.
			X509Certificate remoteCertificate = stream.RemoteCertificate;
			if (stream.RemoteCertificate != null) {
				Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
					remoteCertificate.Subject,
					remoteCertificate.GetEffectiveDateString(),
					remoteCertificate.GetExpirationDateString());
			} else {
				Console.WriteLine("Remote certificate is null.");
			}
		}

		static string ReadMessage(SslStream sslStream) {
			// Read the  message sent by the client. 
			// The client signals the end of the message using the 
			// "<EOF>" marker.
            byte[] buffer = new byte[2048];
			StringBuilder messageData = new StringBuilder();
			int bytes = -1;
			do {
				// Read the client's test message.
				bytes = sslStream.Read(buffer, 0, buffer.Length);

				// Use Decoder class to convert from bytes to UTF8 
				// in case a character spans two buffers.
				Decoder decoder = Encoding.UTF8.GetDecoder();
				char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
				decoder.GetChars(buffer, 0, bytes, chars, 0);
				messageData.Append(chars);
				// Check for EOF or an empty message. 
				if (messageData.ToString().IndexOf("<EOF>") != -1) {
					break;
				}
			} while (bytes != 0);

			return messageData.ToString();
		}


		void ProcessIncomingData(object obj) {
			SslStream sslStream = (SslStream)obj;
			try {
				// Set timeouts for the read and write to 5 seconds.
				sslStream.ReadTimeout = 5000;
				sslStream.WriteTimeout = 5000;
				// Read a message from the client.   
				Console.WriteLine("Waiting for client message...");
				string messageData = ReadMessage(sslStream);
				Console.WriteLine("Received: {0}", messageData);

				// Write a message to the client. 
				byte[] message = Encoding.UTF8.GetBytes("Hello from the server.<EOF>");
				Console.WriteLine("Sending hello message.");
				sslStream.Write(message);
				sslStream.Flush();

			} catch (Exception e) { }

		}

		void ProcessIncomingConnection(IAsyncResult ar) {
			TcpListener listener = (TcpListener)ar.AsyncState;
			TcpClient client = listener.EndAcceptTcpClient(ar);
			SslStream sslStream = new SslStream(client.GetStream(), false);
			try {

				sslStream.AuthenticateAsServer(m_ServerCertificate, false, SslProtocols.Tls, true);
				//DisplayCertificateInformation(sslStream);
			} catch (Exception e) {
				Console.WriteLine("Client no ssl" + client);
				client.Close();
			}



			ThreadPool.QueueUserWorkItem(ProcessIncomingData, sslStream);
			m_TCPClientConnected.Set();
		}

		public void StartListening() {
			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Properties.Settings.Default.MinionCommPort);
			TcpListener listener = new TcpListener(endpoint);
			listener.Start();

			while (true) {
				m_TCPClientConnected.Reset();
				listener.BeginAcceptTcpClient(new AsyncCallback(ProcessIncomingConnection), listener);
				m_TCPClientConnected.WaitOne();
			}
		}
	}
}
