// <copyright file="IncommingQueue.cs" company="SpectralCoding.com">
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
using System.Globalization;
using System.Text;
using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TwitchTallyWorker.Processing;

namespace TwitchTallyWorker.Queueing {
	class IncommingQueue {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public IncommingQueue() {
			var factory = new ConnectionFactory {
				HostName = Properties.Settings.Default.RabbitMQHostname,
				Port = Properties.Settings.Default.RabbitMQPort,
				UserName = Properties.Settings.Default.RabbitMQUsername,
				Password = Properties.Settings.Default.RabbitMQPassword
			};
			using (var connection = factory.CreateConnection()) {
				using (var channel = connection.CreateModel()) {
					channel.QueueDeclare(Properties.Settings.Default.RabbitMQIRCQueue, true, false, false, null);
					channel.BasicQos(0, 1, false);
					EventingBasicConsumer consumer = new EventingBasicConsumer(channel);
					consumer.Received += (model, ea) => {
						Byte[] body = ea.Body;
						String message = Encoding.UTF8.GetString(body);
						String[] strSplit = message.Split(new[] {'|'}, 2);
						DateTime ircDateTime;
						if (DateTime.TryParseExact(strSplit[0], "o", CultureInfo.InvariantCulture, DateTimeStyles.None, out ircDateTime)) {
							IrcParser.Parse(strSplit[1], ircDateTime);
						} else {
							Logger.Info("Bad RabbitMQ Entry: {0}", message);
						}
						channel.BasicAck(ea.DeliveryTag, false);
					};
					channel.BasicConsume(Properties.Settings.Default.RabbitMQIRCQueue, false, consumer);
					Console.WriteLine(" Press [enter] to exit.");
					Console.ReadLine();
				}
			}
		}
	}
}
