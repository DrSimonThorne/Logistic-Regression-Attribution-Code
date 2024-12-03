using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CoreForecast.ClassFiles.Extensions;
using CoreForecast.ClassFiles.Interfaces;
using McMaster.Extensions.CommandLineUtils;

namespace CoreForecast.ClassFiles
{
	abstract class BuildDataObject : CommandLineApplication, ICommand
	{
		protected DbConnectionManager connection { get; set; }
		protected double[] weights { get; set; }
		protected List<int> factorIDs { get; set; }
		protected List<double> accuracy { get; set; }
		protected List<PredictionData> trainData { get; set; }
		protected List<PredictionData> cvData { get; set; }
		protected List<PredictionData> likelihoodData { get; set; }
		protected double[][] rawTrainData { get; set; }
		protected double[][] rawCvData { get; set; }
		protected double[] rawLikelihoodData { get; set; }
		protected FrequencyData frequency { get; set; }
		private int numberOfFactors { get; set; }
		private Dictionary<int, int> trackedEventFactorKeys { get; set; }
		private Dictionary<int, int> trackedOtherEventFactorKeys { get; set; }
		private List<int> activeEvents { get; set; }
		protected string pathToClients { get; private set; }
		protected const string format = "yyyy-MM-dd HH:mm:ss";

		protected void setup()
		{
			this.frequency = new FrequencyData(connection);
			getFactorIDs();
		}
		protected void getConnection(string connectionString, string productId)
		{
			pathToClients = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "data"));
			this.connection = new DbConnectionManager(connectionString, productId);
		}
		protected void getConnection(string connectionString, string startdate, string enddate, string productId)
		{
			pathToClients = Path.GetFullPath(Path.Combine("..", "..", "..", "src", "data"));
			this.connection = new DbConnectionManager(connectionString, startdate, enddate, productId);
		}
		protected double[] convertPredict(PredictionData obj)
		{
			double[] array = obj.convertToDoublePredict().Values.ToArray();
			return array;
		}
		public double[][] convertTrainObjToArray(List<PredictionData> visitors)
		{
			List<double[]> visitorList = new List<double[]>();

			foreach (var visitor in visitors)
			{
				if (visitor.containsOutier == false)
				{
					visitorList.Add(visitor.convertToDoubleTrain().Values.ToArray());
				}
			}

			double[][] visitorMatrix = new double[visitorList.Count][];
			for (int i = 0; i < visitorList.Count(); i++)
			{
				visitorMatrix[i] = visitorList[i];
			}
			return visitorMatrix;
		}
		protected double[] removeSaleEventsFromTrain(double[] inputData)
		{
			List<double> temp = new List<double>(inputData.Length);
			for (int i = 0; i < activeEvents.Count; i++)
			{
				if (activeEvents[i] == 0)
				{
					temp.Add(inputData[i]);
				}
			}
			for (int i = (inputData.Length - trackedOtherEventFactorKeys.Count) - 1; i < inputData.Length; i++)
			{
				temp.Add(inputData[i]);
			}
			double[] updatedOutData = new double[temp.Count];
			for (int i = 0; i < temp.Count; i++)
			{
				updatedOutData[i] = temp[i];
			}
			return updatedOutData;
		}

		public void getFactorIDs()
		{
			factorIDs = connection.getInsertFactorIDs();
		}
		protected static IEnumerable<DateTime> loopRange(DateTime from, DateTime thru)
		{
			for (var day = from.Date; day.Date <= thru.Date; day = day.AddDays(1))
				yield return day;
		}
		public abstract void construct();
		public abstract int runCommands();
		public abstract bool loadData();
		public abstract void setExecute();
		public virtual void insert(List<PredictionData> list) {}
	}
}