using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TwitchTallyWorker.Communication {
	public class MasterServer {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private WorkerConnection m_WorkerConnection;

		public MasterServer(WorkerConnection i_WorkerConnection) {
			m_WorkerConnection = i_WorkerConnection;
			OnClientConnect();
		}

		private void OnClientConnect() {
			// Do Something Here.
		}

		/// <summary>
		/// Triggered when data is recieved from the Client
		/// </summary>
		/// <param name="Data">Contains data sent from the Client</param>
		public void OnReceiveData(String Data) {
			//if (Data.Length > 1000) {
			//	Console.WriteLine("From ClientInfo " + Index + ":\t" + Data.Substring(0, 1000) + "...");
			//} else {
			//	Console.WriteLine("From ClientInfo " + Index + ":\t" + Data);
			//}
			//ParseMessage(Data);
			Logger.Trace("SSL Recieve: {0}", Data);
			if (Data.ToUpper().Contains("PING?")) {
				Send("PONG!");
				Send("PING?");
			}
		}

		private void Send(String Data) {
			m_WorkerConnection.Send(Data);
		}
	}
}
