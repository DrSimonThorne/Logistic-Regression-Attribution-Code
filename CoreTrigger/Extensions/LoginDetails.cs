using System;
using System.IO;
using System.Linq;

namespace CoreTrigger.Extensions
{
        internal static class LoginDetails
        {
                public static string getSafeConnectionString()
                {
                        //builds a 'safe' connection string using the db_connection_credentials.txt file on the current server.
                        //will always point to attrib to allow for token querys.

                        string userName = "";
                        string password = "";
                        string server = "";
                        string port = "";
                
                        getUserName(out userName, out password, out server, out port);

                        return $"mysql://{userName}:{password}@{server}:{port}/attrib";
                }
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
                                Console.WriteLine("ERR: Couldn't open connectionInfo file to collect server connecton info.");
                                Console.WriteLine(dnfe);
                                Environment.Exit(4);
                                throw;
                        }
                }
        }
}
