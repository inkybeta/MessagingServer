using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MessagingServerCore;

namespace MessagingServerBusiness
{
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
		/// Send a shutdown message. Reserved for the server
		/// </summary>
		/// <param name="send">The message to send</param>
	    public void SendShutdown(string send)
	    {
		    var pair = new CommandParameterPair("SDOWN {0}&{1}", Uri.EscapeDataString(send), "0");
		    Send(pair);
			CloseConnection();
	    }

		/// <summary>
		/// Disconnect the client. Reserved for the server
		/// </summary>
		/// <param name="reason">Send the reason for disconnecting the client.</param>
	    public void Disconnect(string reason)
	    {
		    var pair = new CommandParameterPair("DISCONN", Uri.EscapeDataString(reason));
			Send(pair);
			CloseConnection();
	    }

	    public void SendInvalid(string message)
	    {
		    var pair = new CommandParameterPair("INVOP", Uri.EscapeDataString(message));
			Send(pair);
	    }
		/// <summary>
		/// Send a message to the client.
		/// </summary>
		/// <param name="username">The user sending the message</param>
		/// <param name="message">The message to send</param>
	    public void SendMessage(string username, string message)
		{
			var pair = new CommandParameterPair("NEWMSG", Uri.EscapeDataString(username), Uri.EscapeDataString(message));
			Send(pair);
		}

		/// <summary>
		/// Reserved for the server
		/// </summary>
		/// <param name="command">The command to be sent</param>
	    public void SendCommand(CommandParameterPair command)
		{
			Send(command);
		}

		/// <summary>
		/// Recieve a message
		/// </summary>
		/// <returns></returns>
	    public CommandParameterPair RecieveMessage()
	    {
		    int messageLength;
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
			    CommandParameterPair pair = DecodeMessage(Encoding.UTF8.GetString(stream.ToArray()));
			    if (pair == null)
			    {
				    SendInvalid("The command was not formatted correctly");
				    return null;
			    }
			    return pair;
		    }
	    }

		/// <summary>
		/// Recieves the 
		/// </summary>
		/// <param name="command"></param>
	    private void Send(CommandParameterPair command)
	    {
		    if (command.ParameterLength == 0)
		    {
			    byte[] fullMessage = Encoding.UTF8.GetBytes(command.Command);
			    Client.ClientSocket.Send(BitConverter.GetBytes(fullMessage.Length), 4, SocketFlags.None);
			    Client.ClientSocket.Send(fullMessage, fullMessage.Length, SocketFlags.None);
			    return;
		    }
			string smessage = EncodeMessage(command);
		    byte[] byteMessage = Encoding.UTF8.GetBytes(smessage);
		    Client.ClientSocket.Send(BitConverter.GetBytes(byteMessage.Length), 4, SocketFlags.None);
		    Client.ClientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
	    }

		private CommandParameterPair DecodeMessage(string input)
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

	    private string EncodeMessage(CommandParameterPair pair)
	    {
		    if (pair.ParameterLength == 0)
			    return pair.Command;
		    var builder = new StringBuilder();

		    builder.Append(String.Format("{0} ",pair.Command));
		    foreach (string i in pair.Parameters)
			    builder.Append(String.Format("{0}&", Uri.EscapeDataString(i)));
		    string built = builder.ToString();
		    return built.Substring(0, built.Length - 1);
	    }

	    private void CloseConnection()
	    {
		    Client.ClientSocket.Close();
	    }
    }
}
