using System;

namespace MessagingServer.Utilities
{
	public static class ConsoleUtilities
	{
		public static void PrintInformation(string message, params object[] obj)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine(message, obj);
			Console.ResetColor();
		}

		public static void PrintWarning(string message, params object[] obj)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(message, obj);
			Console.ResetColor();
		}

		public static void PrintRequest(string message, params object[] obj)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(message, obj);
			Console.ResetColor();
		}

		public static void PrintCritical(string message, params object[] obj)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message, obj);
			Console.ResetColor();
		}

		public static void PrintCommand(string message, params object[] obj)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine(message, obj);
			Console.ResetColor();
		}
	}
}
