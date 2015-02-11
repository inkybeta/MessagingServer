using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
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
		public static void Connect(TcpClient tcpClient, params string[] parameters)
		{
			if (!tcpClient.Connected)
			{
				tcpClient.Close();
				return;
			}
			NetworkStream stream = tcpClient.GetStream();
			if (parameters.Length != 2)
			{
				SocketUtilities.SendInvalid(stream, "Parameters not formatted correctly");
				stream.Close();
				tcpClient.Close();
				return;
			}
			string username = parameters[0];
			string client = parameters[1];

			if (Program.Clients.ContainsKey(username) | username.Trim().ToLower() == "system" | username.Length > 15)
			{
				SocketUtilities.SendCommand(stream, new CommandParameterPair("INVALIDUN"));
				stream.Close();
				tcpClient.Close();
				return;
			}

			var service = new UserClientService(new UserClient(username, client, tcpClient, new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>(), DateTime.UtcNow));

			SocketUtilities.SendCommand(stream, new CommandParameterPair("CONNECTED"));

			ConsoleUtilities.PrintWarning("Got here!");
			foreach(IMessagingClient sclient in Program.Clients.Values)
				sclient.Alert(String.Format("{0} has connected", username), 3);
			ConsoleUtilities.PrintInformation("Connected {0}", username);
			Program.Clients.TryAdd(username, service);
			Thread thread = new Thread(ClientManagement.ManageClient);
			thread.Start(service);
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void SecureConnect(TcpClient tcpclient, params string[] value)
		{
			var stream = tcpclient.GetStream();
			if (Program.ServerProperties["SSLENABLED"] == "false")
			{
				SocketUtilities.SendInvalid(stream, "SSL isn't enabled on this server");
				tcpclient.Close();
				return;
			}
			if (value.Length != 2)
			{
				SocketUtilities.SendInvalid(stream, "Incorrect number of parameters");
				tcpclient.Close();
				return;
			}
			string username = value[0];
			string client = value[1];

			if (Program.Clients.ContainsKey(username))
			{
				SocketUtilities.SendCommand(stream, new CommandParameterPair("INVALIDUN {0}", username));
				tcpclient.Close();
				return;
			}
			string certFile;
			if (!Program.ServerDependencies.TryGetValue("SSLCERT", out certFile))
			{
				SocketUtilities.SendInvalid(stream, "SSL isn't properly supported on this server");
				tcpclient.Close();
				return;
			}
			string password;
			if (!Program.ServerDependencies.TryGetValue("SSLPASS", out password))
			{
				SocketUtilities.SendInvalid(stream, "SSL isn't properyly supported on this serverserver");
				tcpclient.Close();
				return;
			}
			var service = new UserClientService(new SecureClient(stream, certFile, username, client, new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>(), DateTime.UtcNow, password), true);
			service.SendCommand(new CommandParameterPair("CONNECTED"));
			Program.Clients.TryAdd(username, service);
			var thread = new Thread(ClientManagement.ManageClient);
			thread.Start(service);
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void RequestInfo(TcpClient tcpClient, params string[] value)
		{
			var stream = tcpClient.GetStream();
			if (value.Length != 0)
			{
				SocketUtilities.SendInvalid(stream, "INFOREQ should not send parameters");
				tcpClient.Close();
				return;
			}
			SocketUtilities.SendCommand(stream, new CommandParameterPair("INFORESP",
				JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))));
			Thread thread = new Thread(AcceptClientManagement.ManageAnonymous);
			AnonymousThread athread = new AnonymousThread(tcpClient.Client.RemoteEndPoint.ToString(), tcpClient);
			thread.Start(athread);
			Program.AnonymousThreads.TryAdd(tcpClient.Client.RemoteEndPoint.ToString(), thread);
		}
	}
}
