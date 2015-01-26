using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessagingServerBusiness;
using MessagingServerCore;
using Newtonsoft.Json;

namespace MessagingServer.Tasks
{
	public static class ClientManagement
	{
		public static void AcceptNewClients()
		{
			Console.WriteLine("Thread started");
			while (true)
			{
				Socket clientSocket = Program.ServerSocket.Accept();
				Console.WriteLine("Connection has been accepted from {0}", clientSocket.RemoteEndPoint.ToString());
				int messageLength;
				string message;
				using (var stream = new MemoryStream())
				{
					while (stream.Length != 4)
					{
						var buffer = new byte[4 - stream.Length];
						var bytesRecieved = clientSocket.Receive(buffer);
						stream.Write(buffer, 0, bytesRecieved);
					}
					messageLength = BitConverter.ToInt32(stream.ToArray(), 0);
				}

				//Recieve the message itself
				var bytes = new byte[messageLength];

				using (var stream = new MemoryStream())
				{
					var buffer = new byte[512];
					while (stream.Length != messageLength)
					{
						var bytesRecieved = clientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
						stream.Write(buffer, 0, bytesRecieved);
					}

					message = Encoding.UTF8.GetString(stream.ToArray());
				}

				string[] messageAndValue = message.Split(' ');
				if (messageAndValue.Length > 2)
				{
					SendError(clientSocket, "The command was formatted incorrectly");
					clientSocket.Close();
					continue;
				}

				string command = messageAndValue[0];
				string value = "";

				value = messageAndValue.Length == 2 ? messageAndValue[1] : "";

				if (!Program.InitializeCommands.ContainsKey(command))
				{
					SendInvalid(clientSocket, "The command was not found");
					clientSocket.Close();
					continue;
				}

				InitializeCommand execute;
				if (Program.InitializeCommands.TryGetValue(command, out execute))
					execute(clientSocket, value);
				else
					SendError(clientSocket, "Unable to find command (Concurrency Issues)");
				if (Program.ServerState == 0)
					Console.WriteLine("Thread has been ended");
					return;
			}
		}

		private static void SendError(Socket clientSocket, string message)
		{
			string fullMessage = String.Format("ERROR {0}", message);
			byte[] byteMessage = Encoding.UTF8.GetBytes(fullMessage);
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Send(messageLength, 4, SocketFlags.None);
			clientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		private static void SendInvalid(Socket clientSocket, string message)
		{
			string fullMessage = String.Format("INVOP {0}", message);
			byte[] byteMessage = Encoding.UTF8.GetBytes(fullMessage);
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Send(messageLength, 4, SocketFlags.None);
			clientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		public static void Connect(Socket clientSocket, string value)
		{
			string[] parameters = value.Split('&');
			if (parameters.Length != 2)
			{
				SendInvalid(clientSocket, "Parameters not formatted correctly");
				clientSocket.Close();
				return;
			}
			string username = Uri.UnescapeDataString(parameters[0]);
			string client = Uri.UnescapeDataString(parameters[1]);
			if (Program.Clients.ContainsKey(username))
			{
				var message = String.Format("INVALIDUN {0}", username);
				var fullMessage = Encoding.UTF8.GetBytes(message);
				clientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
				clientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
				clientSocket.Close();
				return;
			}
			UserClientService service = new UserClientService(new UserClient(username, client, clientSocket, new ConcurrentDictionary<string, string>(), new ConcurrentDictionary<string, string>()));
			byte[] connected = Encoding.UTF8.GetBytes("CONNECTED");
			clientSocket.Send(BitConverter.GetBytes(connected.Length), 4, SocketFlags.None);
			clientSocket.Send(connected, connected.Length, SocketFlags.None);
			Program.Clients.TryAdd(username, service);
			Thread thread = new Thread(new ParameterizedThreadStart(ManageThread));
			Program.ClientThreads.TryAdd(username, thread);
		}

		public static void RequestInfo(Socket clientSocket, string value)
		{
			if (value != "")
			{
				SendInvalid(clientSocket, "INFOREQ should not send parameters");
				clientSocket.Close();
				return;
			}
			byte[] json = Encoding.UTF8.GetBytes(String.Format("INFORESP {0}", JsonConvert.SerializeObject(Program.ServerProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))));
			clientSocket.Send(BitConverter.GetBytes(json.Length), 4, SocketFlags.None);
			clientSocket.Send(json, json.Length, SocketFlags.None);
			clientSocket.Close();
		}

		public static void ManageThread(object _service)
		{
			UserClientService service = (UserClientService) _service;
			while (true)
			{
				if (Program.ServerState == 0)
				{
					service.SendShutdown("The server is shutting down.");
					return;
				}
			}
		}
	}
}
