// using System;
// using System.Threading.Tasks;
// using CoreTrigger.ClassFiles;
// using Microsoft.Extensions.CommandLineUtils;
// using System.IO;
// using System.Collections.Generic;
// using System.Linq;
// using CoreTrigger.Extensions;

// namespace CoreTrigger.Commands
// {
//         class Command_BackdatePredict : ParallelLaunchProcess
//         {
//                 private readonly CommandOption _startDate;
//                 private readonly CommandOption _endDate;
//                 private readonly CommandOption _crossValStartDate;
//                 private readonly CommandOption _crossValEndDate;

//                 private readonly CommandOption _clients;
//                 private readonly CommandOption remote;
//                 private readonly int commandNumber = 0;

//                 public Command_BackdatePredict()
//                 {
//                         Name = "BackdatePredictions";
//                         Description = "Will loop through the given date range and  backdate the prediction likelihoods. Training will happen at the start of each week in the date range.";
//                         _startDate = Option("-s | --startdate <startdate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
//                         _endDate = Option("-e | --enddate <enddate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
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
//                                                 directory.EmptyFrom(tasksToQueue);
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
//                         string buildArgs;
//                         ProcessResults logProcessResults = default(ProcessResults);
//                         foreach (var day in loopRange(Convert.ToDateTime(_startDate.Value()), Convert.ToDateTime(_endDate.Value())))
//                         {
//                                 if (day.DayOfWeek == DayOfWeek.Sunday)
//                                 {
//                                         this.Name = "Train";
//                                         buildArgs = $"\"{args}\" {commandNumber} {clientCommand.connectionString} \"{_startDate.Value()}\" \"{_endDate.Value()}\" \"{_crossValStartDate.Value()}\" \"{_crossValEndDate.Value()}\"";
//                                         if (remote.Values.Count == 1)
//                                         {
//                                                 Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} at: {DateTime.Now}");
//                                                 Console.WriteLine($"Between {this._startDate.Value()} And {this._endDate.Value()}");
//                                         }
//                                         logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
//                                         await log.writeAsync(clientCommand.token, this.Name, logProcessResults, buildArgs);
//                                 }
//                                 this.Name = "Predict";
//                                 buildArgs = $"\"{args}\" {commandNumber} {clientCommand.connectionString} \"{_startDate.Value()}\" \"{_endDate.Value()}\" \"{_crossValStartDate.Value()}\" \"{_crossValEndDate.Value()}\"";
//                                 if (remote.Values.Count == 1)
//                                 {
//                                         Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} at: {DateTime.Now}");
//                                         Console.WriteLine($"Between {this._startDate.Value()} And {this._endDate.Value()}");
//                                 }
//                                 logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
//                                 await log.writeAsync(clientCommand.token, this.Name, logProcessResults, buildArgs);
//                         }

//                         return logProcessResults;
//                 }

//                 public static IEnumerable<DateTime> loopRange(DateTime from, DateTime thru)
//                 {
//                         for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
//                                 yield return day;
//                 }
//         }
// }