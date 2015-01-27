using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MessagingServerCore;

namespace MessagingServerBusiness
{
	public delegate void ClientCommand(string parameters);
    public class UserClientService
    {
		public UserClient Client { get; set; }

		/// <summary>
		/// Creates a new service off of a client socket.
		/// </summary>
		/// <param name="client">The client socket</param>
	    public UserClientService(UserClient client)
		{
			Client = client;
		}

	    public void SendShutdown(string send)
	    {
		    string smessage = String.Format("SDOWN {0}&{1}", Uri.EscapeDataString(send), Uri.EscapeDataString("0"));
		    byte[] message = Encoding.UTF8.GetBytes(smessage);
		    Client.ClientSocket.Send(BitConverter.GetBytes(message.Length), 4, SocketFlags.None);
		    Client.ClientSocket.Send(message, message.Length, SocketFlags.None);
			CloseConnection();
	    }

	    public void Disconnect(string reason)
	    {
		    
	    }

	    public void SendMessage(string message)
	    {
		    
	    }

	    public CommandParameterPair RecieveMessage()
	    {
		    int messageLength;
		    string message;
		    using (var stream = new MemoryStream())
		    {
			    while (stream.Length != 4)
			    {
				    var buffer = new byte[4 - stream.Length];
				    int bytesRecieved = Client.ClientSocket.Receive(buffer);
					stream.Write(buffer, 0, bytesRecieved);
			    }
			    messageLength = BitConverter.ToInt32(stream.ToArray(), 0);
		    }
		    using (var stream = new MemoryStream())
		    {
			    while (stream.Length != messageLength)
			    {
				    var buffer = new byte[512];
				    int bytesRecieved = Client.ClientSocket.Receive(buffer);
					stream.Write(buffer, 0, bytesRecieved);
			    }
			    return RecieveMessage(Encoding.UTF8.GetString(stream.ToArray()));
		    }
	    }

	    private void Send(CommandParameterPair command)
	    {
		    
	    }

		public CommandParameterPair RecieveMessage(string input)
		{
			string[] messageAndValue = input.Split(' ');
			if (messageAndValue.Length > 2)
			{
				return null;
			}
			string command = messageAndValue[0];
			if (messageAndValue.Length != 2)
			{
				string[] parameters = messageAndValue[1].Split('&');
				for (int i = 0; i < parameters.Length; i++)
					parameters[i] = Uri.UnescapeDataString(parameters[i]);
				return new CommandParameterPair(command, parameters);
			}
			return new CommandParameterPair(command, new string[0]);
		}

	    private void CloseConnection()
	    {
		    Client.ClientSocket.Close();
	    }
    }
}
