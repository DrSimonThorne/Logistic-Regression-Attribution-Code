using System;
using System.Text;
using System.Collections.Generic;

namespace CoreForecast.ClassFiles
{
    public class RegressionPredictor
    {
        private int numFeatures; // number of x variables aka features
        private double[] weights; // b0 = constant
        private double[] bestWeights;
        private int bestEpoch;
        private double lastBestError;
        private Random rnd;

        public RegressionPredictor(int numFeatures)
        {
            this.numFeatures = numFeatures;
            weights = new double[numFeatures + 1];
            rnd = new Random(0);
        }

        public double[] train(double[][] trainData, double[][] crossVData, int maxEpochs, double alpha)
        {
            // alpha is the learning rate
            Console.WriteLine($"cv data length: {crossVData.Length}");
            int epoch = 0;
            int[] sequence = new int[trainData.Length]; // random order
            for (int i = 0; i < sequence.Length; ++i)
                sequence[i] = i;

            while (epoch < maxEpochs)
            {
                ++epoch;
                int errInterval = 100; // interval to check validation data
                if (epoch % errInterval == 0 && epoch <= maxEpochs)
                {
                    double mse = error(crossVData, weights);
                    Console.WriteLine("\t epoch = " + epoch + "\terror =\t" + mse.ToString("F4"));
                }

                shuffle(sequence); // process data in random order

                for (int ti = 0; ti < trainData.Length; ++ti)
                {
                    int i = sequence[ti];
                    double computed = computeOutput(trainData[i], weights);
                    int targetIndex = trainData[i].Length - 1;
                    double target = trainData[i][targetIndex];

                    weights[0] += alpha * (target - computed) * computed * (1 - computed) * 1;

                    for (int j = 1; j < weights.Length - 1; ++j)
                    {
                        weights[j] += alpha * (target - computed) * computed * (1 - computed) * trainData[i][j - 1];
                    }
                }
                if (findGlobalMinimum(error(crossVData, weights)))
                {
                    Console.WriteLine("\t Best epoch =\t" + epoch);
                    return weights;
                }
            } // while
            Console.WriteLine("\t Best weights not found in: =\t" + epoch + "\t attempts.");
            return weights; // by ref is somewhat risky
        } // Train
        private bool findGlobalMinimum(double mse)
        {
            //double difference = Math.Abs(mse * .000275);
            double difference = Math.Abs(0.0000005);
            if (Math.Abs(mse - lastBestError) <= difference)
            {
                return true;
            }
            lastBestError = mse;
            return false;
        }
        private void shuffle(int[] sequence)
        {
            for (int i = 0; i < sequence.Length; ++i)
            {
                int r = rnd.Next(i, sequence.Length);
                int tmp = sequence[r];
                sequence[r] = sequence[i];
                sequence[i] = tmp;
            }
        }

        private double error(double[][] data, double[] weights)
        {
            // mean squared error using supplied weights
            int yIndex = data[0].Length - 1; // y-value (0/1) is last column
            double sumSquaredError = 0.0;
            for (int i = 0; i < data.Length; ++i) // each data
            {
                double computed = computeOutput(data[i], weights);
                double desired = data[i][yIndex]; // ex: 0.0 or 1.0
                sumSquaredError += (computed - desired) * (computed - desired);
            }
            Console.WriteLine(sumSquaredError / data.Length);
            return sumSquaredError / data.Length;
        }

        private double computeOutput(double[] dataItem, double[] weights)
        {
            double z = 0.0;
            z += weights[0]; // the b0 constant
            for (int i = 0; i < dataItem.Length - 1; ++i) // data might include Y
                z += (weights[i + 1] * dataItem[i]); // skip first weight
            return 1.0 / (1.0 + Math.Exp(-(z)));
        }
        private double computePrediction(double[] dataItem, double[] weights)
        {
            double y = 0.0;
            y = computeOutput(dataItem, weights); // 0.0 to 1.0
            return y;
        }
        private int computeDependent(double[] dataItem, double[] weights)
        {
            double y = computeOutput(dataItem, weights); // 0.0 to 1.0
            if (y >= 0.50)
                return 1;
            if (y <= 0.50)
                return 0;
            return 2;
        }

        public List<double> accuracy(double[][] data, double[] weights)
        {
            double[] results = new double[data.Length];
            int numCorrect = 0;
            int numWrong = 0;
            int pospos = 0;
            int posfalse = 0;
            int falsepos = 0;
            int falsefalse = 0;
            int yIndex = data[0].Length - 1;
            List<double> accuracy = new List<double>();
            for (int i = 0; i < data.Length; ++i)
            {
                int target = (int)data[i][yIndex]; // risky?         
                int computed = computeDependent(data[i], weights);
                results[i] = computed;
                if (computed == target && target == 1)
                {
                    pospos++;
                    numCorrect++;
                }
                else if (computed != target && target == 1)
                {
                    falsepos++;
                    numWrong++;
                }
                else if (computed != target && target == 0)
                {
                    falsefalse++;
                    numWrong++;
                }
                else if (computed == target && target == 0)
                {
                    posfalse++;
                    numCorrect++;
                }
            }
            Console.WriteLine("\t true:");
            Console.WriteLine("\t\t correct:\t" + pospos);
            Console.WriteLine("\t\t incorrect:\t" + falsepos);
            float perc1 = 0;
            float perc0 = 0;
            perc1 = ((float)pospos / ((float)pospos + (float)falsepos)) * 100;
            Console.WriteLine("\t\t\t " + perc1 + "%");

            Console.WriteLine("\t false:");
            Console.WriteLine("\t\t correct:\t" + posfalse);
            Console.WriteLine("\t\t incorrect:\t" + falsefalse);
            perc0 = ((float)posfalse / ((float)posfalse + (float)falsefalse)) * 100;

            Console.WriteLine("\t\t\t " + perc0 + "%");
            if (Single.IsNaN(perc1))
            {
                perc1 = 0;
            }
            if (Single.IsNaN(perc0))
            {
                perc0 = 0;
            }

            accuracy.Add(perc1);
            accuracy.Add(perc0);
            return accuracy;
        }
        public double predict(double[] data, double[] weights)
        {
            return computePrediction(data, weights);
        }
    }
}
