using System;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using CoreForecast.ClassFiles;
using CoreForecast.ClassFiles.Extensions;
using McMaster.Extensions.CommandLineUtils;
namespace CoreForecast.ClassFiles.Commands
{
    class Command_Train : BuildDataObject
    {
        private CommandOption connectionString;
        private CommandOption productID;
        private CommandOption startDate;
        private CommandOption endDate;
        private int trainSplit = 80;
        private int cvSplit;
        public Command_Train()
        {
            construct();
            setExecute();
        }
        public override void construct()
        {
            Name = "train";
            Description = "build the data object, split out the test and the validation data then trains.";
            connectionString = Option("-c | --connection", "target connection for command", CommandOptionType.SingleValue);
            productID = Option("-p | --productid", "target product for command", CommandOptionType.SingleValue);
            startDate = Option("-s | --startdate <startdate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
            endDate = Option("-e | --enddate <enddate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);

            HelpOption("-h | -? | --help");
        }
        public override void setExecute()
        {
            OnExecute((Func<int>)runCommands);
        }
        public override int runCommands()
        {

            getConnection(connectionString.Value(), startDate.Value(), endDate.Value(), productID.HasValue() ? productID.Value() : "all_products");
            setup();
            try
            {
                if (loadData())
                {
                    this.weights = train();
                }
            }
            catch (System.NullReferenceException)
            {
                Console.WriteLine("No Test/ Train data was found, please check source DB for relevent data / change -cs & -ce date ranges.");

            }

            if (this.weights != null)
            {
                handleInsertWeights(this.weights, this.accuracy);
            }

            return 0;
        }
        public override bool loadData()
        {
            var tempData = new List<PredictionData>();
            var tempSalesData = new List<PredictionData>();
            var tempNoneSalesData = new List<PredictionData>();

            tempSalesData = connection.selectSalesData(frequency);
            tempNoneSalesData = connection.selectNoneSalesData(frequency);

            tempSalesData.Shuffle();
            tempNoneSalesData.Shuffle();

            int data_limiter = Math.Min(tempSalesData.Count(), tempNoneSalesData.Count());

            for (int i = 0; i < data_limiter; i++)
            {
                if (tempSalesData.Count() >= data_limiter)
                {
                    tempData.Add(tempSalesData[i]);
                }
                if (tempNoneSalesData.Count() >= data_limiter)
                {
                    tempData.Add(tempNoneSalesData[i]);
                }
            }

            //tempData.AddRange(tempSalesData);
            //tempData.AddRange(tempNoneSalesData);

            if (tempData.Count > 0)
            {
                getTrainAndCvData(tempData);
                tempData = new List<PredictionData>();

                rawTrainData = convertTrainObjToArray(trainData);
                trainData = new List<PredictionData>();

                rawCvData = convertTrainObjToArray(cvData);
                cvData = new List<PredictionData>();

                if (rawCvData.Length > 0 && rawTrainData.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }
        public void getTrainAndCvData(List<PredictionData> tempData)
        {
            tempData.Shuffle();
            var partitionedData = tempData.Partition(trainSplit * tempData.Count / 100);
            // TODO: handle partitionedData not being split into two by combineing whatever number was generated.
            if (partitionedData.Count() == 2)
            {
                trainData = new List<PredictionData>(partitionedData.ElementAt(0));
                cvData = new List<PredictionData>(partitionedData.ElementAt(1));
            }
        }
        public double[] train()
        {
            if (rawCvData != null && rawTrainData != null)
            {
                RegressionPredictor regPred = new RegressionPredictor(rawTrainData[0].Length - 1);

                this.weights = regPred.train(rawTrainData, rawCvData, 1000000, 0.001);
                this.accuracy = regPred.accuracy(rawCvData, weights);
            }
            return weights;
        }
        public void handleInsertWeights(double[] weights, List<double> accuracy)
        {

            connection.insertWeighting(factorIDs, weights, accuracy, endDate.HasValue() ? endDate.Value() : DateTime.Now.ToString(format));
        }
        public static void writeWeights(double[] weights, string path, string fileName)
        {
            Directory.CreateDirectory(path);
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Join(",", weights));
            using (StreamWriter sw = new StreamWriter(
                            new FileStream(
                                    Path.GetFullPath(Path.Combine(path, fileName)), FileMode.Create)))
            {
                sw.WriteLine(sb.ToString());
            }
        }
    }
}