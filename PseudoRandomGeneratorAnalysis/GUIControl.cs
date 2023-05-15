using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Xml.Xsl;
using System.Windows.Forms.VisualStyles;
using static System.Net.Mime.MediaTypeNames;
// using System.Reflection.Emit;

namespace PseudoRandomGeneratorAnalysis {

    public partial class GUIControl : Form {
        private Pen pen = new Pen(Color.FromArgb(100, 0, 0, 0), 1);
        private int lastProgressVal = -1;

        private Generator[] generators = new Generator[] {
            new GrandCLTGenerator(),
            new DynamicNCLTGenerator(),
            new ConstNCLTGenerator()
        };

        public GUIControl() {
            InitializeComponent();
            // DistributionChart.ChartAreas["DistributionArea"].AxisX.Minimum = 0;

            foreach (Generator generator in generators) {
                GeneratorChoose.Items.Add(generator.name);
            }
            GeneratorChoose.SelectedIndex = 0;
        }

        private String SplitLongNumber(String num) {
            int spacesCount = (num.Length - 1) / 3;
            for (int i = spacesCount - 1; i >= 0; i--) {
                num = num.Insert(num.Length - 3 * (i + 1), " ");
            }
            return num;
        }

        private void ClearCharts() {
            DistributionChart.Series.Clear();
            IntegralChart.Series.Clear();
            QualityChart.Series.Clear();
        }

        private void ClearStats() {
            LabelM.Text = "___";
            LabelD.Text = "___";
            LabelSi.Text = "___";
            LabelIn1.Text = "___";
            LabelIn2.Text = "___";
            LabelIn3.Text = "___";
            LabelTime.Text = "___";
        }

        private void AddDistributionDataToChart(Dictionary<int, ulong> data, ulong randCount) {
            System.Windows.Forms.DataVisualization.Charting.Series newSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            newSeries.ChartArea = "DistributionArea";
            //newSeries.LabelForeColor = System.Drawing.Color.BlanchedAlmond;
            newSeries.Name = "distribution_" + DistributionChart.Series.Count;
            newSeries.YValuesPerPoint = 2;
            foreach (KeyValuePair<int, ulong> i in data) {
                newSeries.Points.AddXY(i.Key, (double)i.Value / randCount);
            }
            DistributionChart.Series.Add(newSeries);
        }

        private void AddIntegralDataToChart(Dictionary<int, ulong> data, ulong randCount) {
            System.Windows.Forms.DataVisualization.Charting.Series newSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            newSeries.BackSecondaryColor = System.Drawing.Color.White;
            newSeries.BorderColor = System.Drawing.Color.White;
            newSeries.BorderWidth = 2;
            newSeries.ChartArea = "IntegralArea";
            newSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.StepLine;
            //newSeries.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            newSeries.Name = "integral_" + IntegralChart.Series.Count;
            ulong graphIncr = 0;
            foreach (KeyValuePair<int, ulong> i in data.OrderBy(i => i.Key)) {
                graphIncr += i.Value;
                newSeries.Points.AddXY(i.Key, (double)graphIncr / randCount);
            }
            IntegralChart.Series.Add(newSeries);
        }

        private void AddQualityDataToChart(Dictionary<int, ulong> data, ulong randCount, Generator generator) {
            QualityChart.ChartAreas.First().AxisY.Maximum = DistributionChart.ChartAreas.First().AxisY.Maximum;

            System.Windows.Forms.DataVisualization.Charting.Series differenceSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            differenceSeries.ChartArea = "QualityArea";
            differenceSeries.Name = "difference_" + QualityChart.Series.Count;
            differenceSeries.YValuesPerPoint = 2;

            System.Windows.Forms.DataVisualization.Charting.Series perfectSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            perfectSeries.ChartArea = "DistributionArea";
            perfectSeries.Name = "perfect_" + QualityChart.Series.Count;
            perfectSeries.YValuesPerPoint = 2;
            perfectSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
            perfectSeries.Color = Color.Blue;
            perfectSeries.BorderWidth = 2;
            perfectSeries.Enabled = false;

            double maxPerfectValue = 0;
            ulong maxValue = 0;
            Dictionary<double, KVPair<ulong, double>> comparasions = new Dictionary<double, KVPair<ulong, double>>();
            foreach (KeyValuePair<int, ulong> i in data) {
                double perfectValue = generator.CoreFunction(i.Key);
                comparasions.Add(i.Key, new KVPair<ulong, double>(i.Value, perfectValue));
                if (perfectValue > maxPerfectValue) {
                    maxPerfectValue = perfectValue;
                }
                if (i.Value > maxValue) {
                    maxValue = i.Value;
                }
            }

            double scaler = (double)maxValue / randCount;
            foreach (KeyValuePair<double, KVPair<ulong, double>> i in comparasions) {
                double difference = Math.Abs((double)i.Value.Key / maxValue - i.Value.Value / maxPerfectValue) * scaler;
                differenceSeries.Points.AddXY(i.Key, difference);
                perfectSeries.Points.AddXY(i.Key, i.Value.Value / maxPerfectValue * scaler);
            }
            QualityChart.Series.Add(differenceSeries);
            perfectSeries.Sort(System.Windows.Forms.DataVisualization.Charting.PointSortOrder.Ascending, "X");
            DistributionChart.Series.Add(perfectSeries);
        }

