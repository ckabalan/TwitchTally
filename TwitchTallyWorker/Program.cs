using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TwitchTallyShared;
using TwitchTallyWorker.MasterComm;

namespace TwitchTallyWorker {
	class Program {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args) {
			//Master master = new Master();
			//master.Connect();

			MasterSSL s = new MasterSSL();
			s.ConnectSSL("Test hehehe");

			Logger.Info("Waiting for User Input before exiting.");
			Console.ReadLine();
		}
	}
}
