using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MessagingServerCore.Interfaces
{
	public interface IClient
	{
		string UserName { get; set; }
		[JsonIgnore]
		string ClientType { get; set; }
		string Status { get; set; }
		bool IsOnline { get; set; }
		Socket ClientSocket { get; set; }
		[JsonIgnore]
		ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		[JsonIgnore]
		ConcurrentDictionary<string, string> Properties { get; set; }
		[JsonIgnore]
		DateTime TimeLastUsed { get; set; }
	}
}
