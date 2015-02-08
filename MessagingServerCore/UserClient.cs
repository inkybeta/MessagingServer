using System;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace MessagingServerCore
{
    public class UserClient
    {
		public string UserName { get; set; }
		public string ClientType { get; set; }
		public Socket ClientSocket { get; set; }
		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		public ConcurrentDictionary<string, string> Properties { get; set; }
		public DateTime TimeLastUsed { get; set; }

	    public UserClient(string userName, string clientType, Socket clientSocket, ConcurrentDictionary<string, string> groupAndRole, ConcurrentDictionary<string, string> properties, DateTime time)
	    {
		    UserName = userName;
		    ClientType = clientType;
		    ClientSocket = clientSocket;
		    GroupAndRole = groupAndRole;
		    Properties = properties;
		    TimeLastUsed = time;
	    }
    }
}
