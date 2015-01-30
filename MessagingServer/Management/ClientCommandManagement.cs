using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MessagingServerBusiness;
using MessagingServerCore;
using Newtonsoft.Json;

namespace MessagingServer.ClientManagementTasks
{
	public static class ClientCommandManagement
	{
		public static CommandParameterPair BroadcastMessage(params string[] value)
		{
			if (value.Length != 1)
			{
				return new CommandParameterPair("INVOP", Uri.EscapeDataString("Incorrect amount of parameters"));
			}
			foreach (KeyValuePair<string, UserClientService> client in Program.Clients)
			{
				client.Value.SendMessage(value[0]);
			}
			return null;
		}

		public static CommandParameterPair RequestInfo(params string[] value)
		{
			return new CommandParameterPair("INFORESP", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
		}
	}
}
