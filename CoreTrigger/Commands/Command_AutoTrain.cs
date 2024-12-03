// using CoreTrigger.ClassFiles;
// using CoreTrigger.Extensions;
// using Microsoft.Extensions.CommandLineUtils;
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;

// namespace CoreTrigger.Commands
// {
//         class Command_AutoTrain : ParallelLaunchProcess
//         {
//                 private readonly DateTime _startDate = DateTime.Today.AddDays(-5);
//                 private readonly DateTime _endDate = DateTime.Today.AddMonths(-1).AddDays(-5).AddHours(23).AddMinutes(59).AddSeconds(59);
//                 private readonly DateTime _crossValStartDate = DateTime.Today;
//                 private readonly DateTime _crossValEndDate = DateTime.Today.AddDays(-10).AddHours(23).AddMinutes(59).AddSeconds(59);
                
//                 private readonly CommandOption _clients;
//                 private readonly CommandOption remote;
//                 private readonly int commandNumber = 0;

//                 public Command_AutoTrain()
//                 {
//                         Name = "AutoTrain";
//                         Description = "Should Only be called by Control, Please Use \"Train\" instead";
//                         _clients = Option("-c | --client <client>", "-\"token\" | -all", CommandOptionType.SingleValue);
//                         remote = Option("-r | --remote <true>", "default <False>", CommandOptionType.NoValue);
//                         HelpOption("-h | -? | --help");
//                         OnExecute((Func<int>)runCommands);
//                 }

//                 public override int runCommands()
//                 {
//                         launch(_clients.Value(), remote.Value()).GetAwaiter().GetResult();
//                         return 1;
//                 }

//                 protected override void getAccountsToRun(string clients, out int maxAccounts)
//                 {
//                         if (!endTokenRecived)
//                         {
//                                 string path = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "tokenQueue"));
//                                 List<string> tasksToQueue = new List<string>();
//                                 try
//                                 {
//                                         DirectoryInfo directory = new DirectoryInfo(path);
//                                         try
//                                         {
//                                                 tasksToQueue = directory.GetFiles().Select(file => Path.GetFileNameWithoutExtension(file.ToString())).ToList();
// 												directory.EmptyFrom(tasksToQueue);
//                                         }
//                                         catch (System.InvalidOperationException ioe) { }

//                                         foreach (var token in tasksToQueue)
//                                         {
//                                                 if (token == "endOfAccounts")
//                                                 {
//                                                         endTokenRecived = true;
//                                                 }
//                                                 else
//                                                 {
//                                                         var conn = DbConnectionManager.ConnectionStrings(token);
//                                                         if (conn.Any())
//                                                         {
//                                                                 connsQueue.Enqueue(conn.Dequeue());
//                                                         }
//                                                 }
//                                         }
//                                         maxAccounts = connsQueue.Count;
//                                         if (endTokenRecived)
//                                         {
//                                                 directory.Empty();
//                                         }
//                                 }
//                                 catch (DirectoryNotFoundException dnfe)
//                                 {
//                                         Console.WriteLine($"ERR: Directory was not found. {path} was unable to be found whilst searching for client tokens.");
//                                         Console.WriteLine(dnfe);
//                                         throw;
//                                 }
//                         }
//                         else
//                         {
//                                 maxAccounts = connsQueue.Count();
//                         }
//                 }
//                 public override async Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand)
//                 {
//                         Log log = new Log();

//                         string buildArgs = $"\"{args}\" {commandNumber} {clientCommand.connectionString} " +
//                                            $"\"{_startDate.ToString(format)}\" " +
//                                            $"\"{_endDate.ToString(format)}\" " +
//                                            $"\"{_crossValStartDate.ToString(format)}\" " +
//                                            $"\"{_crossValEndDate.ToString(format)}\"";
//                         if (remote.Values.Count != 0)
//                         {
//                                Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} at: {DateTime.Now}");
//                                System.Console.WriteLine($"Between {this._startDate.ToString(format)} And {this._endDate.ToString(format)}");
//                         }
//                         ProcessResults logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
//                         await log.writeAsync(clientCommand.token, this.Name, logProcessResults, buildArgs);
//                         return logProcessResults;
//                 }
//         }

// }
