using CoreTrigger.ClassFiles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace CoreTrigger.Interfaces
{
    interface ICommand
    {
        void construct();
        int runCommands();
        Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand);
        void setExecute();
    }
}
