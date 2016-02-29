// <copyright file="SocketComm.cs" company="SpectralCoding.com">
//     Copyright (c) 2016 SpectralCoding
// </copyright>
// <license>
// This file is part of TwitchTally.
// 
// TwitchTally is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// TwitchTally is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with TwitchTally.  If not, see <http://www.gnu.org/licenses/>.
// </license>
// <author>Caesar Kabalan</author>

using System;
using System.IO;
using System.Net.Sockets;

namespace TwitchTallyShared {
	public class SocketComm {
		public const Int32 BufferSize = 1024;
		public BinaryWriter BinaryWriter;
		public Byte[] Buffer = new Byte[BufferSize];
		public MemoryStream MemoryStream = new MemoryStream();
		public Object ParentComm;
		public String StringBuffer;
		public Socket WorkSocket = null;

		public SocketComm() {
			BinaryWriter = new BinaryWriter(MemoryStream);
		}

		public void ResetBuffer() {
			StringBuffer = String.Empty;
			Buffer = new Byte[BufferSize];
			MemoryStream = new MemoryStream();
			BinaryWriter = new BinaryWriter(MemoryStream);
		}
	}
}
