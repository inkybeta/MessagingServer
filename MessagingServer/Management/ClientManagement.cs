using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Utilities;
using MessagingServerBusiness.Interfaces;
using MessagingServerCore;

namespace MessagingServer.Management
{
	class ClientManagement
	{
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
						CommandParameterPair classic = command(client.UserName, message.Parameters);
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
				catch (ObjectDisposedException e)
				{
					ConsoleUtilities.PrintWarning("User {0} has disconnected. Data: {1}", client.Client.UserName, e.Message);
					Program.Disconnect(ThreadType.ClientThread, client.UserName);
					return;
				}
				catch (IOException e)
				{
					ConsoleUtilities.PrintWarning("User {0} has disconnected. Data: {1}", client.Client.UserName, e.Message);
					Program.Disconnect(ThreadType.ClientThread, client.UserName);
					return;
				}
				catch (SocketException e)
				{
					ConsoleUtilities.PrintWarning("User {0} has disconnected. Data: {1}", client.Client.UserName, e.Message);
					Program.Disconnect(ThreadType.ClientThread, client.UserName);
					return;
				}
				catch (NetworkInformationException e)
				{
					ConsoleUtilities.PrintWarning("A user has disconnected {0}", e.Message);
					client.Disconnect("An unknown error has occurred.");
					Program.Disconnect(ThreadType.ClientThread, client.Client.UserName);
					return;
				}
			}
		}
	}
}
