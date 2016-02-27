using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTally.WorkerComm {
	public class WorkerClient {
		public Socket Socket;
		public String DataBuffer;
		public int Index;

		public WorkerClient(Socket i_Socket) {
			Socket = i_Socket;
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
			if (Data.Length > 150) {
				Console.WriteLine("To ClientInfo " + Index + ":\t" + Data.Substring(0, 150) + "...");
			} else {
				Console.WriteLine("To ClientInfo " + Index + ":\t" + Data);
			}
			byte[] byteData = Encoding.UTF8.GetBytes(Data + "\x4");
			Socket.Send(byteData);
		}

		/// <summary>
		/// Triggered when data is recieved from the Client
		/// </summary>
		/// <param name="Data">Contains data sent from the Client</param>
		public void OnReceiveData(String Data) {
			if (Data.Length > 1000) {
				Console.WriteLine("From ClientInfo " + Index + ":\t" + Data.Substring(0, 1000) + "...");
			} else {
				Console.WriteLine("From ClientInfo " + Index + ":\t" + Data);
			}
			//ParseMessage(Data);
			Console.WriteLine(Data);
		}


	}
}
