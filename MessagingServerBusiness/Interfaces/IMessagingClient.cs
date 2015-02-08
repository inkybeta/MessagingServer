using MessagingServerCore;
using MessagingServerCore.Interfaces;

namespace MessagingServerBusiness
{
	public interface IMessagingClient
	{
		IClient Client { get; set; }
		/// <summary>
		/// Send a shutdown message. Reserved for the server
		/// </summary>
		/// <param name="send">The message to send</param>
		void SendShutdown(string send);

		/// <summary>
		/// Disconnect the client. Reserved for the server
		/// </summary>
		/// <param name="reason">Send the reason for disconnecting the client.</param>
		void Disconnect(string reason);

		void SendInvalid(string message);

		/// <summary>
		/// Send a message to the client.
		/// </summary>
		/// <param name="username">The user sending the message</param>
		/// <param name="message">The message to send</param>
		void SendMessage(string username, string message);

		/// <summary>
		/// Reserved for the server
		/// </summary>
		/// <param name="command">The command to be sent</param>
		void SendCommand(CommandParameterPair command);

		/// <summary>
		/// Recieve a message
		/// </summary>
		/// <returns></returns>
		CommandParameterPair RecieveMessage();
	}
}