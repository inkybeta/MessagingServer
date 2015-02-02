﻿using System;
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
		public static CommandParameterPair BroadcastMessage(string username, params string[] value)
		{
			if (value.Length != 2)
			{
				return new CommandParameterPair("INVOP", Uri.EscapeDataString("Incorrect amount of parameters"));
			}
			foreach (KeyValuePair<string, UserClientService> client in Program.Clients)
			{
				client.Value.SendMessage(username, value[0]);
			}
			return null;
		}

		public static CommandParameterPair RequestInfo(string username, params string[] value)
		{
			return new CommandParameterPair("INFORESP", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
		}
	}
}
