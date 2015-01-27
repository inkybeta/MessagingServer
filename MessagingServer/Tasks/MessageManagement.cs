﻿using System;
using System.Text;
using MessagingServerCore;

namespace MessagingServer.Tasks
{
	public static class MessageManagement
	{
		public static CommandParameterPair RecieveMessage(string input)
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
					parameters[i] = Uri.UnescapeDataString(parameters[i]);
				return new CommandParameterPair(command, parameters);
			}
			return new CommandParameterPair(command, new string[0]);
		}

		public static string CreateMessage(CommandParameterPair message)
		{
			if (message.ParameterLength == 0)
				return message.Command;
			StringBuilder builder = new StringBuilder();
			builder.Append(String.Format("{0} ", message.Command));
			foreach (string parameter in message.Parameters)
				builder.Append(String.Format("{0}&", parameter));
			return builder.ToString();
		} 
	}
}
