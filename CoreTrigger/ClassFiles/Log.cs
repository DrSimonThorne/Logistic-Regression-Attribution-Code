using CoreTrigger.ClassFiles;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CoreTrigger.ClassFiles
{
        public class Log
        {
                private string path;
                private DateTime todaysDate = DateTime.Now;
                public void initialize(string account, string command)
                {
                        this.path = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "logs", $"{account}", $"{todaysDate:dd-MM-yyyy}", $"{command}"));
                        this.todaysDate = DateTime.Now;
                }
                public void initialize(string account, string command, int productId)
                {
                        this.path = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "logs", $"{account}", $"{todaysDate:dd-MM-yyyy}", $"{command}", $"{productId}"));
                        this.todaysDate = DateTime.Now;
                }

                public async Task writeAsync(string account, string command, int prodictId, ProcessResults results, string launchArgs)
                {
                        initialize(account, command, prodictId);
                        StringBuilder output = new StringBuilder();
                        Directory.CreateDirectory(path);
                        using (StreamWriter sw = new StreamWriter(new FileStream(Path.Combine(path, "log.txt"), FileMode.Append)))
                        {
                                //log header
                                ConsoleTable table = new ConsoleTable("Start/ End", "Command", "Token", "Date", "Run Time",
                                    "Type Of Output", "Exit Code");
                                table.AddRow("Start", command, account, todaysDate, results.RunTime, "StdOut", "N/A");
                                output.Append(table);
                                sw.WriteLine(output.ToString());

                                //log Output + error
                                table = new ConsoleTable("Start/ End", "Command", "Token", "Date", "Run Time", "Type Of Output",
                                    "Exit Code");
                                table.AddRow("Start", command, account, todaysDate, results.RunTime, "StdOut", "N/A");
                                output = new StringBuilder();
                                output.Append(table);
                                output.Append($"LaunchArgs: \n\t {launchArgs} \n");
                                foreach (var line in results.StandardOutput)
                                {
                                        output.Append(line + "\n");
                                }

                                foreach (var line in results.StandardError)
                                {
                                        output.Append(line + "\n");
                                }

                                //write
                                sw.WriteLine(output.ToString());

                        }
                }
                public async Task writeAsync(string account, string command, ConsoleTable Table)
                {
                        initialize(account, command);
                        StringBuilder output = new StringBuilder();
                        Directory.CreateDirectory(path);
                        using (StreamWriter sw = new StreamWriter(new FileStream(Path.Combine(path, "log.txt"), FileMode.Append)))
                        {
                                output.Append(Table);
                                sw.WriteLine(output.ToString());
                        }
                }
        }
}