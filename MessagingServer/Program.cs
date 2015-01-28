using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MessagingServer.Tasks;
using MessagingServerBusiness;
using Newtonsoft.Json;

namespace MessagingServer
{
	public delegate void ServerCommand(string input);
	public delegate void InitializeCommand(Socket clientSocket, params string[] value);

	class Program
	{
		internal static Socket ServerSocket { get; set; }

		internal static ConcurrentBag<Thread> AcceptThreads { get; set; }
		internal static ConcurrentBag<Thread> AnonymousThreads { get; set; } 
		internal static ConcurrentBag<Thread> ClientThreads { get; set; } 

		internal static ConcurrentDictionary<string, string> ServerProperties { get; set; }
		internal static ConcurrentDictionary<string, ServerCommand> ServerCommands { get; set; }
		internal static ConcurrentDictionary<string, InitializeCommand> InitializeCommands { get; set; }
		internal static volatile int ServerState = 1;


		internal static ConcurrentDictionary<string, UserClientService> Clients { get; set; }

		static void Main(string[] args)
		{
			//Initialize the server
			ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			ServerProperties = new ConcurrentDictionary<string, string>();
			ServerCommands = new ConcurrentDictionary<string, ServerCommand>();
			Clients = new ConcurrentDictionary<string, UserClientService>();
			InitializeCommands = new ConcurrentDictionary<string, InitializeCommand>();
			ClientThreads = new ConcurrentBag<Thread>();
			AcceptThreads = new ConcurrentBag<Thread>();
			AnonymousThreads = new ConcurrentBag<Thread>();

			Console.WriteLine("The server is starting");
			Console.WriteLine("Type the name of the file that holds the server properties");

			//Read the file of the properties
			var file = Console.ReadLine();
			string fileName = String.IsNullOrEmpty(file) ? "properties.json" : file;
			if (!File.Exists(fileName))
			{
				StreamWriter writer = new StreamWriter(File.Create(fileName));
				writer.Write("{" +
				             "'SSLENABLED':'false'," +
				             "'SERVERVENDOR': 'inkynet'" +
				             "}");
				writer.Close();
			}
			ServerProperties =
				new ConcurrentDictionary<string, string>(ServerProperties.Concat(JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName))));

			//Add possible init commands
			InitializeCommands.TryAdd("CONNECT", ServerCommandManagement.Connect);
			InitializeCommands.TryAdd("INFOREQ", ServerCommandManagement.RequestInfo);

			//Add possible server commands
			Console.WriteLine("What port should the server be bound to? (2015 is default)");
			int port;
			if (!Int32.TryParse(Console.ReadLine(), out port))
				port = 2015;
			Console.WriteLine("The server has started on {0}", port);

			//Start the socket
			ServerSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
			ServerSocket.Listen(Int32.MaxValue);

			Console.WriteLine("How many threads should be allocated to accepting new clients? (Default is 2)");
			int acceptThreads;
			if (!Int32.TryParse(Console.ReadLine(), out acceptThreads))
				acceptThreads = 2;

			for (int i = 0; i < acceptThreads; i++)
			{
				Thread thread = new Thread(ClientManagement.AcceptNewClients);
				AcceptThreads.Add(thread);
			}
			Console.WriteLine("Press q to stop the server");
			Console.WriteLine("Server is starting");
			foreach (Thread thread in AcceptThreads)
			{
				thread.Start();
			}
			Console.WriteLine("Server has been started");
			while (true)
			{
				if (Console.ReadKey() == new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))
				{
					ServerState = 0;
					Console.WriteLine();
					Console.WriteLine("Server is stopping");
					foreach (Thread thread in AcceptThreads)
					{
						byte[] bytes = Encoding.UTF8.GetBytes("INFOREQ");
						Socket sendSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
						sendSocket.Connect(IPAddress.Loopback, 2015);
						sendSocket.SendTo(BitConverter.GetBytes(bytes.Length), 0, 4, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 2015));
						sendSocket.SendTo(bytes, 0, bytes.Length, SocketFlags.None, new IPEndPoint(IPAddress.Loopback, 2015));
						thread.Join();
					}
					break;
				}
			}
			foreach (Thread thread in ClientThreads)
			{
				thread.Join();
			}
			foreach (Thread thread in AnonymousThreads)
			{
				thread.Abort();
				thread.Join();
			}
			Console.WriteLine("Server has stopped. Press any key to exit");
			Console.ReadKey();
			Environment.Exit(0);
		}
	}
}
