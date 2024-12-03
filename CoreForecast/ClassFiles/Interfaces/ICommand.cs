using CoreForecast.ClassFiles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace CoreForecast.ClassFiles.Interfaces
{
    interface ICommand
    {
        void construct();
        int runCommands();
        bool loadData();
        void setExecute();
    }
}
