using System;
using System.Collections.Concurrent;

namespace MessagingServerCore.Interfaces
{
	public interface IClient
	{
		string UserName { get; set; }
		string ClientType { get; set; }
		string Status { get; set; }
		bool IsOnline { get; set; }
		ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		ConcurrentDictionary<string, string> Properties { get; set; }
		DateTime TimeLastUsed { get; set; }
		void SendCommand(CommandParameterPair command);
		CommandParameterPair RecieveCommand();
		CommandParameterPair DecodeMessage(string input);
		string EncodeMessage(CommandParameterPair pair);
		void SendInvalid(string message);
		void CloseConnection();
		void Abort();
	}
}