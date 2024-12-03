using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using McMaster.Extensions.CommandLineUtils;
using CoreTrigger.Interfaces;
using System.Linq;
using System.Threading;
using CoreTrigger.Extensions;

namespace CoreTrigger.ClassFiles
{
    abstract class ParallelLaunchProcess : CommandLineApplication, ICommand
    {
        protected CommandOption token;
        protected CommandOption connectionString;
        protected CommandOption productID;
        protected CommandOption remote;
        protected DbConnectionManager connection;
        private static int sizeOfBlock;
        private static int bottom;
        private static int completedTasks = 0;
        protected static bool endTokenRecived = false;
        protected string fileName = "dotnet";
        protected string args = Path.GetFullPath(Path.Combine("..", "..", "..", "build", "CoreForecast", "netcoreapp2.0", "CoreForecast.dll"));
        protected Queue<string> accountsQueue = new Queue<string>();
        protected Dictionary<string, Queue<int>> productQueue = new Dictionary<string, Queue<int>>();
        protected int chunkSize = 1;
        private int runningTasksCount = 0;
        public const string format = "yyyy-MM-dd HH:mm:ss";
        protected async Task launch()
        {
            //Basic vars
            var startTime = DateTime.Now;

            //Account Vars
            Queue<Task<ProcessCommands>> runningTasks = new Queue<Task<ProcessCommands>>();
            List<Task<ProcessCommands>> failedAccounts = new List<Task<ProcessCommands>>();

            int maxAccounts = 0;
            while (true)
            {
                if (!remote.HasValue())
                {
                    Console.Clear();
                }

                if (runningTasks.Count < chunkSize)
                {
                    if (!endTokenRecived)
                    {
                        if (token.HasValue())
                        {
                            getAccountsToRun(token.Value(), out maxAccounts); // sets queue of account strings if token is passed
                        }
                        else
                        {
                            accountsQueue.Enqueue(connectionString.Value()); // else pass in connectionString
                            endTokenRecived = true;
                        }

                        foreach (var account in accountsQueue)
                        {
                            productQueue.Add(account, getProductsToRun(account));
                        }
                    }
                    while (accountsQueue.Count > 0 & runningTasks.Count < chunkSize)
                    {
                        string account = accountsQueue.Peek();
                        while (productQueue[account].Count > 0 & runningTasks.Count < chunkSize)
                        {
                            var product = productQueue[account].Dequeue();

                            Task<ProcessCommands> proccmd = Task.Run(() => handleProcess(account, product));

                            Thread.Sleep(2); //sleep to give the server some time between multiple products of the same account hitting prediction_data at once

                            proccmd.Name($"{Regex.Split(account, "[^a-zA-Z0-9._-]+").ElementAt(5)} {product}");
                            proccmd.StartTime();

                            runningTasks.Enqueue(proccmd);
                        }
                        if (productQueue[account].Count == 0)
                        {
                            accountsQueue.Dequeue();
                        }
                    }
                }

                Queue<Task<ProcessCommands>> tempRunningTasks = new Queue<Task<ProcessCommands>>();
                foreach (var proccmd in runningTasks)
                {
                    if (proccmd.Status == TaskStatus.RanToCompletion && proccmd.Result.exitCode == 0)
                    {
                        if (remote.HasValue())
                        {
                            Console.WriteLine($"Success: {this.Name} on: {proccmd.Result.token} {proccmd.Result.productId} at: {DateTime.Now}");
                        }
                    }
                    else if (proccmd.Status != TaskStatus.RanToCompletion)
                    {
                        tempRunningTasks.Enqueue(proccmd);
                        continue;
                    }

                    proccmd.Result.failed = false;

                    if (proccmd.Result.exitCode != 0 || proccmd.Status == TaskStatus.Faulted)
                    {
                        proccmd.Result.failed = true;
                        failedAccounts.Add(proccmd);
                        if (remote.HasValue())
                        {
                            Console.WriteLine($"Fail: {this.Name} on: {proccmd.Result.token} {proccmd.Result.productId} at: {DateTime.Now}");
                        }
                    }
                }
                if (!remote.HasValue())
                {
                    monitorProcesses(accountsQueue.Count + runningTasks.Count, maxAccounts,
                            DateTime.Now - startTime, runningTasks);
                }

                runningTasks = tempRunningTasks;

                if (accountsQueue.Count == 0 && tempRunningTasks.Count == 0 && endTokenRecived == true)
                {
                    if (failedAccounts.Any())
                    {
                        await printFailedAccounts(failedAccounts);
                    }
                    break;
                }

                Thread.Sleep(1);
            }
        }

        protected virtual void getAccountsToRun(string token, out int maxAccounts)
        {
            endTokenRecived = true;
            accountsQueue = connection.ConnectionStrings(token);
            maxAccounts = accountsQueue.Count();
        }
        protected virtual Queue<int> getProductsToRun(string account)
        {
            Queue<int> productQueue = new Queue<int>();
            DbConnectionManager connection = new DbConnectionManager(account, Regex.Split(account, "[^a-zA-Z0-9._-]+").ElementAt(5));
            productQueue = connection.getProducts(productID.HasValue() ? productID.Value() : "all_products");
            return productQueue;
        }
        private async Task<ProcessCommands> handleProcess(string connectionString, int productId)
        {
            ProcessCommands clientCommand = new ProcessCommands()
            {
                connectionString = connectionString,
                token = Regex.Match(connectionString, "[^/]+(?=/$|$)").ToString(),
                productId = productId
            };
            clientCommand.finished(await launchCommandAsync(clientCommand));
            return clientCommand;
        }

        private async Task printFailedAccounts(List<Task<ProcessCommands>> failedAccounts)
        {
            Log log = new Log();
            ConsoleTable failedTable = new ConsoleTable("Accounts - Error", "product ID", "ErrorCode");
            foreach (var acc in failedAccounts)
            {
                failedTable.AddRow(acc.Result.token, acc.Result.productId, acc.Result.exitCode);
            }
            await log.writeAsync("Command", "Error codes Triggered", failedTable);
            if (!remote.HasValue())
            {
                failedTable.Write();
            }
            Console.WriteLine(
                            "Some Accounts Encountered an Error, Please Check the logs for the above accounts.");
        }
        private void monitorProcesses(int itemsInQueue, int maxAccounts, TimeSpan time,
                Queue<Task<ProcessCommands>> runningTasks)
        {
            ConsoleTable header =
                    new ConsoleTable("Number Of Accounts Waiting To Be Processed:", "Time Elapsed");
            var top = Console.CursorTop;
            header.AddRow($"{itemsInQueue}/{maxAccounts}", time);
            header.Write();
            ConsoleTable tableOfTasks = new ConsoleTable("Account [Token] [Pro_ID]", "command", "Duration");

            runningTasks.ToList()
                    .ForEach(
                            iprocess =>
                            {
                                var processDuration = iprocess.Duration();
                                if (processDuration != null)
                                    tableOfTasks.AddRow(
                                                        iprocess.Name(),
                                                        this.Name,
                                                        string.Format("{0:00}:{1:00}:{2:00}",
                                                        processDuration.Value.Hours,
                                                        processDuration.Value.Minutes,
                                                        processDuration.Value.Seconds
                                                )
                                        );
                            }
                    );

            tableOfTasks.Write();
        }
        public void disposeProcess(Task<ProcessCommands> process)
        {
            process.Result.processResults.Dispose();
        }
        public abstract void construct();
        public abstract int runCommands();
        public abstract void setExecute();
        public abstract Task<ProcessResults> launchCommandAsync(ProcessCommands clientCommand);
    }

}
