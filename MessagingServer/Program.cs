using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Tasks;
using MessagingServerBusiness.Interfaces;
using MessagingServerCore;

namespace MessagingServer
{
	public delegate CommandParameterPair ClientCommand(string username, params string[] parameters);
	public delegate string ServerCommand(params string[] input);
	public delegate void InitializeCommand(Socket clientSocket, params string[] value);

	internal class Program
	{
		internal static Socket ServerSocket { get; set; }


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
			
			Console.WriteLine("Press q to stop the server");
			Console.WriteLine("Server is starting");
			foreach (Thread thread in AcceptThreads)
			{
				thread.Start();
			}
			Console.WriteLine("Server has been started");

			while (true)
			{
				var key = Console.ReadKey();
				Console.WriteLine();
				if (key == new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))
				{
					ServerState = 0;
					Console.WriteLine("Server is stopping");
					ServerSocket.Close();
					break;
				}
				if (key == new ConsoleKeyInfo('u', ConsoleKey.U, false, false, false))
				{
					ServerState = 2;
					byte[] bytes = Encoding.UTF8.GetBytes("INFOREQ");
					for (int i = 0; i < 3; i++)
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
						Console.WriteLine("Username: {0} | Type: {1} | IsOnline: {2}", client.UserName, client.Client.ClientType, client.IsAfk);
					}
					ServerThread.ResumeThreads();
				}
			}

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
				value.Join();
			}
		}
	}

	public enum ThreadType
	{
		AnonymousThread,
		ClientThread
	}
}
