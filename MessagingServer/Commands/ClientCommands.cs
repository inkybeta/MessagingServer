using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using MessagingServerBusiness.Interfaces;
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

		public static CommandParameterPair UsersRequest(string username, params string[] value)
		{
			if (value.Length != 0)
			{
				return new CommandParameterPair("INVOP", "Invalid number of parameters for requesting info.");
			}
			return new CommandParameterPair("USERSRESP", JsonConvert.SerializeObject(Program.Clients));
		}

		public static CommandParameterPair RequestInfo(string username, params string[] value)
		{
			return new CommandParameterPair("INFORESP", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)));
		}

		public static CommandParameterPair BroadcastAfkUser(string username, params string[] value)
		{
			if (value.Length != 1)
				return new CommandParameterPair("INVOP", "Only one value (true/false) may be sent!");
			IMessagingClient host;
			if (!Program.Clients.TryGetValue(username, out host))
			{
				throw new NetworkInformationException();
			}
			foreach (IMessagingClient client in Program.Clients.Values)
			{
				if (client.UserName == username)
				{
					if (value[0] == "false")
						client.Client.IsOnline = true;
					else
						client.Client.IsOnline = false;
				}
				client.SendCommand(new CommandParameterPair("AFKUSER", username, value[0]));
			}
			return null;
		}

		public static CommandParameterPair SetStatus(string username, params string[] value)
		{
			if(value.Length != 1)
				return new CommandParameterPair("INVOP", "Only your status is needed");
			IMessagingClient host;
			if (!Program.Clients.TryGetValue(username, out host))
			{
				throw new NetworkInformationException();
			}
			host.Client.Status = value[0];
			foreach (IMessagingClient client in Program.Clients.Values)
			{
				client.Alert(String.Format("{0} has changed their status to {1}", username, value[0]), 2);
			}
			return null;
		}
	}
}
