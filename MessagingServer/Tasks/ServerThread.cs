using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessagingServer.Tasks
{
	public static class ServerThread
	{
		public static void ResumeThreads()
		{
			Program.ServerState = 1;
			foreach (Thread thread in Program.AcceptThreads)
				thread.Interrupt();
			foreach(Thread thread in Program.ClientThreads.Values)
				thread.Interrupt();
		}
	}
}
