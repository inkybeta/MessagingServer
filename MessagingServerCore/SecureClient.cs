using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
		SslStream stream { get; set; }


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
			set { _isOnline = false; }
		}

		public ConcurrentDictionary<string, string> GroupAndRole { get; set; }

		public ConcurrentDictionary<string, string> Properties { get; set; }

		public DateTime TimeLastUsed { get; set; }

		public SecureClient(Socket clientSocket, string userName, string clientType, ConcurrentDictionary<string, string> groupAndRole, ConcurrentDictionary<string, string> properties, DateTime timeLastUsed)
		{
			stream = new SslStream(new NetworkStream(clientSocket));
			stream.AuthenticateAsServer(new X509Certificate());
			UserName = userName;
			ClientType = clientType;
			GroupAndRole = groupAndRole;
			Properties = properties;
			TimeLastUsed = timeLastUsed;
		}
		public void SendCommand(CommandParameterPair command)
		{
			
		}

		public CommandParameterPair RecieveCommand()
		{
			throw new NotImplementedException();
		}

		public CommandParameterPair DecodeMessage(string input)
		{
			throw new NotImplementedException();
		}

		public string EncodeMessage(CommandParameterPair pair)
		{
			throw new NotImplementedException();
		}

		public void SendInvalid(string message)
		{
			throw new NotImplementedException();
		}

		public void CloseConnection()
		{
			throw new NotImplementedException();
		}
	}
}
