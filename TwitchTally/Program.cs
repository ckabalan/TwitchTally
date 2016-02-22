using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TwitchTally {
	class Program {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The main entry function for the application.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		static void Main(string[] args) {
			Console.SetBufferSize(150, 20000);
			Console.SetWindowSize(150, 50);
			Logger.Info("TwitchTally v" + Assembly.GetExecutingAssembly().GetName().Version + " started.");

			Logger.Info("Waiting for User Input before exiting.");
			Console.ReadLine();
		}
	}
}
