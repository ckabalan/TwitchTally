using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;

namespace TwitchTally.Communication {
	public class Worker {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		public TcpClient Client;
		public SslStream SSLStream;
		public String DataBuffer;
		public int Index;

		public Worker(TcpClient i_TcpClient) {
			Client = i_TcpClient;
			SSLStream = new SslStream(Client.GetStream(), false);
			OnClientConnect();
		}

		private void OnClientConnect() {
			// Do Something Here.
		}

		/// <summary>
		/// Sends data to associated Client
		/// </summary>
		/// <param name="Data">String containing the data to be sent to the client</param>
		public void Send(String Data) {
			// Write a message to the client. 
			Logger.Trace("SSL Send: {0}", Data);
			byte[] byteData = Encoding.UTF8.GetBytes(Data + "\x4");
			SSLStream.Write(byteData);
			SSLStream.Flush();
		}
		
		/// <summary>
		/// Triggered when data is recieved from the Client
		/// </summary>
		/// <param name="Data">Contains data sent from the Client</param>
		public void OnReceiveData(String Data) {
			//ParseMessage(Data);
			Logger.Trace("SSL Recieve: {0}", Data);
			if (Data.ToUpper().Contains("PING?")) {
				Send("PONG!");
			}
		}


	}
}
