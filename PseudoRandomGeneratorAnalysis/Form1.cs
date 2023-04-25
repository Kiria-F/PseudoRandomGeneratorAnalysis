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

namespace PseudoRandomGeneratorAnalysis {

    public partial class Form1 : Form {
        private Pen pen = new Pen(Color.FromArgb(100, 0, 0, 0), 1);
        private delegate void SafeCallDelegate(string text);
        private delegate void UniversalSafeCallDelegate(Control obj, Action act);
        private int lastProgressVal = -1;

        private Generator[] generators;

        public Form1() {
            InitializeComponent();
            //BaseChart.ChartAreas["Histogram"].AxisX.Minimum = 0;
            //BaseChart.ChartAreas["Histogram"].AxisX.Maximum = 100;
            //BaseChart.ChartAreas["Graphic"].AxisX.Minimum = 0;
            //BaseChart.ChartAreas["Graphic"].AxisX.Maximum = 100;

            generators = new Generator[] {
                new SlowNormalGenerator(),
                new FastNormalGenerator(),
                new CustomNormalGenerator(),
                new CustomSinGenerator(),
                new CustomLogGenerator(),
                new CustomSqrGenerator(),
                new Custom2PowerGenerator(),
                new VCustomSinGenerator()
            };

            GeneratorChoose.Items.AddRange(new string[] {
                "Нормальное распр.",
                "Нормальное распр. уск.",
                "Каст. нормальное распр.",
                "Синусоидальное распр.",
                "Логарифмическое распр.",
                "Квадратичное распр.",
                "Показательное распр.",
                "Новый синус"
            });

            GeneratorChoose.SelectedIndex = 0;
        }

