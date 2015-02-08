using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using MessagingServerCore.Interfaces;
using Newtonsoft.Json;

namespace MessagingServerCore
{
	public class UserClient : IClient
	{
		public string UserName { get; set; }
		[JsonIgnore]
		public string ClientType { get; set; }
		private string _status = "No status set";
		public string Status
		{
			get { return _status; }
			set { _status = value; }
		}
		private bool _isOnline = true;
		public bool IsOnline
		{
			get { return _isOnline; }
			set { _isOnline = value; }
		}
		[JsonIgnore]
		public Socket ClientSocket { get; set; }
		[JsonIgnore]
		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		[JsonIgnore]
		public ConcurrentDictionary<string, string> Properties { get; set; }
		[JsonIgnore]
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
