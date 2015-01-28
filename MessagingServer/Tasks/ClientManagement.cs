using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
				try
				{
					if (Program.ServerState == 0)
					{
						Console.WriteLine("Thread has been ended");
						return;
					}
					Socket clientSocket = Program.ServerSocket.Accept();
					Console.WriteLine("Connection has been accepted from {0}", clientSocket.RemoteEndPoint);


					int messageLength = SocketManagement.RecieveMessageLength(clientSocket);
					string stringmessage = SocketManagement.RecieveMessage(clientSocket, messageLength);

					CommandParameterPair message = MessageManagement.RecieveMessage(stringmessage);
					if (message == null)
					{
						SocketManagement.SendInvalid(clientSocket, "The message was formatted incorrectly.");
						clientSocket.Close();
						continue;
					}

					if (!Program.InitializeCommands.ContainsKey(message.Command))
					{
						SocketManagement.SendInvalid(clientSocket, "The command was not found");
						clientSocket.Close();
						continue;
					}

					InitializeCommand execute;
					if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
						execute(clientSocket, message.Parameters);
					else
						SocketManagement.SendError(clientSocket, "Unable to find command (Concurrency Issues)");
					if (Program.ServerState == 0)
					{
						Console.WriteLine("Thread has been ended");
						return;
					}
				}
				catch (ThreadAbortException e)
				{
					return;
				}
			}
		}

		public static void ManageClient(object service)
		{
			UserClientService client = (UserClientService) service;
			while (true)
			{
				if (Program.ServerState == 0)
				{
					client.SendShutdown("The server is shutting down.");
					return;
				}
				CommandParameterPair message = client.RecieveMessage();
				ServerCommand command;
				if (Program.ServerCommands.TryGetValue(message.Command, out command))
				{
					string classic = command(message.Parameters);
					if (String.IsNullOrEmpty(classic))
					{
						continue;
					}
				}
				else
				{
					client.SendInvalid(String.Format("Unknown command: {0}", message.Command));
				}
				if (Program.ServerState == 0)
				{
					client.SendShutdown("The server is shutting down.");
					return;
				}
			}
		}

		public static void ManageAnonymous(object socket)
		{
			Socket client = (Socket) socket;
			if (Program.ServerState == 0)
			{
				SocketManagement.SendShutdown(client, "The server is shutting down", "0");
				client.Close();
				return;
			}
			string smessage = SocketManagement.RecieveMessage(client, SocketManagement.RecieveMessageLength(client));
			CommandParameterPair message = MessageManagement.RecieveMessage(smessage);
			InitializeCommand execute;
			if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
			{
				execute(client, message.Parameters);
			}
			else
			{
				SocketManagement.SendError(client, "Unable to find command (Concurrency Issues)");
				client.Close();
			}
		}
	}
}
