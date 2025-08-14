using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ice;

namespace Experica
{
    public class AgentStub
    {
        Communicator communicator;
        public AgentPrx Agent;

        public void Start(string host = "LocalHost", uint port = 8888)
        {
            if (communicator == null) { communicator = Util.initialize(); }

            var obj = communicator.stringToProxy($"Agent:default -h {host} -p {port}");
            Agent = AgentPrxHelper.checkedCast(obj);
            if (Agent == null)
            {
                throw new ApplicationException($"Invalid Agent Proxy from Host: {host}, Port: {port}");
            }
        }

        public void Shutdown()
        {
            if (communicator != null)
            {
                communicator.Dispose();
                communicator = null;
                Agent = null;
            }
        }
    }
}