        private void SafeInvoke(Control obj, Action act) {
            if (obj.InvokeRequired) {
                obj.Invoke(new UniversalSafeCallDelegate(SafeInvoke), new object[] { obj, act });
            } else {
                act();
            }
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

        private void AddDistributionDataToChart(Dictionary<double, ulong> data, ulong randCount) {
            System.Windows.Forms.DataVisualization.Charting.Series newSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            newSeries.ChartArea = "DistributionArea";
            //newSeries.LabelForeColor = System.Drawing.Color.BlanchedAlmond;
            newSeries.Name = "distribution_" + DistributionChart.Series.Count;
            newSeries.YValuesPerPoint = 2;
            foreach (KeyValuePair<double, ulong> i in data) {
                newSeries.Points.AddXY(i.Key, (double)i.Value / randCount);
            }
            DistributionChart.Series.Add(newSeries);
        }

        private void AddIntegralDataToChart(Dictionary<double, ulong> data, ulong randCount) {
            System.Windows.Forms.DataVisualization.Charting.Series newSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            newSeries.BackSecondaryColor = System.Drawing.Color.White;
            newSeries.BorderColor = System.Drawing.Color.White;
            newSeries.BorderWidth = 2;
            newSeries.ChartArea = "IntegralArea";
            newSeries.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.StepLine;
            //newSeries.Color = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(64)))), ((int)(((byte)(0)))));
            newSeries.Name = "integral_" + IntegralChart.Series.Count;
            ulong graphIncr = 0;
            foreach (KeyValuePair<double, ulong> i in data.OrderBy(i => i.Key)) {
                graphIncr += i.Value;
                newSeries.Points.AddXY(i.Key, (double)graphIncr / randCount);
            }
            IntegralChart.Series.Add(newSeries);
        }

        private void AddQualityDataToChart(Dictionary<double, ulong> data, ulong randCount, Generator generator) {
            System.Windows.Forms.DataVisualization.Charting.Series newSeries = new System.Windows.Forms.DataVisualization.Charting.Series();
            QualityChart.ChartAreas.First().AxisY.Maximum = DistributionChart.ChartAreas.First().AxisY.Maximum;
            newSeries.ChartArea = "QualityArea";
            //newSeries.LabelForeColor = System.Drawing.Color.BlanchedAlmond;
            newSeries.Name = "quality_" + QualityChart.Series.Count;
            newSeries.YValuesPerPoint = 2;

            double maxPerfectValue = 0;
            ulong maxValue = 0;
            Dictionary<double, KVPair<ulong, double>> comparasions = new Dictionary<double, KVPair<ulong, double>>();
            foreach (KeyValuePair<double, ulong> i in data) {
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
                newSeries.Points.AddXY(i.Key, difference);
            }
            QualityChart.Series.Add(newSeries);
        }
        
        private void CalcStats(Dictionary<double, ulong> data, ulong randCount) {
            double m = 0, d = 0;
            foreach (KeyValuePair<double, ulong> i in data) {
                m += (double)i.Key * i.Value;
            }
            m /= randCount;
            foreach (KeyValuePair<double, ulong> i in data) {
                double underSqr = (double)i.Key - m;
                d += underSqr * underSqr * i.Value;
            }
            d /= randCount;
            double si = Math.Sqrt(d), si3 = si * 3, leftBorder = m - si3, rightBorder = m + si3;
            double outOf3Si = 0;
            foreach (KeyValuePair<double, ulong> i in data) {
                if (i.Key <= leftBorder || i.Key >= rightBorder) {
                    outOf3Si += i.Value;
                }
            }
            outOf3Si = (1f - outOf3Si / randCount) * 100;
            LabelM.Text = m.ToString();
            LabelD.Text = d.ToString();
            LabelSi.Text = si.ToString();
            LabelIn.Text = outOf3Si.ToString() + "%";
        }

        private void Run(bool rerun) {
            ulong randCount = (ulong)InputCount.Value;
            Generator generator = generators[GeneratorChoose.SelectedIndex];
            generator.CollectParameterValues();
            Dictionary<double, ulong> data = null;
            int seconds1 = 0;
            int millis1 = 0;
            int seconds2 = 0;
            int millis2 = 0;

            Task firstTask = new Task(() => {
                SafeInvoke(ControlPanel, () => {
                    EnableControls(false);
                });
            });
            firstTask.ContinueWith((task) => {
                seconds1 = DateTime.Now.Second;
                millis1 = DateTime.Now.Millisecond;
            }).ContinueWith((task) => {
                data = generator.Sequence(randCount);
            }).ContinueWith((task) => {
                seconds2 = DateTime.Now.Second;
                millis2 = DateTime.Now.Millisecond;
                if (seconds2 < seconds1) {
                    seconds2 += 60;
                }
                SafeInvoke(LabelTime, () => {
                    LabelTime.Text = (((double)(seconds2 * 1000 + millis2 - seconds1 * 1000 - millis1)) / 1000).ToString() + " s";
                });
            }).ContinueWith((task) => {
                SafeInvoke(ChartsSplitContainer, () => {
                    if (rerun) {
                        ClearCharts();
                    }
                });
            }).ContinueWith((task) => {
                SafeInvoke(DistributionChart, () => {
                    AddDistributionDataToChart(data, randCount);
                });
            }).ContinueWith((task) => {
                SafeInvoke(IntegralChart, () => {
                    AddIntegralDataToChart(data, randCount);
                });
            }).ContinueWith((task) => {
                SafeInvoke(QualityChart, () => {
                    AddQualityDataToChart(data, randCount, generator);
                });
            }).ContinueWith((task) => {
                SafeInvoke(StaticInfo, () => {
                    CalcStats(data, randCount);
                });
            }).ContinueWith((task) => {
                SafeInvoke(ControlPanel, () => {
                    EnableControls(true);
                });
            });
            firstTask.Start();
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
            if (MyConsole.InvokeRequired) {
                MyConsole.Invoke(new SafeCallDelegate(ConsoleWrite), new object[] { text });
            } else {
                MyConsole.AppendText(text);
                MyConsole.ScrollToCaret();
            }
        }

        private void ButtonRerun_Click(object sender, EventArgs e) {
            Run(true);
        }

        private void ButtonClear_Click(object sender, EventArgs e) {
            ClearCharts();
        }

        private void ButtonReset_Click(object sender, EventArgs e) {
            for (int i = 0; i < generators[GeneratorChoose.SelectedIndex].parameterInputs.Length; i++) {
                generators[GeneratorChoose.SelectedIndex].parameterInputs[i].Value = generators[GeneratorChoose.SelectedIndex].defaultValues[i];
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
            for (int i = 0; i < generators[GeneratorChoose.SelectedIndex].parameterInputs.Length; i++) {
                generators[GeneratorChoose.SelectedIndex].parameterInputs[i].Enabled = isEnabled;
            }
        }

        private void GeneratorChoose_SelectedIndexChanged(object sender, EventArgs e) {
            ComboBox currentSender = (ComboBox)sender;
            int currentIndex = currentSender.SelectedIndex;
            InputContainer.Controls.Clear();
            Panel[] inputsContainer = generators[currentIndex].GenerateCompleteInputs();
            for (int i = inputsContainer.Length - 1; i >= 0; i--) {
                InputContainer.Controls.Add(inputsContainer[i]);
            }
        }
    }

    abstract class Generator {
        protected Random random = new Random();
        public string[] parameterNames;
        public string[] parameterlabels;
        public decimal[] defaultValues;
        public NumericUpDown[] parameterInputs;
        protected Dictionary<string, decimal> parameters = new Dictionary<string, decimal>();

        protected NumericUpDown ConstructInput(string name, decimal defaultValue, int quality, decimal? min = null, decimal? max = null) {
            if (!min.HasValue) min = decimal.MinValue;
            if (!max.HasValue) max = decimal.MaxValue;

            NumericUpDown input = new NumericUpDown();
            input.Name = "input_" + name;
            input.DecimalPlaces = quality;
            input.AutoSize = true;
            input.Dock = System.Windows.Forms.DockStyle.Fill;
            input.Minimum = min.Value;
            input.Maximum = max.Value;
            input.Value = defaultValue;
            return input;
        }

        public void CollectParameterValues() {
            parameters.Clear();
            for (int i = 0; i < parameterInputs.Count(); i++) {
                parameters.Add(parameterNames[i], parameterInputs[i].Value);
            }
        }

        public Panel[] GenerateCompleteInputs() {
            Panel[] container = new Panel[parameterInputs.Count()];
            for (int i = 0; i < container.Length; i++) {
                container[i] = new Panel();
                container[i].AutoSize = true;
                container[i].AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
                container[i].Dock = System.Windows.Forms.DockStyle.Top;
                container[i].Name = "panelInput_" + parameterNames[i];
                container[i].Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);

                Label inputLabel = new Label();
                inputLabel.Name = "InputLabel_" + parameterNames[i];
                inputLabel.Text = parameterlabels[i] + " ";
                inputLabel.Dock = System.Windows.Forms.DockStyle.Top;
                inputLabel.Size = new System.Drawing.Size(25, 22);

                container[i].Controls.Add(parameterInputs[i]);
                container[i].Controls.Add(inputLabel);
            }
            return container;
        }

        public virtual Dictionary<int, ulong> ISequence(ulong randCount) {
            return null;
        }

        public virtual Dictionary<double, ulong> Sequence(ulong randCount) {
            Dictionary<int, ulong> iSequence = ISequence(randCount);
            Dictionary<double, ulong> dSequence = new Dictionary<double, ulong>();
            foreach(KeyValuePair<int, ulong> kvp in iSequence) {
                dSequence.Add(kvp.Key, kvp.Value);
            }
            return dSequence;
        }

        protected double Gauss(double x, double si, double m) {
            double a = 1d / si / Math.Sqrt(2 * Math.PI);
            double z = -1d / 2 / si / si;
            x -= m;
            return a * Math.Pow(Math.E, x * x * z);
        }

        public abstract double CoreFunction(double x);
    }