        private void CalcStats(Dictionary<int, ulong> data, ulong randCount) {
            double m = 0, d = 0;
            foreach (KeyValuePair<int, ulong> i in data) {
                m += (double)i.Key * i.Value;
            }
            m /= randCount;
            foreach (KeyValuePair<int, ulong> i in data) {
                double underSqr = (double)i.Key - m;
                d += underSqr * underSqr * i.Value;
            }
            d /= randCount;
            double si = Math.Sqrt(d);
            double leftBorder1 = m - si * 1, rightBorder1 = m + si * 1;
            double leftBorder2 = m - si * 2, rightBorder2 = m + si * 2;
            double leftBorder3 = m - si * 3, rightBorder3 = m + si * 3;
            double in1Si = 0, in2Si = 0, in3Si = 0;
            foreach (KeyValuePair<int, ulong> i in data) {
                if (i.Key >= leftBorder3 && i.Key <= rightBorder3) {
                    in3Si += i.Value;
                    if (i.Key >= leftBorder2 && i.Key <= rightBorder2) {
                        in2Si += i.Value;
                        if (i.Key >= leftBorder1 && i.Key <= rightBorder1) {
                            in1Si += i.Value;
                        }
                    }
                }
            }
            LabelM.Text = m.ToString();
            LabelD.Text = d.ToString();
            LabelSi.Text = si.ToString();
            LabelIn1.Text = ((in1Si / randCount) * 100).ToString() + "%";
            LabelIn2.Text = ((in2Si / randCount) * 100).ToString() + "%";
            LabelIn3.Text = ((in3Si / randCount) * 100).ToString() + "%";
        }

        private void Run(bool rerun) {
            ulong randCount = (ulong)InputCount.Value;
            Generator generator = generators[GeneratorChoose.SelectedIndex];
            generator.CollectParameterValues();
            Dictionary<int, ulong> data = null;
            int seconds1 = 0;
            int millis1 = 0;
            int seconds2 = 0;
            int millis2 = 0;

            new Task(() => {
                ActiveForm.Invoke((Action)(() => {
                    EnableControls(false);
                }));
                generator.Prepare();
                seconds1 = DateTime.Now.Second;
                millis1 = DateTime.Now.Millisecond;
                data = generator.ISequence(randCount);
                seconds2 = DateTime.Now.Second;
                millis2 = DateTime.Now.Millisecond;
                if (seconds2 < seconds1) {
                    seconds2 += 60;
                }
                ActiveForm.Invoke((Action)(() => {
                    LabelTime.Text = (((double)(seconds2 * 1000 + millis2 - seconds1 * 1000 - millis1)) / 1000).ToString() + " s";
                    if (rerun) {
                        ClearCharts();
                    }
                    AddDistributionDataToChart(data, randCount);
                    AddIntegralDataToChart(data, randCount);
                    AddQualityDataToChart(data, randCount, generator);
                    CalcStats(data, randCount);
                    EnableControls(true);
                }));
            }).Start();
            
            /*.ContinueWith((task) => {
                MyConsole.Invoke(() => { 
                    ConsoleWrite((generator as DynamicNCLTGenerator).GetCalcedN().ToString() + "\n");
                });
            })*/
        }

        private void ButtonRun_Click(object sender, EventArgs e) {
            Run(false);
        }

        private void ConsoleSetProgress(int val1000) {
            if (val1000 == lastProgressVal) {
                return;
            }
            MyConsoleProgressBar.Value = val1000;
        }

