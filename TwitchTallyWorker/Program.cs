using System;
using NLog;
using TwitchTallyWorker.Queueing;

namespace TwitchTallyWorker {
	class Program {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		static void Main(string[] args) {
			//Master master = new Master();
			//master.Connect();
			//WorkerConnection workerConnection = new WorkerConnection();
			//workerConnection.Connect();
			IncommingQueue incommingQueue = new IncommingQueue();

			Logger.Info("Waiting for User Input before exiting.");
			Console.ReadLine();
		}
	}
}
