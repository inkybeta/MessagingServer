using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServerCore
{
    public class UserClient
    {
		public string UserName { get; set; }
		public string ClientType { get; set; }
		public Socket ClientSocket { get; set; }
		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		public ConcurrentDictionary<string, string> Properties { get; set; } 

	    public UserClient(string userName, string clientType, Socket clientSocket, ConcurrentDictionary<string, string> groupAndRole, ConcurrentDictionary<string, string> properties)
	    {
		    UserName = userName;
		    ClientType = clientType;
		    ClientSocket = clientSocket;
		    GroupAndRole = groupAndRole;
		    Properties = properties;
	    }
    }
}
