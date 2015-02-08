using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace MessagingServer.Utilities
{
	public class SocketUtilities
	{
		public static void SendError(Socket clientSocket, params string[] message)
		{
			string fullMessage = String.Format("ERROR {0}", message[0]);
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Send(messageLength, 4, SocketFlags.None);
			clientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		public static void SendInvalid(Socket clientSocket, params string[] message)
		{
			string fullMessage = String.Format("INVOP {0}", message[0]);
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Send(messageLength, 4, SocketFlags.None);
			clientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		public static void SendShutdown(Socket clientSocket, params string[] message)
		{
			if (message.Length != 2)
			{
				message[0] = "The server encountered a fatal error.";
				message[1] = "0";
			}
			string fullMessage = String.Format("SDONW {0}&{1}", Uri.EscapeDataString(message[0]), Uri.EscapeDataString(message[1]));
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			clientSocket.Send(BitConverter.GetBytes(byteMessage.Length), 4, SocketFlags.None);
			clientSocket.Send(byteMessage, byteMessage.Length, SocketFlags.None);
		}

		public static int RecieveMessageLength(Socket clientSocket)
		{
			using (var stream = new MemoryStream())
			{
				while (stream.Length != 4)
				{
					var buffer = new byte[4 - stream.Length];
					var bytesRecieved = clientSocket.Receive(buffer);
					stream.Write(buffer, 0, bytesRecieved);
				}
				return BitConverter.ToInt32(stream.ToArray(), 0);
			}
		}

		/// <summary>
		/// Recieves the message based on the length provided assumming that the length has already been recieved
		/// </summary>
		/// <param name="clientSocket">The socket connecting to the client</param>
		/// <param name="messageLength">The length of the message</param>
		/// <returns>The message</returns>
		public static string RecieveMessage(Socket clientSocket, int messageLength)
		{
			var bytes = new byte[messageLength];

			using (var stream = new MemoryStream())
			{
				var buffer = new byte[512];
				while (stream.Length != messageLength)
				{
					var bytesRecieved = clientSocket.Receive(buffer, 0, buffer.Length, SocketFlags.None);
					stream.Write(buffer, 0, bytesRecieved);
				}

				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}
	}
}
