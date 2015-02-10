using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Management;
using MessagingServer.Models;
using MessagingServer.Utilities;
using MessagingServerBusiness;
using MessagingServerBusiness.Interfaces;
using MessagingServerCore;
using Newtonsoft.Json;

namespace MessagingServer.Commands
{
	public class ServerCommands
	{
		public static void Connect(Socket clientSocket, params string[] parameters)
		{
			if (parameters.Length != 2)
			{
				SocketUtilities.SendInvalid(clientSocket, "Parameters not formatted correctly");
				clientSocket.Close();
				return;
			}
			string username = parameters[0];
			string client = parameters[1];

			if (Program.Clients.ContainsKey(username) | username.Trim().ToLower() == "system" | username.Length > 15)
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

			foreach(IMessagingClient sclient in Program.Clients.Values)
				sclient.Alert(String.Format("{0} has connected", username), 3);

			Program.Clients.TryAdd(username, service);
			Thread thread = new Thread(ClientManagement.ManageClient);
			thread.Start(service);
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void SecureConnect(Socket clientSocket, params string[] value)
		{
			if (Program.ServerProperties["SSLENABLED"] == "false")
			{
				SocketUtilities.SendInvalid(clientSocket, "SSL isn't enabled on this server");
				clientSocket.Close();
				return;
			}
			if (value.Length != 2)
			{
				SocketUtilities.SendInvalid(clientSocket, "Incorrect number of parameters");
				clientSocket.Close();
				return;
			}
			string username = value[0];
			string client = value[1];

			if (Program.Clients.ContainsKey(username))
			{
				var message = String.Format("INVALIDUN {0}", username);
				var fullMessage = Encoding.UTF8.GetBytes(message);
				clientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
				clientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
				clientSocket.Close();
				return;
			}
			string certFile;
			if (!Program.ServerDependencies.TryGetValue("SSLCERT", out certFile))
			{
				SocketUtilities.SendInvalid(clientSocket, "SSL isn't properly supported on this server");
				clientSocket.Close();
				return;
			}
			string password;
			if (!Program.ServerDependencies.TryGetValue("SSLPASS", out password))
			{
				SocketUtilities.SendInvalid(clientSocket, "SSL isn't properyly supported on this serverserver");
				clientSocket.Close();
				return;
			}
			var service = new UserClientService(new SecureClient(clientSocket, certFile, username, client, new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>(), DateTime.UtcNow, password), true);
			service.SendCommand(new CommandParameterPair("CONNECTED"));
			Program.Clients.TryAdd(username, service);
			var thread = new Thread(ClientManagement.ManageClient);
			thread.Start(service);
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void RequestInfo(Socket clientSocket, params string[] value)
		{
			if (value.Length != 0)
			{
				SocketUtilities.SendInvalid(clientSocket, "INFOREQ should not send parameters");
				clientSocket.Close();
				return;
			}
			byte[] json =
				Encoding.UTF8.GetBytes(String.Format("INFORESP {0}",
					JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))));
			clientSocket.Send(BitConverter.GetBytes(json.Length), 4, SocketFlags.None);
			clientSocket.Send(json, json.Length, SocketFlags.None);
			Thread thread = new Thread(ClientManagement.ManageAnonymous);
			Guid guid = Guid.NewGuid();
			AnonymousThread athread = new AnonymousThread(guid.ToString(), clientSocket);
			thread.Start(athread);
			Program.AnonymousThreads.TryAdd(guid.ToString(), thread);
		}
	}
}
