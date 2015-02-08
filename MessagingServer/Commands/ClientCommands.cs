using System;
using System.Collections.Generic;
using System.Linq;
using MessagingServerBusiness;
using MessagingServerCore;
using Newtonsoft.Json;

namespace MessagingServer.Commands
{
	public static class ClientCommands
	{
		public static CommandParameterPair BroadcastMessage(string username, params string[] value)
		{
			if (value.Length != 1)
			{
				return new CommandParameterPair("INVOP", Uri.EscapeDataString("Incorrect amount of parameters"));
			}
			foreach (KeyValuePair<string, IMessagingClient> client in Program.Clients)
			{
				client.Value.SendMessage(username, value[0]);
			}
			return null;
		}

		public static CommandParameterPair RequestInfo(string username, params string[] value)
		{
			return new CommandParameterPair("INFORESP", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
		}

		public static CommandParameterPair BroadcastAfkUser(string username, params string[] value)
		{
			if (value.Length != 1)
				return new CommandParameterPair("INVOP", "Only one value (true/false) may be sent!");
			foreach (IMessagingClient client in Program.Clients.Values)
			{
				client.SendCommand(new CommandParameterPair("AFKUSER", username, value[0]));
			}
			return null;
		}
	}
}
