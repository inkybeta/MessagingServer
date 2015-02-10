using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagingServerCore.Exceptions
{
	public class SslServerException : Exception
	{
		public string Reason { get; set; }
	}
}
