using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTally.IRC {
	public class IRCComm {
		public Socket WorkSocket = null;
		public const int BufferSize = 1024;
		public byte[] Buffer = new byte[BufferSize];
		public MemoryStream MemoryStream = new MemoryStream();
		public BinaryWriter BinaryWriter;
		public string StringBuffer;
		public ServerComm ParentServerComm;

		public IRCComm() {
			BinaryWriter = new BinaryWriter(MemoryStream);
		}

		public void ResetBuffer() {
			StringBuffer = String.Empty;
			Buffer = new byte[BufferSize];
			MemoryStream = new MemoryStream();
			BinaryWriter = new BinaryWriter(MemoryStream);
		}

	}
}
