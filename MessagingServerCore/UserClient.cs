using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MessagingServerCore.Interfaces;
using Newtonsoft.Json;

namespace MessagingServerCore
{
	public class UserClient : IClient
	{
		public string UserName { get; set; }
		[JsonIgnore]
		public string ClientType { get; set; }
		private string _status = "No status set";
		public string Status
		{
			get { return _status; }
			set { _status = value; }
		}
		private bool _isOnline = true;
		public bool IsOnline
		{
			get { return _isOnline; }
			set { _isOnline = value; }
		}
		[JsonIgnore]
		private Socket ClientSocket { get; set; }
		[JsonIgnore]
		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }
		[JsonIgnore]
		public ConcurrentDictionary<string, string> Properties { get; set; }
		[JsonIgnore]
		public DateTime TimeLastUsed { get; set; }

	    public UserClient(string userName, string clientType, Socket clientSocket, ConcurrentDictionary<string, string> groupAndRole, ConcurrentDictionary<string, string> properties, DateTime time)
	    {
		    UserName = userName;
		    ClientType = clientType;
		    ClientSocket = clientSocket;
		    GroupAndRole = groupAndRole;
		    Properties = properties;
		    TimeLastUsed = time;
	    }

		public void SendCommand(CommandParameterPair command)
		{
			if (command.ParameterLength == 0)
			{
				byte[] fullMessage = Encoding.UTF8.GetBytes(command.Command);
				ClientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
				ClientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
				return;
			}
			string smessage = EncodeMessage(command);
			byte[] byteMessage = Encoding.UTF8.GetBytes(smessage);
			ClientSocket.Send(BitConverter.GetBytes(byteMessage.Length), 4, SocketFlags.None);
			ClientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		public CommandParameterPair RecieveCommand()
		{
			while (true)
			{
				int messageLength;
				using (var stream = new MemoryStream())
				{
					while (stream.Length != 4)
					{
						var buffer = new byte[4 - stream.Length];
						int bytesRecieved = ClientSocket.Receive(buffer);
						stream.Write(buffer, 0, bytesRecieved);
						if (!CheckIfConnected())
							return null;
					}
					messageLength = BitConverter.ToInt32(stream.ToArray(), 0);
				}
				using (var stream = new MemoryStream())
				{
					while (stream.Length != messageLength)
					{
						var buffer = new byte[512];
						int bytesRecieved = ClientSocket.Receive(buffer);
						stream.Write(buffer, 0, bytesRecieved);
						if (!CheckIfConnected())
							return null;
					}
					CommandParameterPair pair = DecodeMessage(Encoding.UTF8.GetString(stream.ToArray()));
					if (pair == null)
					{
						SendInvalid("The command was not formatted correctly");
						continue;
					}
					return pair;
				}
			}
		}

		public CommandParameterPair DecodeMessage(string input)
		{
			string[] messageAndValue = input.Split(' ');
			if (messageAndValue.Length > 2)
				return null;
			string command = messageAndValue[0];
			if (messageAndValue.Length == 2)
			{
				string[] parameters = messageAndValue[1].Split('&');
				for (int i = 0; i < parameters.Length; i++)
					parameters[i] = Uri.UnescapeDataString(parameters[i]);
				return new CommandParameterPair(command, parameters);
			}
			return new CommandParameterPair(command, new string[0]);
		}

		public string EncodeMessage(CommandParameterPair pair)
		{
			if (pair.ParameterLength == 0)
				return pair.Command;
			var builder = new StringBuilder();

			builder.Append(String.Format("{0} ", pair.Command));
			foreach (string i in pair.Parameters)
				builder.Append(String.Format("{0}&", Uri.EscapeDataString(i)));
			string built = builder.ToString();
			return built.Substring(0, built.Length - 1);
		}
		public void SendInvalid(string message)
		{
			var pair = new CommandParameterPair("INVOP", Uri.EscapeDataString(message));
			SendCommand(pair);
		}

		public void CloseConnection()
		{
			ClientSocket.Disconnect(false);
		}

		private bool CheckIfConnected()
		{
			bool isBlocking = ClientSocket.Poll(1000, SelectMode.SelectRead);
			bool isAvailable = (ClientSocket.Available == 0);
			if (isBlocking && isAvailable)
				return false;
			return true;
		}
    }
}
