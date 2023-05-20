using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PseudoRandomGeneratorAnalysis {
    internal static class Fitter {
        private static readonly Random random = new Random();
        public static int gensCount = 100;
        public static int genSize = 150;
        public static int selectedGenSize = 30;

        public static int iterations = 1000;
        public static double stepMultiplier = 0.95;

        public static double[] FitGrad(
                string[] paramNames,
                double[] startPos,
                double siMin,
                double siMax,
                Func<double[], double> minFun,
                Action<string> logsOutput,
                Action<double> progressOutput,
                Action<Dictionary<string, double>> leaderControl) {
            int paramsCount = paramNames.Length;
            double[] pos = startPos.Clone() as double[];
            double step = 0.0001;
            double minY = double.NaN;
            double lossDelta = double.NaN;
            for (int iteration = 0; true; iteration++) {
                for (int direction = 0; direction < paramsCount; direction++) {
                    var cancellationTokenSource = new CancellationTokenSource();
                    var cancellationToken = cancellationTokenSource.Token;
                    Task task = Task.Run(() => {
                        pos[direction] = FindClosestMin(
                            cancellationToken,
                            (double x) => {
                                try {
                                    pos[direction] = x;
                                } catch (IndexOutOfRangeException) {
                                    logsOutput(" ==Error== \n");
                                }
                                //si = si + (siMax - siMin) * 0.05;
                                //if (si > siMax) {
                                //    si = si - siMax - siMin;
                                //}
                                return minFun(pos);
                            },
                            pos[direction],
                            step,
                            progressOutput,
                            out double minYInner);
                        lossDelta = minYInner - minY;
                        minY = minYInner;
                    }, cancellationToken);
                    if (!task.Wait(TimeSpan.FromSeconds(120))) {
                        cancellationTokenSource.Cancel();
                        logsOutput(" ==Canceled== \n");
                        task.Wait();
                    }
                    task.Dispose();
                    string stats = "";
                    for (int paramI = 0; paramI < paramsCount; paramI++) {
                        stats += paramNames[paramI] + " : " + pos[paramI] + "\n";
                    }
                    logsOutput(
                        "ITERATION " + " : " + iteration +
                        "\nStep = " + step +
                        "\n" + stats +
                        "Loss: " + minY +
                        "\nLoss delta: " + lossDelta + "\n\n");
                    progressOutput((double)iteration / iterations * direction / paramsCount);
                }
                step *= stepMultiplier;
            }
        }

        private static double FindClosestMin(
                CancellationToken cancellationToken,
                Func<double, double> function,
                double startPos,
                double step,
                Action<double> progressOutput,
                out double minY) {
            double minX = startPos;
            double progress = 0;
            minY = function(startPos);
            if (cancellationToken.IsCancellationRequested) return minX;
            if (function(startPos + step) > minY) {
                step *= -1;
            }
            if (cancellationToken.IsCancellationRequested) return minX;
            progress = progress + (1 - progress) * 0.1;
            progressOutput(progress);
            double x = minX + step;
            double y = function(x);
            while (y < minY) {
                if (cancellationToken.IsCancellationRequested) return minX;
                minX = x;
                minY = y;
                x += step;
                y = function(x);
                progress = progress + (1 - progress) * 0.1;
                progressOutput(progress);
            }
            return minX;
        }

        public static double[] FitGen(
                string[] paramNames,
                double[] minVals,
                double[] maxVals,
                Func<double[], double> minFun,
                Action<string> logsOutput,
                Action<double> progressOutput,
                Action<Dictionary<string, double>> leaderControl) {
            int paramsCount = paramNames.Length;
            Sheep[] generation = new Sheep[genSize];
            for (int sheepI = 0; sheepI < genSize; sheepI++) {
                double[] parameters = new double[paramsCount];
                for (int paramI = 0; paramI < paramsCount; paramI++) {
                    parameters[paramI] = (maxVals[paramI] - minVals[paramI]) * random.NextDouble() + minVals[paramI];
                }
                generation[sheepI] = new Sheep(parameters);
            }

            Sheep[] selection;
            Sheep leader = new Sheep(new double[] { 0, 0, 0 });

            for (int genI = 0; genI < gensCount; genI++) {
                object lockObject = new object();
                double progress = 0;
                Parallel.ForEach(generation, new ParallelOptions { MaxDegreeOfParallelism = 8 }, sheep => {
                    sheep.Rating = minFun(sheep.parameters);
                    progress += 1D / genSize;
                    progressOutput(progress);
                });

                Array.Sort(generation);
                generation.Reverse();
                selection = generation.Take(selectedGenSize).ToArray();
                leader = selection[0];
                Dictionary<string, double> leaderParams = new Dictionary<string, double>();
                for (int i = 0; i < paramsCount; i++) {
                    leaderParams.Add(paramNames[i], leader[i]);
                }
                leaderControl(leaderParams);
                double selectionMeanRating = selection.Average(sheep => sheep.Rating);
                double[] paramsMins = new double[paramsCount];
                double[] paramsMaxes = new double[paramsCount];
                for (int paramI = 0; paramI < paramsCount; paramI++) {
                    paramsMins[paramI] = selection.Min(sheep => sheep[paramI]);
                    paramsMaxes[paramI] = selection.Max(sheep => sheep[paramI]);
                }
                {
                    string selectionMeanStats = "";
                    string leaderStats = "";
                    for (int paramI = 0; paramI < paramsCount; paramI++) {
                        selectionMeanStats += "\n" + paramNames[paramI] + " ∈ [ " + paramsMins[paramI] + ", " + paramsMaxes[paramI] + " ]";
                        leaderStats += "\n  " + paramNames[paramI] + " = " + leader[paramI];
                    }
                    logsOutput(
                    "GEN " + genI + ":" +
                    "\nMean rating = " + selectionMeanRating +
                    selectionMeanStats +
                    "\nLeader:" +
                    "\n  Rating = " + leader.Rating +
                    leaderStats +
                    "\n\n");
                }
                for (int i = 0; i < generation.Length; i++) {
                    Sheep sheepA = selection[random.Next(selection.Length)];
                    Sheep sheepB = selection[random.Next(selection.Length)];
                    generation[i] = Sheep.Reproduce(sheepA, sheepB);
                }
                // progressOutput((double)genI / gensCount);
            }
            logsOutput("\n\n  << DONE >>\n");
            progressOutput(0D);
            return leader.parameters;
        }
    }

    class Sheep : IComparable<Sheep> {
        private static readonly Random random = new Random();
        public double[] parameters;
        public double Rating;

        public Sheep(double[] parameters) {
            this.parameters = parameters;
            Rating = 0f;
        }

        public static Sheep Reproduce(Sheep sheepA, Sheep sheepB) {
            int paramsCount = sheepA.parameters.Length;
            double[] newParams = new double[paramsCount];
            for (int paramIndex = 0; paramIndex < paramsCount; paramIndex++) {
                bool isNegative = sheepA[paramIndex] < 0;
                string geneA = Convert.ToString((long)Math.Abs(sheepA[paramIndex] * 1_000_000_000D), 2);
                string geneB = Convert.ToString((long)Math.Abs(sheepB[paramIndex] * 1_000_000_000D), 2);
                while (geneA.Length > geneB.Length) {
                    geneB = "0" + geneB;
                }
                while (geneB.Length > geneA.Length) {
                    geneA = "0" + geneA;
                }
                int crossPoint = (int)(random.NextDouble() * random.NextDouble() * geneA.Length + 0.8D);
                string crossGeneStr = geneA.Substring(0, crossPoint) + geneB.Substring(crossPoint);
                newParams[paramIndex] = Convert.ToInt64(crossGeneStr, 2);
                newParams[paramIndex] /= 1_000_000_000D;
                if (isNegative) {
                    newParams[paramIndex] *= -1;
                }
            }
            return new Sheep(newParams);
        }

        public static Sheep Reproduce_OLD(Sheep sheepA, Sheep sheepB) {
            double[] props = new double[sheepA.parameters.Length];
            for (int i = 0; i < props.Length; i++) {
                props[i] = random.NextDouble();
            }
            double[] newParameters = new double[sheepA.parameters.Length];
            for (int i = 0; i < newParameters.Length; i++) {
                newParameters[i] = sheepA.parameters[i] * props[i] + sheepB.parameters[i] * (1D - props[i]);
            }
            return new Sheep(newParameters);
        }

        public override string ToString() {
            string output = "[";
            for (int i = 0; i < parameters.Length; i++) {
                output += parameters[i].ToString() + ", ";
            }
            output = output.TrimEnd(',');
            output += "] | Rating: " + Rating.ToString();
            return output;
        }

        public int CompareTo(Sheep sheep) {
            return Rating.CompareTo(sheep.Rating);
        }

        public double this[int index] {
            get { return parameters[index]; }
            set { parameters[index] = value; }
        }
    }
}
