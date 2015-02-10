using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MessagingServerCore.Interfaces;

namespace MessagingServerCore
{
	public class SecureClient : IClient
	{
		public SslStream Stream { get; set; }


		public string UserName { get; set; }

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

		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }

		public ConcurrentDictionary<string, string> Properties { get; set; }

		public DateTime TimeLastUsed { get; set; }

		public SecureClient(Socket clientSocket, string certFile, string userName, string clientType, ConcurrentDictionary<string, string> groupAndRole, ConcurrentDictionary<string, string> properties, DateTime timeLastUsed, string password)
		{
			Stream = new SslStream(new NetworkStream(clientSocket));
			Stream.AuthenticateAsServer(String.IsNullOrEmpty(password)
				? new X509Certificate(certFile)
				: new X509Certificate(certFile, password));
			UserName = userName;
			ClientType = clientType;
			GroupAndRole = groupAndRole;
			Properties = properties;
			TimeLastUsed = timeLastUsed;
		}
		public void SendCommand(CommandParameterPair command)
		{
			try
			{
				byte[] comm = Encoding.UTF8.GetBytes(EncodeMessage(command));
				Stream.Write(BitConverter.GetBytes(comm.Length));
				Stream.Write(comm);
			}
			catch (Exception)
			{
				throw new SocketException();
			}
		}

		public CommandParameterPair RecieveCommand()
		{
			int messageLength;
			using (var stream = new MemoryStream())
			{
				while (stream.Length != 4)
				{
					var buffer = new byte[4 - stream.Length];
					int bytesRead = Stream.Read(buffer, 0, (int) (4 - stream.Length));
					stream.Write(buffer, 0, bytesRead);
				}
				messageLength = BitConverter.ToInt32(stream.ToArray(), 0);
			}
			string message;
			using (var stream = new MemoryStream())
			{
				while (stream.Length != messageLength)
				{
					var buffer = new byte[512];
					int bytesRecieved = Stream.Read(buffer, 0, 512);
					stream.Write(buffer, 0, bytesRecieved);
				}
				message = Encoding.UTF8.GetString(stream.ToArray());
			}
			return DecodeMessage(message);
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
			return builder.ToString().Substring(0, builder.Length - 1);
		}

		public void SendInvalid(string message)
		{
			var pair = new CommandParameterPair("INVOP", Uri.EscapeDataString(message));
			SendCommand(pair);
		}

		public void CloseConnection()
		{
			Stream.Close();
			Stream.Dispose();
		}

		public void Abort()
		{
			Stream.Close();
		}
	}
}
