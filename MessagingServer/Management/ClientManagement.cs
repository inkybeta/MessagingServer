using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Models;
using MessagingServer.Utilities;
using MessagingServerBusiness.Interfaces;
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
						ConsoleUtilities.PrintCritical("Thread has been ended");
						return;
					}
					if (Program.ServerState == 2)
					{
						Thread.Sleep(Timeout.Infinite);
					}

					Socket clientSocket = Program.ServerSocket.Accept();
					Console.WriteLine("Connection has been accepted from {0}", clientSocket.RemoteEndPoint);


					int messageLength = SocketUtilities.RecieveMessageLength(clientSocket);
					if (messageLength == -1)
					{
						clientSocket.Close();
						continue;
					}
					string stringmessage = SocketUtilities.RecieveMessage(clientSocket, messageLength);
					if (stringmessage == null)
					{
						clientSocket.Close();
						continue;
					}

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
					if (Program.ServerState == 2)
					{
						Thread.Sleep(Timeout.Infinite);
					}
				}
				catch (ThreadAbortException e)
				{
					Console.WriteLine("Thread has been stopped {0}", e.Data.Values);
					return;
				}
				catch (ThreadInterruptedException)
				{
					Console.WriteLine("Resuming accepting threads");
				}
				catch (SocketException e)
				{
					ConsoleUtilities.PrintWarning("Improper disconnect. Data: {0}", e.Data);
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
					if (Program.ServerState == 2)
					{
						Thread.Sleep(Timeout.Infinite);
					}
					CommandParameterPair message = client.RecieveMessage();
					if (message == null)
					{
						Program.Disconnect(ThreadType.ClientThread, client.UserName);
						return;
					}
					var parameters = new StringBuilder();
					foreach (string param in message.Parameters)
					{
						parameters.Append(param + " ");
					}
					ConsoleUtilities.PrintCommand("Command {0} was sent from the user {1} with parameters '{2}'", message.Command,
						client.UserName,
						parameters);
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
				catch (ThreadAbortException)
				{
					return;
				}
				catch (ThreadInterruptedException)
				{
				}
				catch (SocketException e)
				{
					ConsoleUtilities.PrintWarning("User {0} has disconnected. Data: {1}", client.Client.UserName, e.Data);
					Program.Disconnect(ThreadType.ClientThread, client.Client.UserName);
					return;
				}
				catch (NetworkInformationException e)
				{
					ConsoleUtilities.PrintWarning("A user has disconnected");
					client.Disconnect("An unknown error has occurred.");
					Program.Disconnect(ThreadType.ClientThread, client.Client.UserName);
					return;
				}
			}
		}

		public static void ManageAnonymous(object socket)
		{
			AnonymousThread client = (AnonymousThread) socket;
			try
			{
				if (Program.ServerState == 0)
				{
					SocketUtilities.SendShutdown(client.Client, "The server is shutting down", "0");
					client.Client.Close();
					return;
				}
				string smessage = SocketUtilities.RecieveMessage(client.Client, SocketUtilities.RecieveMessageLength(client.Client));
				if (smessage == null)
				{
					Program.Disconnect(ThreadType.AnonymousThread, client.Guid);
					return;
				}
				CommandParameterPair message = MessageUtilites.RecieveMessage(smessage);
				InitializeCommand execute;
				if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
				{
					execute(client.Client, message.Parameters);
				}
				else
				{
					SocketUtilities.SendError(client.Client, "Unable to find command (Concurrency Issues)");
					client.Client.Close();
				}
			}
			catch (SocketException e)
			{
				if (client.Client.RemoteEndPoint == null)
				{
					Console.WriteLine("A severe error has occurred with the client. Data: {0}", e.Data);
					return;
				}
				Console.WriteLine("Error: A client has disconnected from {0}. ", (client.Client.RemoteEndPoint as IPEndPoint).Address);
			}
		}
	}
}
