using System;
using System.IO;
using System.Linq;
namespace CoreForecast.ClassFiles.Extensions
{
        internal static class LoginDetails
        {
                public static void getUserName(out string userName, out string password, out string server, out string port)
                {
                        userName = "";
                        password = "";
                        server = "";
                        port = "";
                        var path = "";

                        path = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "keys",
                            "db_connection_credentials.txt")
                        );
                        try
                        {
                                var lines = File.ReadAllLines(path);
                                if (lines.Any())
                                {
                                        userName = lines[0];
                                        password = lines[1];
                                        server = lines[2];
                                        port = lines[3];
                                }
                        }
                        catch (DirectoryNotFoundException dnfe)
                        {
                                Console.WriteLine("ERR: Couldn't open connection Info file to collect server connecton info.");
                                Console.WriteLine(dnfe);
                                Environment.Exit(4);
                                throw;
                        }
                }
        }
}
