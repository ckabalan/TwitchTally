// <copyright file="OutgoingQueue.cs" company="SpectralCoding.com">
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
using System.Text;
using RabbitMQ.Client;

namespace TwitchTally.Queueing {
	public static class OutgoingQueue {
		private static IModel _channel;

		public static void Initialize() {
			ConnectionFactory factory = new ConnectionFactory {
				HostName = Properties.Settings.Default.RabbitMQHostname,
				Port = Properties.Settings.Default.RabbitMQPort,
				UserName = Properties.Settings.Default.RabbitMQUsername,
				Password = Properties.Settings.Default.RabbitMQPassword
			};
			IConnection connection = factory.CreateConnection();
			_channel = connection.CreateModel();
			_channel.QueueDeclare(Properties.Settings.Default.RabbitMQIRCQueue, true, false, false, null);
		}

		public static void QueueRaw(String data) {
			var body = Encoding.UTF8.GetBytes(data);
			var properties = _channel.CreateBasicProperties();
			properties.Persistent = true;
			_channel.BasicPublish("", Properties.Settings.Default.RabbitMQIRCQueue, properties, body);
		}

		public static void QueueIrc(String lineToQueue, DateTime dateTime) {
			QueueRaw($"{dateTime:O}|{lineToQueue}");
		}
	}
}
