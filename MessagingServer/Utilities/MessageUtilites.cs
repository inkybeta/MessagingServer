using System;
using System.Text;
using MessagingServerCore;

namespace MessagingServer.Utilities
{
	public static class MessageUtilites
	{
		public static CommandParameterPair DecodeMessage(string input)
		{
			string[] messageAndValue = input.Split(' ');
			if (messageAndValue.Length > 2)
			{
				return null;
			}
			string command = messageAndValue[0];
			if (messageAndValue.Length == 2)
			{
				string[] parameters = messageAndValue[1].Split('&');
				for (int i = 0; i < parameters.Length; i++)
				{
					parameters[i] = Uri.UnescapeDataString(parameters[i]);
				}
				return new CommandParameterPair(command, parameters);
			}
			return new CommandParameterPair(command, new string[0]);
		}

		public static string EncodeMessage(CommandParameterPair message)
		{
			if (message.ParameterLength == 0)
				return message.Command;
			StringBuilder builder = new StringBuilder();
			builder.Append(String.Format("{0} ", message.Command));
			foreach (string parameter in message.Parameters)
				builder.Append(String.Format("{0}&", Uri.EscapeDataString(parameter)));
			return builder.ToString().Substring(0, builder.Length - 1);
		} 
	}
}
