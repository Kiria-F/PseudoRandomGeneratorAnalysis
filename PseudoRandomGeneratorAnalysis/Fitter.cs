﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PseudoRandomGeneratorAnalysis {
    internal class Fitter {
        private readonly Random random = new Random();
        public int gensCount = 100;
        public int genSize = 150;
        public int selectedGenSize = 30;

        public double[] Fit( double[] paramNames, double[] minVals, double[] maxVals, Func<double[], double> minFun, Action<string> logsOutput, Action<double> progressOutput) {
            int paramsCount = paramNames.Length;
            Sheep[] generation = new Sheep[genSize];
            for (int sheepI = 0; sheepI < genSize; sheepI++) {
                for (int paramI = 0; paramI < paramsCount; paramI++) {
                    generation[sheepI][paramI] = (maxVals[paramI] - minVals[paramI]) * random.NextDouble() + minVals[paramI];
                }
            }

            Sheep[] selection;
            Sheep leader = new Sheep(new double[] { 0, 0, 0 });

            for (int genI = 0; genI < gensCount; genI++) {
                for (int sheepI = 0; sheepI < genSize; sheepI++)
                {
                    generation[sheepI].Rating = minFun(generation[sheepI].parameters);
                }
                progressOutput(0D);
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
                for (int i = 0; i < generation.Length; i++)  // генерирование следующего поколения
                {
                    Sheep sheepA = selection[random.Next(selection.Length)];
                    Sheep sheepB = selection[random.Next(selection.Length)];
                    generation[i] = Sheep.Reproduce(sheepA, sheepB);
                }
            }
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
            for (int i = 0;i < newParameters.Length;i++) {
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
