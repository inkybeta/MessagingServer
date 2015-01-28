using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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

		/// <summary>
		/// Send a shutdown message.
		/// </summary>
		/// <param name="send">The message to send</param>
	    public void SendShutdown(string send)
	    {
		    var pair = new CommandParameterPair("SDOWN {0}&{1}", Uri.EscapeDataString(send), "0");
		    Send(pair);
			CloseConnection();
	    }

		/// <summary>
		/// Disconnect the client
		/// </summary>
		/// <param name="reason">Send the reason for disconnecting the client.</param>
	    public void Disconnect(string reason)
	    {
		    var pair = new CommandParameterPair("DISCONN", Uri.EscapeDataString(reason));
			Send(pair);
			CloseConnection();
	    }

		/// <summary>
		/// Send a message to the client.
		/// </summary>
		/// <param name="message">The message to send</param>
	    public void SendMessage(string message)
		{
			var pair = new CommandParameterPair("NEWMSG", Uri.EscapeDataString(message));
			Send(pair);
			CloseConnection();
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
			    return ConvertMessage(Encoding.UTF8.GetString(stream.ToArray()));
		    }
	    }

	    private void Send(CommandParameterPair command)
	    {
		    if (command.ParameterLength == 0)
		    {
			    byte[] fullMessage = Encoding.UTF8.GetBytes(command.Command);
			    Client.ClientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
			    Client.ClientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
			    return;
		    }
			StringBuilder builder = new StringBuilder();
		    foreach (string parameter in command.Parameters)
			    builder.Append(String.Format("{0}&", parameter));
		    string temp = builder.ToString();
		    string parameters = temp.Substring(0, temp.Length - 1);
		    string smessage = String.Format("{0} {1}", command.Command, parameters);
		    byte[] byteMessage = Encoding.UTF8.GetBytes(smessage);
		    Client.ClientSocket.Send(BitConverter.GetBytes(byteMessage.Length), 4, SocketFlags.None);
		    Client.ClientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
	    }

		private CommandParameterPair ConvertMessage(string input)
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
