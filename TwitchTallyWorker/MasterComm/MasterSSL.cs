using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TwitchTallyWorker.MasterComm {
	public class MasterSSL {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		// "Server SSL Certyficate (CN=www.domain.com)"
		public string hostname = "TwithTally Master Server";

		// "Server host www.example.com"
		public string host = "127.0.0.1";

		// "Server port"
		public int port = Properties.Settings.Default.MasterPort;

		List<string> PosOpenID = new List<string>();

		public static string txt = "";

		private static Hashtable certificateErrors = new Hashtable();

		protected void OnBar() {
			ConnectSSL("Hello Server dohoho");
		}


		//================================================================================================================
		//                                                                                  SSL Socket client Data send
		//================================================================================================================

		// The following method is invoked by the RemoteCertificateValidationDelegate.
		public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
			if (sslPolicyErrors == SslPolicyErrors.None)
				return true;

			Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

			// Do not allow this client to communicate with unauthenticated servers when false.
			//return false;
			//Force ssl certyfikates as correct
			return true;
		}

		static string ReadMessage(SslStream sslStream) {
			// Read the  message sent by the server. 
			// The end of the message is signaled using the 
			// "<EOF>" marker.
			byte[] buffer = new byte[2048];
			StringBuilder messageData = new StringBuilder();
			int bytes = -1;
			do {
				bytes = sslStream.Read(buffer, 0, buffer.Length);

				// Use Decoder class to convert from bytes to UTF8 
				// in case a character spans two buffers.
				Decoder decoder = Encoding.UTF8.GetDecoder();
				char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
				decoder.GetChars(buffer, 0, bytes, chars, 0);
				messageData.Append(chars);
				// Check for EOF. 
				if (messageData.ToString().IndexOf("<EOF>") != -1) {
					break;
				}
			} while (bytes != 0);

			return messageData.ToString();
		}


		public void ConnectSSL(string msg = "") {

			txt = "";
			try {
				TcpClient client = new TcpClient(host, port);

				// Create an SSL stream that will close the client's stream.
				SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
				try {
					sslStream.AuthenticateAsClient(hostname);
				} catch (AuthenticationException e) {
					Console.WriteLine("Exception: {0}", e.Message);
					if (e.InnerException != null) {
						Logger.Info("Inner exception: {0}", e.InnerException.Message);
					}
					Logger.Info("Authentication failed - closing the connection.");
					client.Close();
					return;
				}


				// Signal the end of the message using the "<EOF>".
				// Semd message
				byte[] messsage = Encoding.UTF8.GetBytes(msg + " <EOF>");
				// Send hello message to the server. 
				sslStream.Write(messsage);
				sslStream.Flush();
				// Read message from the server. 
				string serverMessage = ReadMessage(sslStream);
				Logger.Info("Server says: {0}", serverMessage);
				// Close the client connection.
				client.Close();
				Logger.Info("Client closed.");


			} catch (ArgumentNullException e) {
				Logger.Info("ArgumentNullException: {0}", e);
			} catch (SocketException e) {
				Logger.Info("SocketException: {0}", e);
			}

		}


	}

}
