using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
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

        public static double[] Fit(string[] paramNames, double[] minVals, double[] maxVals, Func<double[], double> minFun, Action<string> logsOutput, Action<double> progressOutput) {
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
