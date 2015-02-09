using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagingServer.Utilities;
using MessagingServerBusiness;
using MessagingServerCore;

namespace MessagingServer.Management
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


					int messageLength = SocketUtilities.RecieveMessageLength(clientSocket);
					string stringmessage = SocketUtilities.RecieveMessage(clientSocket, messageLength);

					CommandParameterPair message = MessageUtilites.RecieveMessage(stringmessage);
					if (message == null)
					{
						SocketUtilities.SendInvalid(clientSocket, "The message was formatted incorrectly.");
						clientSocket.Close();
						continue;
					}

					if (!Program.InitializeCommands.ContainsKey(message.Command))
					{
						SocketUtilities.SendInvalid(clientSocket, "The command was not found");
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
						SocketUtilities.SendError(clientSocket, "Unable to find command (Concurrency Issues)");
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
					Console.WriteLine("Improper disconnect. Data: {0}", e.Data);
					continue;
				}
			}
		}

		public static void ManageClient(object service)
		{
			IMessagingClient client = (IMessagingClient) service;
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
					Console.WriteLine("Command {0} was sent from the user {1} with parameters {2}", message.Command, client.UserName, message.Parameters);
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
					Console.WriteLine("User {0} has disconnected. Data: {1}", client.Client.UserName, e.Data);
					Program.Disconnect(ThreadType.ClientThread, client.Client.UserName);
					return;
				}
			}
		}

		public static void ManageAnonymous(object socket)
		{
			Socket client = (Socket)socket;
			try
			{
				if (Program.ServerState == 0)
				{
					SocketUtilities.SendShutdown(client, "The server is shutting down", "0");
					client.Close();
					return;
				}
				string smessage = SocketUtilities.RecieveMessage(client, SocketUtilities.RecieveMessageLength(client));
				CommandParameterPair message = MessageUtilites.RecieveMessage(smessage);
				InitializeCommand execute;
				if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
				{
					execute(client, message.Parameters);
				}
				else
				{
					SocketUtilities.SendError(client, "Unable to find command (Concurrency Issues)");
					client.Close();
				}
			}
			catch (SocketException e)
			{
				if (client.RemoteEndPoint == null)
				{
					Console.WriteLine("A severe error has occurred with the client. Data: {0}", e.Data);
					return;
				}
				Console.WriteLine("Error: A client has disconnected from {0}. ", (client.RemoteEndPoint as IPEndPoint).Address);
			}
		}
	}
}
