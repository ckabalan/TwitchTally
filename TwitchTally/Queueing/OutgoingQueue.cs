using System;
using System.Text;
using RabbitMQ.Client;

namespace TwitchTally.Queueing {
	class OutgoingQueue {
		private IConnection m_Connection;
		private IModel m_Channel;

		public OutgoingQueue() {
			ConnectionFactory factory = new ConnectionFactory() { HostName = "192.168.1.196" };
			m_Connection = factory.CreateConnection();
			m_Channel = m_Connection.CreateModel();
			m_Channel.QueueDeclare(queue: "irc_queue",
				durable: true,
				exclusive: false,
				autoDelete: false,
				arguments: null);
		}

		public void Queue(String data) {
			var body = Encoding.UTF8.GetBytes(data);
			var properties = m_Channel.CreateBasicProperties();
			properties.Persistent = true;
			m_Channel.BasicPublish(exchange: "",
				routingKey: "irc_queue",
				basicProperties: properties,
				body: body);
			Console.WriteLine(" [x] Sent {0}", data);
			Console.WriteLine(" Press [enter] to exit.");
			Console.ReadLine();
		}
	}
}
