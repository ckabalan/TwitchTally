using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTally.Communication {
	public class SslStreamEventArgs {
		public byte[] DataBuffer = new byte[Properties.Settings.Default.MinionCommBufferSize];
		public int WorkerIndex;
		public Worker Worker;
		/// <summary>
		/// Constructor which starts a SocketPacket based on a ClientInfo object.
		/// </summary>
		/// <param name="iServer">Associated ClientInfo object</param>
		public SslStreamEventArgs(Worker iWorker) {
			Worker = iWorker;
		}
	}
}
