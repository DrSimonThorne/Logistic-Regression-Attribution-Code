using System;
using System.Text;
using System.Collections.Generic;
using McMaster.Extensions.CommandLineUtils;
using CoreForecast.ClassFiles.Commands;

namespace CoreForecast.ClassFiles
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