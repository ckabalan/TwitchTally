using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TwitchTally.Logging {
	public static class IRCLog {
		private static FileStream m_LogFile;
		private static StreamWriter m_LogStream;

		private static void OpenLog() {
			String logDir = Properties.Settings.Default.LogDirectory.TrimEnd(new Char[] {'/', '\\'});
            if (!Directory.Exists(Properties.Settings.Default.LogDirectory)) {
				Directory.CreateDirectory(Properties.Settings.Default.LogDirectory);
			}
			string logname = String.Format("{0}/{1:yyyyMMdd-HH}.log", logDir, DateTime.UtcNow);
			if (File.Exists(logname)) {
				m_LogFile = new FileStream(String.Format("{0}/{1:yyyyMMdd-HH}.log", logDir, DateTime.UtcNow), FileMode.Append, FileAccess.Write);
			} else {
				m_LogFile = new FileStream(String.Format("{0}/{1:yyyyMMdd-HH}.log", logDir, DateTime.UtcNow), FileMode.Create, FileAccess.Write);
			}
			m_LogStream = new StreamWriter(m_LogFile);
			m_LogStream.AutoFlush = true;
			WriteLine(String.Format("Opened (v{0})", Assembly.GetExecutingAssembly().GetName().Version.ToString()), true);
		}

		public static void CloseLog() {
			WriteLine("Closed.", true);
			m_LogStream.Close();
			m_LogFile.Close();
		}

		public static void WriteLine(string LineToAdd, bool Meta = false) {
			//TimeSpan TimeSinceMidnight = DateTime.UtcNow.TimeOfDay;
			//double MSOfDay = Math.Floor(TimeSinceMidnight.TotalMilliseconds);
			//Output = String.Format("{0} {1}", MSOfDay, LineToAdd);
			if (m_LogFile == null) {
				OpenLog();
			} else {
				if (Path.GetFileName(m_LogFile.Name) != String.Format("{0:yyyyMMdd-HH}.log", DateTime.UtcNow)) {
					m_LogStream.Close();
					m_LogFile.Close();
					OpenLog();
				}
			}
			if (Meta) {
				// Meta lines seperate the Timestamp from the content with #
				m_LogStream.WriteLine(String.Format("{0:O}#{1}", DateTime.UtcNow, LineToAdd));
			} else {
				// Strict log lines seperate the Timestamp from the content with |
				m_LogStream.WriteLine(String.Format("{0:O}|{1}", DateTime.UtcNow, LineToAdd));
			}
		}
	}
}