    abstract class NormalGenerator : Generator {
        public override double CoreFunction(double x) {
            return Gauss(x, (double)parameters["si"], (double)parameters["m"]);
        }
    }

    class SlowNormalGenerator : NormalGenerator {

        public SlowNormalGenerator() {
            parameterNames = new string[] {
                "si", // ср. кв. отклонение
                "m", // мат. ожидание
                "n" // кол-во использемых равномерных последовательностей
            };
            parameterlabels = new string[] {
                "σ", // ср. кв. отклонение
                "m", // мат. ожидание
                "n" // кол-во использемых равномерных последовательностей
            };
            defaultValues = new decimal[] {
                10M, // si
                50M, // m
                12M // n
            };
            parameterInputs = new NumericUpDown[parameterNames.Count()];
            decimal?[] minVals = { 0M, null, 0M };
            int[] parameterDecPlaces = new int[] { 3, 3, 3 };
            for (int i = 0; i < parameterNames.Length; i++) {
                parameterInputs[i] = ConstructInput(parameterNames[i], defaultValues[i], parameterDecPlaces[i], minVals[i], null);
            }
        }

        private double NormalNext(double parameter_n) {
            double sum = 0;
            for (int i = 0; i < (int)parameter_n; i++) {
                sum += random.NextDouble();
            }
            double rest = (parameter_n - (int)(parameter_n)) * random.NextDouble();
            sum += rest;
            return sum / parameter_n;
        }

