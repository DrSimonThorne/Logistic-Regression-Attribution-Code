// using System;
// using System.Threading.Tasks;
// using CoreTrigger.ClassFiles;
// using Microsoft.Extensions.CommandLineUtils;

// namespace CoreTrigger.Commands
// {
//         class Command_InsertSP : ParallelLaunchProcess
//         {
//                 private readonly CommandOption _clients;
//                 private readonly CommandOption _fileName;
//                 private readonly CommandOption remote;
//                 private readonly int commandNumber = 4;
//                 public Command_InsertSP()
//                 {
//                         Name = "insertSP";
//                         Description = "Inserts the named SP into the Client(s) Selected.";
//                         _clients = Option("-c | --client <client>", "-\"token\" | -all", CommandOptionType.SingleValue);
//                         _fileName = Option("-f | --file <crossvalenddate>", "/path/filename.sql", CommandOptionType.SingleValue);
//                         remote = Option("-r | --remote <true>", "default <False>", CommandOptionType.NoValue);
//                         HelpOption("-h | -? | --help");
//                         OnExecute((Func<int>)runCommands);
//                 }
//                 public override int runCommands()
//                 {
//                         chunkSize = 15;
//                         launch(_clients.Value(), remote.Value()).GetAwaiter().GetResult();
//                         return 1;
//                 }
//                 public override async Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand)
//                 {
//                         Log log = new Log();
//                         string buildArgs = $"\"{args}\" {commandNumber} {clientCommand.connectionString} {_fileName.Value()}";
//                         if (remote.Values.Count == 1)
//                         {
//                                 Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} at: {DateTime.Now}");
//                         }
//                         ProcessResults logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
//                         await log.writeAsync(clientCommand.token, this.Name, logProcessResults, buildArgs);
//                         return logProcessResults;
//                 }

//         }
// }
