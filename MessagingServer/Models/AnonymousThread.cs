using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
