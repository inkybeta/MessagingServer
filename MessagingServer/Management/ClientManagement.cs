using System;
using System.Net.Sockets;
using System.Threading;
using MessagingServer.Tasks;
using MessagingServerBusiness;
using MessagingServerCore;

namespace MessagingServer.ClientManagementTasks
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
					{
						execute(clientSocket, message.Parameters);
						continue;
					}
					else
					{
						SocketManagement.SendError(clientSocket, "Unable to find command (Concurrency Issues)");
					}
					if (Program.ServerState == 0)
					{
						Console.WriteLine("Thread has been ended");
						return;
					}
				}
				catch (ThreadAbortException e)
				{
					Console.WriteLine("Thread has been stopped {0}", e.Data);
					return;
				}
				catch (SocketException e)
				{
					Console.WriteLine("Improper disconect.");
					continue;
				}
			}
		}

		public static void ManageClient(object service)
		{
			UserClientService client = (UserClientService) service;
			while (true)
			{
				try
				{
					if (Program.ServerState == 0)
					{
						client.SendShutdown("The server is shutting down.");
						return;
					}
					CommandParameterPair message = client.RecieveMessage();
					if (message == null || message.Command == null)
					{
						client.SendInvalid("Message was formatted incorrectly");
					}
					Console.WriteLine("Command {0} was sent from the user {1}", message.Command, client.Client.UserName);
					ClientCommand command;
					if (Program.ClientCommands.TryGetValue(message.Command, out command))
					{
						CommandParameterPair classic = command(client.Client.UserName, message.Parameters);
						if (classic == null)
							continue;
						client.SendCommand(classic);
					}
					else
						client.SendInvalid(String.Format("Unknown command: {0}", message.Command));
					if (Program.ServerState == 0)
					{
						client.SendShutdown("The server is shutting down.");
						return;
					}
				}

				catch (SocketException e)
				{
					Console.WriteLine("User {0} has disconnected", client.Client.UserName);
					Program.Disconnect(ThreadType.ClientThread, client.Client.UserName);
					return;
				}
			}
		}

		public static void ManageAnonymous(object socket)
		{
			try
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
			catch (SocketException e)
			{
				
			}
		}
	}
}
