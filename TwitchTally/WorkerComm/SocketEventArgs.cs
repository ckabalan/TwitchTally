using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTally.WorkerComm {
	public class SocketEventArgs {
		public Socket Socket;
		public byte[] DataBuffer = new byte[Properties.Settings.Default.MinionCommBufferSize];
		public int WorkerIndex;
		public WorkerClient WorkerClient;
		/// <summary>
		/// Constructor which starts a SocketPacket based on a ClientInfo object.
		/// </summary>
		/// <param name="iServer">Associated ClientInfo object</param>
		public SocketEventArgs(WorkerClient i_WorkerClient) {
			Socket = i_WorkerClient.Socket;
			WorkerClient = i_WorkerClient;
		}
	}
}
