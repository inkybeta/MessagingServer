using System.Net.Sockets;

namespace MessagingServer.Models
{
	public class AnonymousThread
	{
		public string Guid { get; set; }
		public Socket Client { get; set; }

		public AnonymousThread(string guid, Socket client)
		{
			Guid = guid;
			Client = client;
		}
	}
}
