using System;
using System.IO;
using System.Net.Sockets;

namespace TwitchTallyShared {
	public class SocketComm {
		public Socket WorkSocket = null;
		public const int BufferSize = 1024;
		public byte[] Buffer = new byte[BufferSize];
		public MemoryStream MemoryStream = new MemoryStream();
		public BinaryWriter BinaryWriter;
		public string StringBuffer;
		public object ParentComm;

		public SocketComm() {
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