        private double CalcPreSi(Dictionary<double, ulong> preData) {
            double pre_m = 0, pre_d = 0;
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                pre_m += kvp.Key * kvp.Value;
            }
            pre_m /= preData.Count;
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                double underSqr = kvp.Key - pre_m;
                pre_d += underSqr * underSqr * kvp.Value;
            }
            pre_d /= preData.Count;
            return Math.Sqrt(pre_d);
        }

        private Dictionary<double, ulong> NormalSequence(ulong randCount, double parameter_n) {
            Dictionary<double, ulong> data = new Dictionary<double, ulong>();
            for (ulong i = 0; i < randCount; i++) {
                double x = NormalNext(parameter_n);
                if (data.ContainsKey(x)) {
                    data[x]++;
                } else {
                    data.Add(x, 1);
                }
            }
            return data;
        }

        private int Modificate(double x, double pre_si, double parameter_si, double parameter_m) {
            double t = (x - 0.5f) * parameter_si / pre_si + parameter_m;
            if (t < 0) t -= 1;
            return (int)t;
        }

        private Dictionary<int, ulong> ModificateSequence(Dictionary<double, ulong> preData, double preSi, double parameter_si, double parameter_m) {
            Dictionary<int, ulong> data = new Dictionary<int, ulong>();
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                int t = Modificate(kvp.Key, preSi, parameter_si, parameter_m); // 1 -> preSi
                if (data.ContainsKey(t)) {
                    data[t] += kvp.Value;
                } else {
                    data.Add(t, kvp.Value);
                }
            }
            return data;
        }

        public override Dictionary<int, ulong> ISequence(ulong randCount) {
            double parameter_n = (double)parameters["n"];
            double parameter_si = (double)parameters["si"];
            double parameter_m = (double)parameters["m"];

            Dictionary<double, ulong> preData = NormalSequence(randCount, parameter_n);
            double preSi = CalcPreSi(preData);
            Dictionary<int, ulong> data = ModificateSequence(preData, preSi, parameter_si, parameter_m);
            return data;
        }
    }

    class FastNormalGenerator : NormalGenerator {

        public FastNormalGenerator() {
            parameterNames = new string[] {
                "si", // ср. кв. отклонение
                "m", // мат. ожидание
                "n", // кол-во использемых равномерных последовательностей
                "a", // вспом. параметр a
                "b" // вспом. параметр b
            };
            parameterlabels = new string[] {
                "σ", // ср. кв. отклонение
                "m", // мат. ожидание
                "n", // кол-во использемых равномерных последовательностей
                "a", // вспом. параметр a
                "b" // вспом. параметр b
            };
            defaultValues = new decimal[] {
                10M, // si
                50M, // m
                12M, // n
                0.288077825M, // a
                -0.4988888129M // b
            };
            parameterInputs = new NumericUpDown[parameterNames.Count()];
            decimal?[] minVals = { 0M, null, 0M, null, null };
            int[] parameterDecPlaces = new int[] { 3, 3, 3, 9, 9 };
            for (int i = 0; i < parameterNames.Length; i++) {
                parameterInputs[i] = ConstructInput(parameterNames[i], defaultValues[i], parameterDecPlaces[i], minVals[i], null);
            }
        }

        private int? Modificate(double x, double preSi, double si, double m) {
            double t = (x - 0.5f) * si / preSi + m;
            if (t < 0) t -= 1;
            if (t < int.MinValue) return null;
            if (t > int.MaxValue) return null;
            return (int)t;
        }

        private double NormalNext(double n) {
            double sum = 0;
            for (int i = 0; i < (int)n; i++) {
                sum += random.NextDouble();
            }
            double rest = (n - (int)n) * random.NextDouble();
            sum += rest;
            return sum / n;
        }

        public override Dictionary<int, ulong> ISequence(ulong randCount) {
            double parameter_n = (double)parameters["n"];
            double parameter_a = (double)parameters["a"];
            double parameter_b = (double)parameters["b"];
            double parameter_si = (double)parameters["si"];
            double parameter_m = (double)parameters["m"];

            Dictionary<int, ulong> data = new Dictionary<int, ulong>();

            double preSi = parameter_a * Math.Pow(parameter_n, parameter_b);

            for (ulong i = 0; i < randCount; i++) {
                int? x = Modificate(NormalNext(parameter_n), preSi, parameter_si, parameter_m);
                if (x.HasValue) {
                    if (data.ContainsKey(x.Value)) {
                        data[x.Value]++;
                    } else {
                        data.Add(x.Value, 1);
                    }
                }
            }
            return data;
        }
    }

    class CustomNormalGenerator : NormalGenerator
    {
        public CustomNormalGenerator()
        {
            parameterNames = new string[] {
                "left", // левостороннее крайнее значение
                "right", // правостороннее крайнее значение
                "si", // ср. кв. отклонение
                "m", // мат. ожидание
            };
            parameterlabels = new string[] {
                "[", // левостороннее крайнее значение
                "]", // правостороннее крайнее значение
                "σ", // ср. кв. отклонение
                "m", // мат. ожидание
            };
            defaultValues = new decimal[] {
                0M, // left
                100M, // right
                10M, // si
                50M, // m
            };
            parameterInputs = new NumericUpDown[parameterNames.Length];
            decimal?[] minVals = { null, null, 0M, null };
            int[] parameterDecPlaces = new int[] { 3, 3, 3, 3 };
            for (int i = 0; i < parameterNames.Length; i++)
            {
                parameterInputs[i] = ConstructInput(parameterNames[i], defaultValues[i], parameterDecPlaces[i], minVals[i], null);
            }
        }
        
        private double[] ModelFunction(int left, int right, double si, double m)
        {
            Func<double, double> coreFunction;
            {
                double a = 1d / si / Math.Sqrt(2 * Math.PI);
                double b = m;
                double c = si;
                double z = -1d / 2 / c / c;
                coreFunction = (x) => { x -= b; return a * Math.Pow(Math.E, x * x * z); };
            }
            double[] model = new double[right - left];
            double yLast = coreFunction(left);
            model[0] = yLast;
            for (int i = 1; i < right - left; i++)
            {
                double x = (double)i + left;
                double y = coreFunction(x);
                yLast += y;
                model[i] = yLast;
            }
            double decreaser = model[0];
            yLast -= decreaser;
            for (int i = 0; i < model.Length; i++)
            {
                model[i] = (model[i] - decreaser) / yLast;
            }
            return model;
        }

        public override Dictionary<int, ulong> ISequence(ulong randCount)
        {
            int parameter_left = (int)((double)parameters["left"] + 0.5);
            int parameter_right = (int)((double)parameters["right"] + 0.5) + 2;
            double parameter_si = (double)parameters["si"];
            double parameter_m = (double)parameters["m"];
            double[] model = ModelFunction(parameter_left, parameter_right, parameter_si, parameter_m);
            Dictionary<int, ulong> sequence = new Dictionary<int, ulong>();
            int il, ir, im;
            for (ulong iteration = 0; iteration < randCount; iteration++)
            {
                double rand = random.NextDouble();
                il = 0;
                ir = model.Length - 1;
                im = (il + ir) / 2;
                while (ir - il > 1)
                {
                    if (rand >= model[im])
                    {
                        il = im;
                    }
                    else
                    {
                        ir = im;
                    }
                    im = (il + ir) / 2;
                }
                int key = il + parameter_left;
                if (sequence.ContainsKey(key))
                {
                    sequence[key]++;
                }
                else
                {
                    sequence.Add(key, 1);
                }
            }
            return sequence;
        }
    }

    abstract class CustomGenerator : Generator {

        protected KVPair<double, double>[] ModelFunction(double left, double right, int detalization) {
            double step = (right - left) / (detalization - 1);

            KVPair<double, double>[] model = new KVPair<double, double>[detalization];
            double yLast = 0;
            double x = left;
            for (int i = 0; i < detalization; i++, x = left + step * i) {
                double y = CoreFunction(x);
                if (y < 0) {
                    y = 0;
                }
                yLast += y;
                model[i] = new KVPair<double, double>(x, yLast);
            }
            double decrease = model[0].Value;
            double compression = yLast - decrease;
            for (int i = 0; i < model.Length; i++) {
                model[i].Value = (model[i].Value - decrease) / compression;
            }
            return model;
        }
    }

    abstract class RasterCustomGenerator : CustomGenerator {

        public RasterCustomGenerator() {
            parameterNames = new string[] {
                "left",
                "right",
                "detalization"
            };
            parameterlabels = new string[] {
                "Левая  гр.",
                "Правая гр.",
                "Детализация"
            };
            defaultValues = new decimal[] {
                0M,
                10M,
                100M
            };
            parameterInputs = new NumericUpDown[parameterNames.Length];
            int[] parameterDecPlaces = new int[] { 3, 3, 0 };
            for (int i = 0; i < parameterNames.Length; i++) {
                parameterInputs[i] = ConstructInput(parameterNames[i], defaultValues[i], parameterDecPlaces[i], null, null);
            }
        }

        public override Dictionary<double, ulong> Sequence(ulong randCount) {
            KVPair<double, double>[] model = ModelFunction((double)parameters["left"], (double)parameters["right"], (int)parameters["detalization"]);
            Dictionary<double, ulong> sequence = new Dictionary<double, ulong>();
            int il, ir, im;
            for (ulong iteration = 0; iteration < randCount; iteration++) {
                double rand = random.NextDouble();
                il = 0;
                ir = model.Length - 1;
                im = (il + ir) / 2;
                while (ir - il > 1) {
                    if (rand >= model[im].Value) {
                        il = im;
                    } else {
                        ir = im;
                    }
                    im = (il + ir) / 2;
                }
                double key = model[il].Key;
                if (sequence.ContainsKey(key)) {
                    sequence[key]++;
                } else {
                    sequence.Add(key, 1);
                }
            }
            return sequence;
        }
    }

    class CustomSinGenerator : RasterCustomGenerator {

        public override double CoreFunction(double x) {
            return Math.Sin(x) + 1;
        }
    }

    class CustomLogGenerator : RasterCustomGenerator {

        public override double CoreFunction(double x) {
            return Math.Log(x + 1);
        }
    }

    class CustomSqrGenerator : RasterCustomGenerator {

        public override double CoreFunction(double x) {
            return x * x;
        }
    }

    class Custom2PowerGenerator : RasterCustomGenerator {

        public override double CoreFunction(double x) {
            return Math.Pow(Math.E, x);
        }
    }

    abstract class VectorCustomGenerator : CustomGenerator {

        public VectorCustomGenerator() {
            parameterNames = new string[] {
                "left",
                "right",
                "quality",
                "detalization"
            };
            parameterlabels = new string[] {
                "Левая  гр.",
                "Правая гр.",
                "Качество",
                "Детализация"
            };
            defaultValues = new decimal[] {
                0M,
                10M,
                100M,
                100M,
            };
            parameterInputs = new NumericUpDown[parameterNames.Length];
            int[] parameterDecPlaces = new int[] { 3, 3, 0, 0 };
            for (int i = 0; i < parameterNames.Length; i++) {
                parameterInputs[i] = ConstructInput(parameterNames[i], defaultValues[i], parameterDecPlaces[i], null, null);
            }
        }

        public override Dictionary<double, ulong> Sequence(ulong randCount) {
            int detalization = (int)parameters["detalization"];
            KVPair<double, double>[] model = ModelFunction((double)parameters["left"], (double)parameters["right"], (int)parameters["quality"]);
            Dictionary<double, ulong> sequence = new Dictionary<double, ulong>();
            double left = (double)parameters["left"];
            double right = (double)parameters["right"];
            double stretch = detalization / (right - left);
            double key_step = model[1].Key - model[0].Key;
            int il, ir, im;
            for (ulong iteration = 0; iteration < randCount; iteration++) {
                double rand = random.NextDouble();
                il = 0;
                ir = model.Length - 1;
                im = (il + ir) / 2;
                while (ir - il > 1) {
                    if (rand >= model[im].Value) {
                        il = im;
                    } else {
                        ir = im;
                    }
                    im = (il + ir) / 2;
                }
                double key = (rand - model[il].Value) / (model[ir].Value - model[il].Value) * key_step + model[il].Key;
                double simplified_key = Math.Floor((key - left) * stretch) / stretch + left;
                if (sequence.ContainsKey(simplified_key)) {
                    sequence[simplified_key]++;
                } else {
                    sequence.Add(simplified_key, 1);
                }
            }
            return sequence;
        }
    }

    class VCustomSinGenerator : VectorCustomGenerator {

        public override double CoreFunction(double x) {
            return Math.Sin(x) + 1;
        }
    }
}
