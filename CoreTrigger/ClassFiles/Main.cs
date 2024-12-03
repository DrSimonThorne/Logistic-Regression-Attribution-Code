using System;
using System.Collections.Generic;
using System.Text;
using McMaster.Extensions.CommandLineUtils;

namespace CoreTrigger.ClassFiles
{
    class Main : CommandLineApplication
    {
        public Main()
        {
	    Commands.Add(new Commands.Command_Train());
	    Commands.Add(new Commands.Command_Predict());
	    Commands.Add(new Commands.Command_Sync_Predict());
	    HelpOption("-h | -? | --help");
        }
    }
}
