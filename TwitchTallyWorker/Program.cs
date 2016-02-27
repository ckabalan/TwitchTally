using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NLog;
using TwitchTallyShared;
using TwitchTallyWorker.Communication;

namespace TwitchTallyWorker {
	class Program {
		private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args) {
			//Master master = new Master();
			//master.Connect();
			WorkerConnection workerConnection = new WorkerConnection();
			workerConnection.Connect();
			Logger.Info("Waiting for User Input before exiting.");
			Console.ReadLine();
		}
	}
}
