using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace CoreTrigger.ClassFiles
{
    public partial class ProcessEx
    {
        public static Task<ProcessResults> RunAsync(string fileName)
        {
            return RunAsync(new ProcessStartInfo(fileName));
        }

        public static Task<ProcessResults> RunAsync(string fileName, string arguments)
        {
            return RunAsync(new ProcessStartInfo(fileName, arguments));
        }
    }
}