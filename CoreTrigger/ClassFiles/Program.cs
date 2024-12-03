using System;
namespace CoreTrigger.ClassFiles
{
    public class Program
    {
        public static int Main(params string[] args)
        {
            try
            {
                new Main().Execute(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.Write(ex);
                return ex.HResult;
            }
        }
    }
}