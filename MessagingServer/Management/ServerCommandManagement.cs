using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Management;
using MessagingServerBusiness;
using MessagingServerCore;
using Newtonsoft.Json;

namespace MessagingServer.Tasks
{
	public class ServerCommandManagement
	{
		public static void Connect(Socket clientSocket, params string[] parameters)
		{
			if (parameters.Length != 2)
			{
				SocketManagement.SendInvalid(clientSocket, "Parameters not formatted correctly");
				clientSocket.Close();
				return;
			}
			string username = parameters[0];
			string client = parameters[1];

			if (Program.Clients.ContainsKey(username))
			{
				var message = String.Format("INVALIDUN {0}", username);
				var fullMessage = Encoding.UTF8.GetBytes(message);
				clientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
				clientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
				clientSocket.Close();
				return;
			}

			var service = new UserClientService(new UserClient(username, client, clientSocket, new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>(), DateTime.UtcNow));

			byte[] connected = Encoding.UTF8.GetBytes("CONNECTED");
			clientSocket.Send(BitConverter.GetBytes(connected.Length), 4, SocketFlags.None);
			clientSocket.Send(connected, connected.Length, SocketFlags.None);

			Program.Clients.TryAdd(username, service);
			Thread thread = new Thread(ClientManagement.ManageClient);
			thread.Start(service);
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void RequestInfo(Socket clientSocket, params string[] value)
		{
			if (value.Length != 0)
			{
				SocketManagement.SendInvalid(clientSocket, "INFOREQ should not send parameters");
				clientSocket.Close();
				return;
			}
			byte[] json = Encoding.UTF8.GetBytes(String.Format("INFORESP {0}", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))));
			clientSocket.Send(BitConverter.GetBytes(json.Length), 4, SocketFlags.None);
			clientSocket.Send(json, json.Length, SocketFlags.None);
			Thread thread = new Thread(ClientManagement.ManageAnonymous);
			thread.Start(clientSocket);
			Guid guid = new Guid();
			Program.AnonymousThreads.TryAdd(guid.ToString(), thread);
		}
	}
}
