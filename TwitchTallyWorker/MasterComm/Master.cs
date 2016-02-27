using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace TwitchTallyWorker.MasterComm {
	public class Master {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		private MasterComm m_MasterComm;
		private String m_MasterHost = String.Empty;
		private Int32 m_MasterPort = -1;
		public String Hostname { get { return m_MasterHost; } set { m_MasterHost = value; } }
		public int Port { get { return m_MasterPort; } set { m_MasterPort = value; } }


		public Master() {
			m_MasterHost = Properties.Settings.Default.MasterHostname;
			m_MasterPort = Properties.Settings.Default.MasterPort;
		}

		public void Connect() {
			Logger.Info("Connecting to {0}:{1}...", Properties.Settings.Default.MasterHostname, Properties.Settings.Default.MasterPort);
			m_MasterComm = new MasterComm();
			m_MasterComm.StartClient(this);
		}

		public void ParseRawLine(String LineToParse) {
			Logger.Trace("Incomming Data: {0}", LineToParse);
		}

		public void Send(string i_DataToSend) {
			// No queuing here, just override it.
			Logger.Trace("Outgoing Data: {0}", i_DataToSend);
			m_MasterComm.Send(i_DataToSend);
		}
	}
}
