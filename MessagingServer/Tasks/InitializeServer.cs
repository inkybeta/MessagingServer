using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagingServer.ClientManagementTasks;
using MessagingServer.Management;
using MessagingServerBusiness;
using Newtonsoft.Json;

namespace MessagingServer.Tasks
{
	public class InitializeServer
	{
		/// <summary>
		/// Initializes the fields in the main program
		/// </summary>
		public static void InitializeFields()
		{
			Program.ServerSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			Program.ServerProperties = new ConcurrentDictionary<string, string>();
			Program.ServerCommands = new ConcurrentDictionary<string, ServerCommand>();
			Program.Clients = new ConcurrentDictionary<string, UserClientService>();
			Program.InitializeCommands = new ConcurrentDictionary<string, InitializeCommand>();
			Program.ClientCommands = new ConcurrentDictionary<string, ClientCommand>();
			Program.ClientThreads = new ConcurrentDictionary<string, Thread>();
			Program.AcceptThreads = new ConcurrentBag<Thread>();
			Program.AnonymousThreads = new ConcurrentDictionary<string, Thread>();
		}

		/// <summary>
		/// Request for the properties of the file.
		/// </summary>
		public static void LoadProperites()
		{
			try
			{
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
				Program.ServerProperties =
					new ConcurrentDictionary<string, string>(
						Program.ServerProperties.Concat(
							JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName))));
			}
			catch (JsonException exception)
			{
				Console.WriteLine("Error: {0}", exception.Data);
				Console.WriteLine("Using default values.");
				Program.ServerProperties.TryAdd("SSLENABLED", "false");
				Program.ServerProperties.TryAdd("SERVERVENDOR", "inkynet");
				Console.WriteLine("An invalid file was found. Using a temporary file.");
			}
		}

		public static void LoadThreads()
		{
			//Add possible init commands
			Program.InitializeCommands.TryAdd("CONNECT", ServerCommandManagement.Connect);
			Program.InitializeCommands.TryAdd("INFOREQ", ServerCommandManagement.RequestInfo);

			//Add possible server commands
			Program.ClientCommands.TryAdd("SEND", ClientCommandManagement.BroadcastMessage);
			Program.ClientCommands.TryAdd("INFOREQ", ClientCommandManagement.RequestInfo);

			Console.WriteLine("What port should the server be bound to? (2015 is default)");
			int port;
			if (!Int32.TryParse(Console.ReadLine(), out port))
				port = 2015;
			Console.WriteLine("The server has started on {0}", port);

			//Start the socket
			Program.ServerSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
			Program.ServerSocket.Listen(Int32.MaxValue);

			Console.WriteLine("How many threads should be allocated to accepting new clients? (Default is 2)");
			int acceptThreads;
			if (!Int32.TryParse(Console.ReadLine(), out acceptThreads))
				acceptThreads = 2;

			for (int i = 0; i < acceptThreads; i++)
			{
				Thread thread = new Thread(ClientManagement.AcceptNewClients);
				Program.AcceptThreads.Add(thread);
			}
		}

		public static void Start()
		{
			InitializeFields();
			LoadProperites();
			LoadThreads();
		}
	}
}
