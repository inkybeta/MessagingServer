using System;

namespace MessagingServerCore.Exceptions
{
	public class SslServerException : Exception
	{
		public string Reason { get; set; }
	}
}
