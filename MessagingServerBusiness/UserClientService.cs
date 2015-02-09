using System;
using MessagingServerBusiness.Interfaces;
using MessagingServerCore;
using MessagingServerCore.Interfaces;

namespace MessagingServerBusiness
{
    public class UserClientService : IMessagingClient
    {
		public IClient Client { get; set; }

		public string UserName { get { return Client.UserName; } set { Client.UserName = value; } }
		public bool IsAfk { get { return Client.IsOnline; } set { Client.IsOnline = value; } }

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
		    Client.SendCommand(pair);
			Client.CloseConnection();
	    }

		/// <summary>
		/// Disconnect the client. Reserved for the server
		/// </summary>
		/// <param name="reason">Send the reason for disconnecting the client.</param>
	    public void Disconnect(string reason)
	    {
		    var pair = new CommandParameterPair("DISCONN", Uri.EscapeDataString(reason));
			Client.SendCommand(pair);
			Client.CloseConnection();
	    }
		/// <summary>
		/// Send a message to the client.
		/// </summary>
		/// <param name="username">The user sending the message</param>
		/// <param name="message">The message to send</param>
	    public void SendMessage(string username, string message)
		{
			var pair = new CommandParameterPair("NEWMSG", Uri.EscapeDataString(username), Uri.EscapeDataString(message));
			Client.SendCommand(pair);
		}

		/// <summary>
		/// Reserved for the server
		/// </summary>
		/// <param name="command">The command to be sent</param>
	    public void SendCommand(CommandParameterPair command)
		{
			Client.SendCommand(command);
		}

	    public void SendInvalid(string message)
	    {
		    Client.SendInvalid(message);
	    }

	    public CommandParameterPair RecieveMessage()
	    {
		    return Client.RecieveCommand();
	    }
    }
}
