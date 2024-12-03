using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;

namespace CoreTrigger.Extensions
{
    static class EmptyDirs
    {
        public static void Empty(this DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles()) file.Delete();
            foreach (var subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
        }

        public static void EmptyFrom(this DirectoryInfo directory, List<string> tokensRead)
        {
            foreach (var file in directory.GetFiles())
                if (tokensRead != null && tokensRead.Contains(Path.GetFileNameWithoutExtension(file.ToString()))
                    && !file.ToString().Equals("gitkeep"))
                    file.Delete();
        }
    }
}
