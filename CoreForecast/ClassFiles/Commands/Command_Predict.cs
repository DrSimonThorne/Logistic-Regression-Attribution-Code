using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CoreForecast.ClassFiles;
using McMaster.Extensions.CommandLineUtils;
namespace CoreForecast.ClassFiles.Commands
{
    class Command_Predict : BuildDataObject
    {
        protected CommandOption connectionString;
        protected CommandOption productID;
        protected CommandOption startDate;
        protected CommandOption endDate;
        public Command_Predict()
        {
            construct();
            setExecute();
        }
        public override void construct()
        {
            Name = "predict";
            Description = "reads weighting for account and product the runs predictions for the given date range";
            connectionString = Option("-c | --connection", "target connection for command", CommandOptionType.SingleValue);
            productID = Option("-p | --productid", "target product for command", CommandOptionType.SingleValue);
            startDate = Option("-s | --startdate <startdate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
            endDate = Option("-e | --enddate <enddate>", "{yyyy-mm-dd} + {hh:mm:ss}.", CommandOptionType.SingleValue);
        }
        public override void setExecute()
        {
            OnExecute((Func<int>)runCommands);
        }
        public override int runCommands()
        {
            getConnection(connectionString.Value(), startDate.Value(), endDate.Value(), productID.HasValue() ? productID.Value() : "all_products");
            setup();
            if (loadWeights())
            {
                foreach (DateTime day in loopRange(Convert.ToDateTime(startDate.Value()), Convert.ToDateTime(endDate.Value())))
                {
                    getConnection(
                        connectionString.Value(),
                        day.ToString(format),
                        day.AddHours(23).AddMinutes(59).AddSeconds(59).ToString(format),
                        productID.HasValue() ? productID.Value() : "all_products"
                    );

                    if (loadData())
                    {
                        predict();
                    }
                }
            }
            return 0;
        }
        public bool loadWeights()
        {
            weights = connection.getWeights(endDate.HasValue() ? endDate.Value() : DateTime.Now.ToString(format));
            if (weights.Length > 0)
            {
                return true;
            }
            return false;
        }
        public override bool loadData()
        {
            likelihoodData = new List<PredictionData>();
            likelihoodData = connection.selectPredictionData(likelihoodData, frequency, productID.Value());
            if (likelihoodData.Count > 0)
            {
                return true;
            }
            return false;
        }
        public void predict()
        {
            foreach (var visitor in likelihoodData)
            {
                rawLikelihoodData = convertPredict(visitor);
                RegressionPredictor regPred = new RegressionPredictor(rawLikelihoodData.Length);
                visitor.likelihood = regPred.predict(rawLikelihoodData, weights);
            }
            insert(likelihoodData);
        }
        public override void insert(List<PredictionData> likelihoodData)
        {
            connection.insertLiklihood(likelihoodData);
        }
    }
}