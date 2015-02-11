using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagingServer.Models;
using MessagingServer.Utilities;
using MessagingServerCore;

namespace MessagingServer.Management
{
	public static class AcceptClientManagement
	{
		public static void AcceptNewClients()
		{
			Console.WriteLine("Thread started");
			while (true)
			{
				try
				{
					TcpClient tcpClient = Program.ServerSocket.AcceptTcpClient();
					if (Program.ServerState == 0)
					{
						ConsoleUtilities.PrintCritical("Thread has been ended {0}", DateTime.UtcNow);
						return;
					}
					if (Program.ServerState == 2)
					{
						Thread.Sleep(Timeout.Infinite);
					}
					NetworkStream clientStream = tcpClient.GetStream();
					ConsoleUtilities.PrintCommand("Connection has been accepted from {0}", tcpClient.Client.RemoteEndPoint);

					if (Program.AnonymousThreads.ContainsKey(tcpClient.Client.RemoteEndPoint.ToString()))
					{
						SocketUtilities.SendInvalid(clientStream, "Your network is already connected. Try again later.");
						tcpClient.Close();
						continue;
					}

					int messageLength = SocketUtilities.RecieveMessageLength(clientStream);
					if (messageLength == -1)
					{
						ConsoleUtilities.PrintCommand("Connection has been closed for {0}", tcpClient.Client.RemoteEndPoint);
						tcpClient.Close();
						continue;
					}
					string stringmessage = SocketUtilities.RecieveMessage(clientStream, messageLength);
					if (stringmessage == null)
					{
						tcpClient.Close();
						continue;
					}

					CommandParameterPair message = MessageUtilites.DecodeMessage(stringmessage);
					if (message == null)
					{
						SocketUtilities.SendInvalid(clientStream, "The message was formatted incorrectly.");
						tcpClient.Close();
						continue;
					}

					if (!Program.InitializeCommands.ContainsKey(message.Command))
					{
						SocketUtilities.SendInvalid(clientStream, "The command was not found");
						tcpClient.Close();
						continue;
					}

					InitializeCommand execute;
					if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
					{
						execute(tcpClient, message.Parameters);
						continue;
					}
					SocketUtilities.SendError(clientStream, "Unable to find command (Concurrency Issues)");
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
				catch (InvalidOperationException e)
				{
					ConsoleUtilities.PrintCritical("Accept thread has been terminated {0}", e.Message);
					return;
				}
				catch (ThreadInterruptedException)
				{
					Console.WriteLine("Resuming accepting threads");
				}
				catch (IOException e)
				{
					ConsoleUtilities.PrintWarning("Impropert disconnect. {1}", e.Message);
				}
				catch (SocketException e)
				{
					ConsoleUtilities.PrintWarning("Improper disconnect. Data: {0}", e.Data);
				}
			}
		}

		public static void ManageAnonymous(object socket)
		{
			AnonymousThread client = (AnonymousThread) socket;
			try
			{
				var clientStream = client.Client.GetStream();
				if (Program.ServerState == 0)
				{
					SocketUtilities.SendShutdown(clientStream, "The server is shutting down", "0");
					clientStream.Close();
					return;
				}
				string smessage = SocketUtilities.RecieveMessage(clientStream, SocketUtilities.RecieveMessageLength(clientStream));
				if (smessage == null)
				{
					Program.Disconnect(ThreadType.AnonymousThread, client.Guid);
					return;
				}
				CommandParameterPair message = MessageUtilites.DecodeMessage(smessage);
				InitializeCommand execute;
				if (Program.InitializeCommands.TryGetValue(message.Command, out execute))
				{
					execute(client.Client, message.Parameters);
				}
				else
				{
					SocketUtilities.SendError(clientStream, "Unable to find command (Concurrency Issues)");
					clientStream.Close();
				}
			}
			catch (SocketException e)
			{
				if (client.Client.Client.RemoteEndPoint == null)
				{
					Console.WriteLine("A severe error has occurred with the clientStream. Data: {0}", e.Data);
					return;
				}
				Console.WriteLine("Error: A clientStream has disconnected from {0}. ", (client.Client.Client.RemoteEndPoint as IPEndPoint).Address);
			}
		}
	}
}
