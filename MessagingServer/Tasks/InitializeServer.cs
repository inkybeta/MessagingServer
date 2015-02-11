using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MessagingServer.Commands;
using MessagingServer.Management;
using MessagingServer.Utilities;
using MessagingServerBusiness.Interfaces;
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
			Console.WriteLine("Setting up the messaging server...");
			Program.ServerProperties = new ConcurrentDictionary<string, string>();
			Program.ServerDependencies = new ConcurrentDictionary<string, string>();
			Program.ServerCommands = new ConcurrentDictionary<string, ServerCommand>();
			Program.Clients = new ConcurrentDictionary<string, IMessagingClient>();
			Program.InitializeCommands = new ConcurrentDictionary<string, InitializeCommand>();
			Program.ClientCommands = new ConcurrentDictionary<string, ClientCommand>();
			Program.ClientThreads = new ConcurrentDictionary<string, Thread>();
			Program.AcceptThreads = new ConcurrentBag<Thread>();
			Program.AnonymousThreads = new ConcurrentDictionary<string, Thread>();
			Console.WriteLine("Server has been initialized.");
			Console.WriteLine("This is a messaging server v1.0a");
			ConsoleUtilities.PrintInformation("Bugs can be reported at http://messaging.explodingbytes.com/bugs/report");
			ConsoleUtilities.PrintInformation("Documentation can be found on http://messaging.explodingbytes.com/documentation");
		}

		/// <summary>
		/// Request for the properties of the file.
		/// </summary>
		public static void LoadProperites()
		{
			try
			{
				Console.WriteLine("The server is starting");
				ConsoleUtilities.PrintRequest("Type the name of the file that holds the server properties");

				//Read the file of the properties
				var file = Console.ReadLine();
				string fileName = String.IsNullOrEmpty(file) ? "properties.json" : file;
				if (!File.Exists(fileName))
				{
					StreamWriter writer = new StreamWriter(File.Create(fileName));
					writer.Write("{" +
					             "'SSLENABLED':'false'," +
					             "'SERVERVENDOR': 'inkynet'" +
								 "'SERVERNAME' : 'inkynettest'" +
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
			}
			Console.WriteLine("The server has been initiated with the following properties:");
			Console.WriteLine(JsonConvert.SerializeObject(Program.ServerProperties));
		}

		public static void LoadDependencies()
		{
			try
			{
				ConsoleUtilities.PrintRequest(
					"Enter the name of the file that holds the server dependencies.");
				ConsoleUtilities.PrintInformation(
					"Go to http://messaging.explodingbytes.com/documentation/dependencies to find out more");
				var file = Console.ReadLine();
				string fileName = String.IsNullOrEmpty(file) ? "dependencies.json" : file;
				if (!File.Exists(fileName))
				{
					StreamWriter writer = new StreamWriter(File.Create(fileName));
					writer.Write("{}");
					writer.Close();
				}
				Program.ServerDependencies =
					new ConcurrentDictionary<string, string>(
						Program.ServerDependencies.Concat(
							JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(fileName))));
			}

			catch (JsonException exception)
			{
				ConsoleUtilities.PrintCritical("Error: {0}", exception.Data);
				ConsoleUtilities.PrintCritical("Using default values.");
			}
			Console.WriteLine("The server has been initiated with the following dependencies:");
			Console.WriteLine(JsonConvert.SerializeObject(Program.ServerDependencies));
		}

		public static void LoadThreads()
		{
			//Add possible init commands
			Program.InitializeCommands.TryAdd("CONNECT", ServerCommands.Connect);
			Program.InitializeCommands.TryAdd("INFOREQ", ServerCommands.RequestInfo);

			//Add possible server commands
			Program.ClientCommands.TryAdd("SEND", ClientCommands.BroadcastMessage);
			Program.ClientCommands.TryAdd("INFOREQ", ClientCommands.RequestInfo);
			Program.ClientCommands.TryAdd("AFK", ClientCommands.BroadcastAfkUser);
			Program.ClientCommands.TryAdd("USERSREQ", ClientCommands.UsersRequest);
			Program.ClientCommands.TryAdd("STATUS", ClientCommands.SetStatus);

			ConsoleUtilities.PrintRequest("What port should the server be bound to? (2015 is default)");
			int port;
			if (!Int32.TryParse(Console.ReadLine(), out port))
				port = 2015;
			Console.WriteLine("The server has started on {0}", port);
			Program.ServerSocket = new TcpListener(new IPEndPoint(IPAddress.Loopback, 2015));
			Program.ServerSocket.Start();

			ConsoleUtilities.PrintRequest("How many threads should be allocated to accepting new clients? (Default is 2)");
			int acceptThreads;
			if (!Int32.TryParse(Console.ReadLine(), out acceptThreads))
				acceptThreads = 2;

			for (int i = 0; i < acceptThreads; i++)
			{
				Thread thread = new Thread(AcceptClientManagement.AcceptNewClients);
				Program.AcceptThreads.Add(thread);
			}
		}

		public static void Start()
		{
			InitializeFields();
			LoadProperites();
			LoadDependencies();
			LoadThreads();
		}
	}
}
