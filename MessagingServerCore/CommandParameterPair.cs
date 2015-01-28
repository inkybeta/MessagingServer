using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServerCore
{
	/// <summary>
	/// Represents a command an parameters that have been escaped
	/// </summary>
	public class CommandParameterPair
	{
		public string Command { get; set; }

		public string[] Parameters { get; set; }

		public int ParameterLength
		{
			get { return Parameters.Length; }
		}

		public CommandParameterPair(string command, params string[] parameters)
		{
			Command = command;
			Parameters = parameters;
		}

		public CommandParameterPair(string command)
		{
			Command = command;
			Parameters = new string[0];
		}
	}
}
