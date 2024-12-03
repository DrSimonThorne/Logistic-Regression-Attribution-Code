using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CoreForecast.ClassFiles;
using McMaster.Extensions.CommandLineUtils;
namespace CoreForecast.ClassFiles.Commands
{
    class Command_Sync_Predict : Command_Predict
    {
        public override void construct()
        {
            Name = "Sync_Predict";
            Description = "Overries predict to only pull from agg_prediction_journey_sync";
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
            getConnection(connectionString.Value(), productID.HasValue() ? productID.Value() : "all_products");
            setup();
            if (loadWeights())
            {
                getConnection(
                    connectionString.Value(),
                    productID.HasValue() ? productID.Value() : "all_products"
                );

                if (loadData())
                {
                    predict();
                }
            }
            return 0;
        }

        public override bool loadData()
        {
            likelihoodData = new List<PredictionData>();
            likelihoodData = connection.selectPredictionDataSync(likelihoodData, frequency, productID.Value());
            if (likelihoodData.Count > 0)
            {
                return true;
            }
            return false;
        }
        public override void insert(List<PredictionData> likelihoodData)
        {
            connection.insertLiklihood(likelihoodData);
        }
    }
}