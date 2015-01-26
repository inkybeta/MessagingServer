using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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

	    public void SendShutdown(string send)
	    {
		    string smessage = String.Format("SDOWN {0}&{1}", Uri.EscapeDataString(send), Uri.EscapeDataString("0"));
		    byte[] message = Encoding.UTF8.GetBytes(smessage);
		    Client.ClientSocket.Send(BitConverter.GetBytes(message.Length), 4, SocketFlags.None);
		    Client.ClientSocket.Send(message, message.Length, SocketFlags.None);
			CloseConnection();
	    }

	    private void CloseConnection()
	    {
		    Client.ClientSocket.Close();
	    }
    }
}
