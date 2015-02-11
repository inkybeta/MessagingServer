using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MessagingServer.Tasks;
using MessagingServer.Utilities;
using MessagingServerBusiness.Interfaces;
using MessagingServerCore;

namespace MessagingServer
{
	public delegate CommandParameterPair ClientCommand(string username, params string[] parameters);
	public delegate string ServerCommand(params string[] input);
	public delegate void InitializeCommand(TcpClient clientSocket, params string[] value);

	internal class Program
	{
		internal static TcpListener ServerSocket { get; set; }


		// The bag of threads for managing clients
		internal static ConcurrentBag<Thread> AcceptThreads { get; set; }
		internal static ConcurrentDictionary<string, Thread> AnonymousThreads { get; set; }
		internal static ConcurrentDictionary<string, Thread> ClientThreads { get; set; }

		internal static ConcurrentDictionary<string, string> ServerProperties { get; set; }
		internal static ConcurrentDictionary<string, string> ServerDependencies { get; set; }
		internal static ConcurrentDictionary<string, ServerCommand> ServerCommands { get; set; }
		internal static ConcurrentDictionary<string, InitializeCommand> InitializeCommands { get; set; }
		internal static ConcurrentDictionary<string, ClientCommand> ClientCommands { get; set; }
		internal static volatile int ServerState = 1;

		internal static ConcurrentDictionary<string, IMessagingClient> Clients { get; set; }

		private static void Main(string[] args)
		{
			//Initialize the server
			InitializeServer.Start();
			
			Console.WriteLine("Server is starting");
			foreach (Thread thread in AcceptThreads)
			{
				thread.Start();
			}
			Console.WriteLine("Server has been started");
			ConsoleUtilities.PrintInformation("Go to http://messaging.explodingbytes.com/console/keys for more information on what to do.");
			ConsoleUtilities.PrintInformation("Press 'q' to stop the server.");
			while (true)
			{
				var key = Console.ReadKey();
				Console.WriteLine();
				if (key == new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))
				{
					ServerState = 0;
					ConsoleUtilities.PrintInformation("Server is stopping");
					ServerSocket.Stop();
					break;
				}
				if (key == new ConsoleKeyInfo('u', ConsoleKey.U, false, false, false))
				{
					ServerState = 2;
					byte[] bytes = Encoding.UTF8.GetBytes("INFOREQ");
					for (int i = 0; i < 2; i++)
					{
						Socket sendSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						sendSocket.Connect(IPAddress.Loopback, 2015);
						sendSocket.SendTo(BitConverter.GetBytes(bytes.Length), 0, 4, SocketFlags.None,
							new IPEndPoint(IPAddress.Loopback, 2015));
						sendSocket.SendTo(bytes, 0, bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 2015));
						sendSocket.Close();
					}
					Thread.Sleep(1000);
					Console.WriteLine("Users:");
					foreach (IMessagingClient client in Clients.Values)
					{
						Console.WriteLine("Username: {0, 15} | Type: {1, 15} | IsOnline: {2, 15}", client.UserName, client.Client.ClientType, client.IsAfk);
					}
					ServerThread.ResumeThreads();
				}
				if (key == new ConsoleKeyInfo('c', ConsoleKey.C, false, false, false))
				{
					ServerState = 2;
					byte[] bytes = Encoding.UTF8.GetBytes("INFOREQ");
					for (int i = 0; i < 2; i++)
					{
						Socket sendSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						sendSocket.Connect(IPAddress.Loopback, 2015);
						sendSocket.SendTo(BitConverter.GetBytes(bytes.Length), 0, 4, SocketFlags.None,
							new IPEndPoint(IPAddress.Loopback, 2015));
						sendSocket.SendTo(bytes, 0, bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 2015));
						sendSocket.Close();
					}
					Thread.Sleep(1000);
					string command = Console.ReadLine();
					var parameters = new List<string>();
					while (true)
					{
						string var = Console.ReadLine();
						if (String.IsNullOrEmpty(var))
							break;
						parameters.Add(var);
					}
					var pair = new CommandParameterPair(command, parameters.ToArray());
					foreach (IMessagingClient client in Clients.Values)
					{
						client.SendCommand(pair);
					}
					ServerThread.ResumeThreads();
				}
			}

			foreach (IMessagingClient service in Clients.Values)
			{
				service.SendShutdown("The server is shutting down");
			}

			foreach (IMessagingClient service in Clients.Values)
			{
				service.Abort("The server is shutting down.");
			}
			Thread.Sleep(10000);
			Console.WriteLine("You may exit safely at this point by ending the process");
			// Clean up code
			foreach (KeyValuePair<string, Thread> thread in ClientThreads)
			{
				thread.Value.Abort();
				thread.Value.Join();
			}
			foreach (KeyValuePair<string, Thread> thread in AnonymousThreads)
			{
				thread.Value.Abort();
				thread.Value.Join();
			}
			Console.WriteLine("Server has stopped. Press any key to exit");
			Console.ReadKey();
			Environment.Exit(0);
		}

		public static void Disconnect(ThreadType type, string username)
		{
			try
			{
				if (type == ThreadType.AnonymousThread)
				{
					Thread value;
					AnonymousThreads.TryRemove(username, out value);
					value.Join();
				}
				if (type == ThreadType.ClientThread)
				{
					Thread value;
					ClientThreads.TryRemove(username, out value);
					IMessagingClient client;
					Clients.TryRemove(username, out client);
					foreach (IMessagingClient sclient in Clients.Values)
					{
						sclient.Alert(String.Format("{0} has diconnected", username), 3);
					}
					value.Join();
				}
			}
			catch (Exception e)
			{
				Guid guid = Guid.NewGuid();
				ConsoleUtilities.PrintCritical("A major server error has occured. Report to developer immediately. Writing to file {0}.error", guid);
				StreamWriter writer = new StreamWriter(String.Format("{0}.error", guid));
				writer.WriteLine(e.Message);
				foreach (var i in e.Data)
				{
					writer.WriteLine("{0}", i);
				}
			}
		}
	}

	public enum ThreadType
	{
		AnonymousThread,
		ClientThread
	}
}
