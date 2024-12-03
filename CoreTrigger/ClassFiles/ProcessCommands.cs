using System;
using System.Collections.Generic;

namespace CoreTrigger.ClassFiles
{
        public class ProcessCommands
        {
                public string connectionString { get; set; }
                public string token { get; set; }
                public int productId { get; set; }
                public string action { get; set; }
                public string progress { get; set; }
                public DateTime startTime { get; set; }
                public TimeSpan runTime { get; set; }
                public int exitCode { get; set; }
                public bool failed { get; set; }
                public ProcessResults processResults { get; set; }

                public List<String> StandardError { get; set; }
                public List<String> StandardOutput { get; set; }


                public ProcessCommands()
                {
                        startTime = DateTime.Now;
                        failed = false;
                        StandardError = new List<string>();
                        StandardOutput = new List<string>();
                }
                public void finished(ProcessResults results)
                {
                        this.processResults = results;
                        progress = "Finished.";
                        exitCode = results.ExitCode;
                        runTime = (DateTime.Now - startTime);
                        this.StandardError.AddRange(results.StandardError);
                        this.StandardOutput.AddRange(results.StandardOutput);
                }
                public TimeSpan getRunTime()
                {
                        this.runTime = DateTime.Now - startTime;
                        return this.runTime;
                }

        }
}
