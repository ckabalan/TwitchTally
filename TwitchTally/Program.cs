using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TwitchTally.IRC;
using TwitchTally.Logging;

namespace TwitchTally {
	class Program {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The main entry function for the application.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		static void Main(string[] args) {
			//Console.SetBufferSize(250, 20000);
			//Console.SetWindowSize(250, 50);
			Logger.Info("TwitchTally v" + Assembly.GetExecutingAssembly().GetName().Version + " started.");
			Connect();
			Logger.Info("Waiting for User Input before exiting.");
			Console.ReadLine();
			IRCLog.CloseLog();
		}

		static void Connect() {
			Server mainConnection = new Server();
			mainConnection.Hostname = Properties.Settings.Default.IRCServer;
			mainConnection.Port = Properties.Settings.Default.IRCPort;
			mainConnection.Nick = Properties.Settings.Default.IRCUsername;
			mainConnection.AltNick = Properties.Settings.Default.IRCUsernameAlt;
			mainConnection.Pass = Properties.Settings.Default.IRCPassword;
			mainConnection.Connect();
		}
	}
}
