using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace PseudoRandomGeneratorAnalysis {

    public partial class GUIControl : Form {
        private Pen pen = new Pen(Color.FromArgb(100, 0, 0, 0), 1);
        private int lastProgressVal = -1;

        private Generator[] generators = new Generator[] {
            new GrandCLTGenerator(),
            new StaticCLTGenerator(),
            new DynamicCLTGenerator(),
            new BasicGenerator()
        };

        public GUIControl() {
            InitializeComponent();
            // DistributionChart.ChartAreas["DistributionArea"].AxisX.Minimum = 0;

            foreach (Generator generator in generators) {
                GeneratorChoose.Items.Add(generator.name);
            }
            GeneratorChoose.SelectedIndex = 0;
            Generator.SetLogsOutput((string text) => { Invoke((Action)(() => { ConsoleWrite(text); })); });
            Generator.SetProgressOutput((double progress) => { Invoke((Action)(() => { ConsoleSetProgress(progress); })); });
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
            perfectSeries.Enabled = ShowFunctionCheckBox.Checked;

            double maxPerfectValue = 0;
            ulong maxValue = 0;
            Dictionary<double, KVPair<ulong, double>> comparasions = new Dictionary<double, KVPair<ulong, double>>();
            foreach (KeyValuePair<int, ulong> dataX in data) {
                double perfectValue = generator.CoreFunction(dataX.Key);
                comparasions.Add(dataX.Key, new KVPair<ulong, double>(dataX.Value, perfectValue));
                if (perfectValue > maxPerfectValue) {
                    maxPerfectValue = perfectValue;
                }
                if (dataX.Value > maxValue) {
                    maxValue = dataX.Value;
                }
            }
            foreach (KeyValuePair<double, KVPair<ulong, double>> i in comparasions) {
                double difference = (double)i.Value.Key / randCount - i.Value.Value;
                differenceSeries.Points.AddXY(i.Key, difference);
                perfectSeries.Points.AddXY(i.Key, i.Value.Value);
            }
            int xMin = data.Keys.Min();
            int xMax = data.Keys.Max();
            for (int x = xMin; x <= xMax; x++) {
                if (!data.Keys.Contains(x)) {
                    perfectSeries.Points.AddXY(x, generator.CoreFunction(x));
                }
            }
            QualityChart.Series.Add(differenceSeries);
            perfectSeries.Sort(System.Windows.Forms.DataVisualization.Charting.PointSortOrder.Ascending, "X");
            DistributionChart.Series.Add(perfectSeries);
        }

        private void CorrectChartsZoom(Dictionary<int, ulong> data, double parameter_m) {
            double dmin = data.Keys.Min();
            double dmax = data.Keys.Max();
            double amplitude = Math.Max(Math.Abs(parameter_m - dmin), Math.Abs(parameter_m - dmax));
            amplitude = ((int)amplitude / 10 + 1) * 10;
            int min = (int)(parameter_m - amplitude + 0.5);
            int max = (int)(parameter_m + amplitude + 0.5);
            DistributionChart.Update();
            DistributionChart.ChartAreas["DistributionArea"].AxisX.Minimum = min;
            DistributionChart.ChartAreas["DistributionArea"].AxisX.Maximum = max;
            IntegralChart.ChartAreas["IntegralArea"].AxisX.Minimum = min;
            IntegralChart.ChartAreas["IntegralArea"].AxisX.Maximum = max;
            QualityChart.ChartAreas["QualityArea"].AxisX.Minimum = min;
            QualityChart.ChartAreas["QualityArea"].AxisX.Maximum = max;
            CorrectQualityChartZoom();
        }

        private void CorrectQualityChartZoom() {
            if (NaturalProportionsCheckBox.Checked) {
                double amplitude = DistributionChart.ChartAreas["DistributionArea"].AxisY.Maximum / 2;
                QualityChart.ChartAreas["QualityArea"].AxisY.Maximum = amplitude;
                QualityChart.ChartAreas["QualityArea"].AxisY.Minimum = -amplitude;
            } else {
                QualityChart.ChartAreas["QualityArea"].AxisY.Maximum = double.NaN;
                QualityChart.ChartAreas["QualityArea"].AxisY.Minimum = double.NaN;
                //QualityChart.Update();
                //double top = QualityChart.ChartAreas["QualityArea"].AxisY.Maximum;
                //double bottom = QualityChart.ChartAreas["QualityArea"].AxisY.Minimum;
                //double shift = (top + bottom) / 2;
                //QualityChart.ChartAreas["QualityArea"].AxisY.Maximum -= shift;
                //QualityChart.ChartAreas["QualityArea"].AxisY.Minimum -= shift;
            }
        }

        private void CalcStats(Dictionary<int, ulong> data, ulong randCount, out double m, out double si) {
            double d = 0;
            m = 0;
            foreach (KeyValuePair<int, ulong> i in data) {
                m += (double)i.Key * i.Value;
            }
            m /= randCount;
            foreach (KeyValuePair<int, ulong> i in data) {
                double underSqr = (double)i.Key - m;
                d += underSqr * underSqr * i.Value;
            }
            d /= randCount;
            si = Math.Sqrt(d);
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
            EnableControls(false);
            Generator generator = generators[GeneratorChoose.SelectedIndex];
            generator.CollectParameterValues();
            new Task(() => {
                ulong randCount = (ulong)InputCount.Value;
                generator.Prepare();
                int millis1 = DateTime.Now.Millisecond;
                int seconds1 = DateTime.Now.Second;
                Dictionary<int, ulong> data = generator.Sequence(randCount);
                int millis2 = DateTime.Now.Millisecond;
                int seconds2 = DateTime.Now.Second;
                if (seconds2 < seconds1) {
                    seconds2 += 60;
                }
                Invoke((Action)(() => {
                    LabelTime.Text = (((double)(seconds2 * 1000 + millis2 - seconds1 * 1000 - millis1)) / 1000).ToString() + " s";
                    if (rerun) {
                        ClearCharts();
                    }
                    AddDistributionDataToChart(data, randCount);
                    AddIntegralDataToChart(data, randCount);
                    AddQualityDataToChart(data, randCount, generator);
                    CalcStats(data, randCount, out double m, out double si);
                    CorrectChartsZoom(data, m);
                    EnableControls(true);
                }));
            }).Start();
        }

        private void ButtonRun_Click(object sender, EventArgs e) {
            Run(false);
        }

        private void ConsoleSetProgress(double progress) {
            int intProgress = (int) (progress * 1000);
            if (intProgress == lastProgressVal) {
                return;
            }
            if (intProgress > 1000) {
                intProgress = 1000;
            }
            MyConsoleProgressBar.Value = intProgress;
            lastProgressVal = intProgress;
        }

        private void ConsoleWrite(string text) {
            MyConsole.AppendText(text);
            MyConsole.ScrollToCaret();
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
            foreach (Panel controlWrapper in generators[currentIndex].controls.Values.Reverse()) {
                InputContainer.Controls.Add(controlWrapper);
            }
        }

        private void ShowFunctionCheckBox_CheckedChanged(object sender, EventArgs e) {
            int seriesCount = DistributionChart.Series.Count / 2;
            for (int i = 0; i < seriesCount; i++) {
                string seriesName = "perfect_" + i;
                DistributionChart.Series[seriesName].Enabled = (sender as CheckBox).Checked;
            }
        }

        private void NaturalProportionsCheckBox_CheckedChanged(object sender, EventArgs e) {
            CorrectQualityChartZoom();
        }

        private void Test_AI() {
            EnableControls(false);
            ulong randCount = (ulong)InputCount.Value;
            double[] fitResult;
            new Task(() => {
                fitResult = Fitter.FitGrad(
                    new string[] { "a", "b" },
                    new double[] { 1.8456679511, -0.36761 },
                    3, 20,
                    (double[] parameters) => {
                        double loss = 0;
                        //Parallel.For(2, 5, (int si) => {
                        double si = 3.5;
                            StaticCLTGenerator generator = new StaticCLTGenerator();
                            //generator.SetSi(si);
                            //generator.SetParameters(parameters);
                            generator.Prepare();
                            Dictionary<int, ulong> data = generator.Sequence(randCount);
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
                            double siExp = Math.Sqrt(d);
                            loss += Math.Abs(si - siExp);
                        //});
                        return loss;
                    },
                    (string text) => {
                        Invoke((Action)(() => ConsoleWrite(text)));
                    },
                    (double val1) => {
                        Invoke((Action)(() => ConsoleSetProgress(val1)));
                    },
                    (Dictionary<string, double> leaderParams) => {
                        Invoke((Action)(() => {
                            foreach (KeyValuePair<string, double> leaderParam in leaderParams) {
                                ((NumericUpDown)generators[1].controls[leaderParam.Key].Tag).Value = (decimal)leaderParam.Value;
                            }
                        }));
                    });
                Invoke((Action)(() => {
                    Console.WriteLine("\n\nRESULT: " + fitResult.ToString());
                    EnableControls(true);
                }));
            }).Start();
        }

        private void Test_2text() {
            BasicGenerator generator = generators.Last() as BasicGenerator;
            ulong N = (ulong)InputCount.Value;
            new Task(() => {
                Random random = new Random();
                TextWriter xs = new StreamWriter(new FileStream("f22xs.txt", FileMode.OpenOrCreate, FileAccess.Write));
                TextWriter ys = new StreamWriter(new FileStream("f22ys.txt", FileMode.OpenOrCreate, FileAccess.Write));
                TextWriter ps = new StreamWriter(new FileStream("f22ps.json", FileMode.OpenOrCreate, FileAccess.Write));
                var n_max = 5;
                for (double n = 1; n < n_max; n += 0.01) {
                    generator.SetN(n);
                    Dictionary<int, ulong> data = generator.Sequence(N);
                    double m = 0, d = 0;
                    foreach (KeyValuePair<int, ulong> i in data) {
                        m += (double)i.Key * i.Value;
                    }
                    m /= N;
                    foreach (KeyValuePair<int, ulong> i in data) {
                        double underSqr = i.Key - m;
                        d += underSqr * underSqr * i.Value;
                    }
                    d /= N;
                    double si = Math.Sqrt(d);
                    ps.Write("{ \"x\": " + (n).ToString().Replace(',', '.') + ", \"y\": " + (si).ToString().Replace(',', '.') + " }, ");
                    xs.WriteLine(n.ToString());
                    ys.WriteLine((si).ToString());
                    
                    Invoke((Action)(() => { ConsoleSetProgress(n / n_max); }));
                }
                xs.Close();
                ys.Close();
                ps.Close();
                Invoke((Action)(() => {
                    ConsoleWrite("DONE");
                    ConsoleSetProgress(0);
                }));
            }).Start();
        }

        private void ButtonTest_Click(object sender, EventArgs e) {
            Test_2text();
        }

        private void GUIControl_SizeChanged(object sender, EventArgs e) {
            ConsoleWrite(Size.ToString() + "\n");
        }

    }
}
