using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using MessagingServerCore;

namespace MessagingServer.Utilities
{
	public class SocketUtilities
	{
		public static void SendError(NetworkStream clientSocket, params string[] message)
		{
			string fullMessage = String.Format("ERROR {0}", message[0]);
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Write(messageLength, 0, 4);
			clientSocket.Write(byteMessage, 0, 4);
			clientSocket.Flush();
		}

		public static void SendInvalid(NetworkStream clientSocket, params string[] message)
		{
			string fullMessage = String.Format("INVOP {0}", message[0]);
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			byte[] messageLength = BitConverter.GetBytes(byteMessage.Length);
			clientSocket.Write(messageLength, 0, 4);
			clientSocket.Write(byteMessage, 0, 4);
		}

		public static void SendShutdown(NetworkStream clientSocket, params string[] message)
		{
			if (message.Length != 2)
			{
				message[0] = "The server encountered a fatal error.";
				message[1] = "0";
			}
			string fullMessage = String.Format("SDONW {0}&{1}", Uri.EscapeDataString(message[0]), Uri.EscapeDataString(message[1]));
			byte[] byteMessage = Encoding.UTF8.GetBytes(Uri.EscapeDataString(fullMessage));
			clientSocket.Write(BitConverter.GetBytes(byteMessage.Length), 0, 4);
			clientSocket.Write(byteMessage, 0, 4);
		}

		public static void SendCommand(NetworkStream clientStream, CommandParameterPair pair)
		{
			byte[] message = Encoding.UTF8.GetBytes(MessageUtilites.EncodeMessage(pair));
			clientStream.Write(BitConverter.GetBytes(message.Length), 0, 4);
			clientStream.Write(message, 0, message.Length);
		}

		public static int RecieveMessageLength(NetworkStream clientSocket)
		{
			using (var stream = new MemoryStream())
			{
				while (stream.Length != 4)
				{
					var buffer = new byte[4 - stream.Length];
					var bytesRecieved = clientSocket.Read(buffer, 0, buffer.Length);
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
		public static string RecieveMessage(NetworkStream clientSocket, int messageLength)
		{
			if (messageLength == -1)
				return null;

			var bytes = new byte[messageLength];

			using (var stream = new MemoryStream())
			{
				var buffer = stream.Length - messageLength > 512 ? new byte[512] : new byte[512 - (messageLength - stream.Length)];
				while (stream.Length != messageLength)
				{
					var bytesRecieved = clientSocket.Read(buffer, 0, buffer.Length);
					stream.Write(buffer, 0, bytesRecieved);
				}

				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}
	}
}