        private void ConsoleWrite(string text) {
            MyConsole.AppendText(text);
            MyConsole.ScrollToCaret();
            //if (MyConsole.InvokeRequired) {
            //    MyConsole.Invoke(new SafeCallDelegate(ConsoleWrite), new object[] { text });
            //} else {
            //    MyConsole.AppendText(text);
            //    MyConsole.ScrollToCaret();
            //}
        }

        private void ButtonRerun_Click(object sender, EventArgs e) {
            Run(true);
        }

        private void ButtonClear_Click(object sender, EventArgs e) {
            ClearCharts();
            ClearStats();
        }

        private void ButtonReset_Click(object sender, EventArgs e) {
            foreach (KeyValuePair<string, Panel> controlWrapper in generators[GeneratorChoose.SelectedIndex].controls) {
                NumericUpDown input = controlWrapper.Value.Tag as NumericUpDown;
                input.Value = (decimal)input.Tag;
            }
        }

        private void EnableControls(bool isEnabled) {
            ButtonRun.Enabled = isEnabled;
            ButtonClear.Enabled = isEnabled;
            ButtonRerun.Enabled = isEnabled;
            ButtonSave.Enabled = isEnabled;
            ButtonReset.Enabled = isEnabled;
            InputCount.Enabled = isEnabled;
            GeneratorChoose.Enabled = isEnabled;
            foreach (KeyValuePair<string, Panel> controlWrapper in generators[GeneratorChoose.SelectedIndex].controls) {
                NumericUpDown inputContainer = controlWrapper.Value.Tag as NumericUpDown;
                inputContainer.Enabled = isEnabled;
            }
        }

        private void GeneratorChoose_SelectedIndexChanged(object sender, EventArgs e) {
            int currentIndex = (sender as ComboBox).SelectedIndex;
            InputContainer.Controls.Clear();
            foreach (KeyValuePair<string, Panel> controlWrapper in generators[currentIndex].controls) {
                InputContainer.Controls.Add(controlWrapper.Value);
            }
        }

        private void ShowFunctionCheckBox_CheckedChanged(object sender, EventArgs e) {
            int seriesCount = DistributionChart.Series.Count / 2;
            for (int i = 0; i < seriesCount; i++) {
                string seriesName = "perfect_" + i;
                DistributionChart.Series[seriesName].Enabled = ((CheckBox)sender).Checked;
            }
        }

        private void Test_AI() {
            ulong randCount = (ulong)InputCount.Value;
            double[] fitResult;
            new Task(() => {
                fitResult = Fitter.Fit(
                    new string[] { "a", "b", "c" },
                    new double[] { 0, -100, -100 },
                    new double[] { 100, 0, 100 },
                    (double[] parameters) => {
                        double loss = 0;
                        for (int controlI = 0; controlI < 4; controlI++) {
                            DynamicNCLTGenerator generator = new DynamicNCLTGenerator();
                            generator.SetParameters(parameters);
                            generator.Prepare();
                            Dictionary<int, ulong> data = generator.ISequence(randCount);
                            double m = 0, d = 0;
                            foreach (KeyValuePair<int, ulong> i in data) {
                                m += (double)i.Key * i.Value;
                            }
                            m /= randCount;
                            foreach (KeyValuePair<int, ulong> i in data) {
                                double underSqr = i.Key - m;
                                d += underSqr * underSqr * i.Value;
                            }
                            d /= randCount;
                            double si = Math.Sqrt(d);
                            loss += Math.Abs(si - generator.GetCalcedN());
                        }
                        return loss;
                    },
                    (string text) => {
                        MyConsole.Invoke((Action)(() => ConsoleWrite(text)));
                    },
                    (double val1) => {
                        MyConsoleProgressBar.Invoke((Action)(() => ConsoleSetProgress(Math.Min((int)(val1 * 1000D), 1000))));
                    },
                    (Dictionary<string, double> leaderParams) => {
                        generatorParameters.Invoke((Action)(() => {
                            foreach (KeyValuePair<string, double> leaderParam in leaderParams) {
                                ((NumericUpDown)generators[1].controls[leaderParam.Key].Tag).Value = (decimal)leaderParam.Value;
                            }
                        }));
                    });
            }).Start();
        }

        private void Test_2text() {

        }

        private void ButtonTest_Click(object sender, EventArgs e) {
            Test_2text();
        }
    }
}
