// using System;
// using System.Threading.Tasks;
// using CoreTrigger.ClassFiles;
// using Microsoft.Extensions.CommandLineUtils;

// namespace CoreTrigger.Commands
// {
//         class Command_BatchPredict : ParallelLaunchProcess
//         {
//                 private readonly CommandOption _startDate;
//                 private readonly CommandOption _endDate;
//                 private readonly CommandOption _clients;
//                 private readonly CommandOption remote;
//                 private readonly int commandNumber = 2;

//                 public Command_BatchPredict()
//                 {
//                         Name = "batchPredict";
//                         Description = "Runs the Batch Predict task for the Client(s) Selected";
//                         _clients = Option("-c | --client <client>", "-\"token\" | -all", CommandOptionType.SingleValue);
//                         _startDate = Option("-s | --startdate <startdate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
//                         _endDate = Option("-e | --enddate <enddate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
//                         remote = Option("-r | --remote <true>", "default <False>", CommandOptionType.NoValue);
//                         HelpOption("-h | -? | --help");
//                         OnExecute((Func<int>)runCommands);
//                 }
//                 public override int runCommands()
//                 {
//                         chunkSize = 5;
//                         launch(_clients.Value(), remote.Value()).GetAwaiter().GetResult();
//                         return 1;
//                 }

//                 public override async Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand)
//                 {
//                         Log log = new Log();
//                         string buildArgs = $"\"{args}\" {commandNumber} {clientCommand.connectionString} \"{_startDate.Value()}\" \"{_endDate.Value()}\"";
//                         if (remote.Values.Count == 1)
//                         {
//                                 Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} at: {DateTime.Now}");
//                                 Console.WriteLine($"Between {this._startDate.Value()} And {this._endDate.Value()}");
//                         }
//                         ProcessResults logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
//                         await log.writeAsync(clientCommand.token, this.Name, logProcessResults, buildArgs);
//                         return logProcessResults;
//                 }
//         }
// }
