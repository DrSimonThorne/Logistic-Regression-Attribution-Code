using System;
using System.Threading.Tasks;
using CoreTrigger.ClassFiles;
using CoreTrigger.Extensions;
using McMaster.Extensions.CommandLineUtils;

namespace CoreTrigger.Commands
{
    class Command_Sync_Predict : Command_Predict
    {
        private CommandOption startDate;
        private CommandOption endDate;
        public override void construct()
        {
            Name = "Sync_Predict";
            Description = "Calls overriding command of predict to point only to journey_sync table";
            token = Option("-t | --token <client_name>", "-\"token\" | -all", CommandOptionType.SingleValue);
            connectionString = Option("-c | --connection <connection_string>", "-\"TODO:enter example string\" | -all", CommandOptionType.SingleValue);
            startDate = Option("-s | --startdate <startdate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
            endDate = Option("-e | --enddate <enddate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
            productID = Option("-p | --productid", "target product for command", CommandOptionType.SingleValue);
            remote = Option("-r | --remote <true>", "default <False>", CommandOptionType.NoValue);

            HelpOption("-h | -? | --help");
        }
        public override void setExecute()
        {
            OnExecute((Func<int>)runCommands);
        }

        public override int runCommands()
        {
            base.connection = new DbConnectionManager(
                connectionString.HasValue() ? connectionString.Value() : LoginDetails.getSafeConnectionString(),
                token.Value()
            );
            launch().GetAwaiter().GetResult();
            return 1;
        }

        public override async Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand)
        {
            Log log = new Log();
            string buildArgs = $"\"{args}\" {Name} --connection \"{clientCommand.connectionString}\" --startdate \"{startDate.Value()}\" --enddate \"{endDate.Value()}\" --productid \"{clientCommand.productId}\"";
            Console.WriteLine(buildArgs);
            if (remote.Values.Count == 1)
            {
                Console.WriteLine($"Started: {this.Name} on: {clientCommand.token} {clientCommand.productId} at: {DateTime.Now}");
            }
            ProcessResults logProcessResults = await ProcessEx.RunAsync(fileName, buildArgs);
            await log.writeAsync(clientCommand.token, this.Name, clientCommand.productId, logProcessResults, buildArgs);
            return logProcessResults;
        }
    }
}