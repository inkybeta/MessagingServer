using System;
using System.Net.Sockets;

namespace MessagingServer.Models
{
	public class AnonymousThread
	{
		public string Guid { get; set; }
		public TcpClient Client { get; set; }
		public DateTime TimeCreated { get; set; }

		public AnonymousThread(string guid, TcpClient client)
		{
			Guid = guid;
			Client = client;
			TimeCreated = DateTime.UtcNow;
		}
	}
}
