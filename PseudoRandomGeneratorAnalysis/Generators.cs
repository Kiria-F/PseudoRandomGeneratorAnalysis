using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;

namespace PseudoRandomGeneratorAnalysis {

    abstract class Generator {
        protected static Action<string> logsOutput;
        protected static Action<double> progressOutput;
        protected static readonly Random random = new Random();
        public string name;
        public readonly Dictionary<string, Panel> controls;
        protected double parameterM;
        protected double parameterSi;
        protected object[] debugMemoryWire;

        public Generator() {
            name = "Новый генератор";
            controls = new Dictionary<string, Panel>();
            AddNewControl("Мат. ожидание", "m", 50M, 3);
            AddNewControl("СКв отклонение", "si", 10M, 3, 0M);
        }

        public static void SetLogsOutput(Action<string> logsOutput) {
            Generator.logsOutput = logsOutput;
        }

        public static void SetProgressOutput(Action<double> progressOutput) {
            Generator.progressOutput = progressOutput;
        }

        protected NumericUpDown ConstructInput(string name, decimal defaultValue, int quality, decimal? min = null, decimal? max = null, decimal? increment = null) {
            if (!min.HasValue) min = decimal.MinValue;
            if (!max.HasValue) max = decimal.MaxValue;
            if (!increment.HasValue) increment = (decimal)(1D / Math.Pow(10, quality));

            NumericUpDown newInput = new NumericUpDown();
            newInput.Name = "input_" + name;
            newInput.DecimalPlaces = quality;
            newInput.Increment = increment.Value;
            newInput.AutoSize = true;
            newInput.Dock = System.Windows.Forms.DockStyle.Fill;
            newInput.Minimum = min.Value;
            newInput.Maximum = max.Value;
            newInput.Value = defaultValue;
            newInput.Tag = defaultValue;
            return newInput;
        }

        protected Panel ConstructControl(string label, NumericUpDown newInput) {
            string namePostfix = newInput.Name.Substring(newInput.Name.LastIndexOf("_"));
            Panel newPanel = new Panel();
            newPanel.AutoSize = true;
            newPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            newPanel.Dock = System.Windows.Forms.DockStyle.Top;
            newPanel.Name = "panelInput_" + namePostfix;
            newPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 6);

            Label inputLabel = new Label();
            inputLabel.Name = "InputLabel_" + namePostfix;
            inputLabel.Text = label + " ";
            inputLabel.Dock = System.Windows.Forms.DockStyle.Top;
            inputLabel.Size = new System.Drawing.Size(25, 22);

            newPanel.Controls.Add(newInput);
            newPanel.Tag = newInput;
            newPanel.Controls.Add(inputLabel);
            return newPanel;
        }

        protected void AddNewControl(string label, string name, decimal defaultValue, int quality, decimal? min = null, decimal? max = null, decimal? increment = null) {
            NumericUpDown newInput = ConstructInput(name, defaultValue, quality, min, max, increment);
            Panel newControl = ConstructControl(label, newInput);
            controls.Add(name, newControl);
        }

        public virtual void CollectParameterValues() {
            parameterM = (double)(controls["m"].Tag as NumericUpDown).Value;
            parameterSi = (double)(controls["si"].Tag as NumericUpDown).Value;
        }

        protected virtual double NormalNext(double n) {
            double sum = 0;
            ulong nInt = (ulong)n;
            for (ulong i = 0; i < n; i++) {
                sum += random.NextDouble();
            }
            double rest = random.NextDouble();
            if (rest < n - nInt) {
                sum += rest;
            }
            return sum;
        }

        public virtual Dictionary<int, ulong> Sequence(ulong randCount) {
            Dictionary<int, ulong> sequence = new Dictionary<int, ulong>();
            for (ulong i = 0; i < randCount; i++) {
                double gen = Next();
                if (gen < -0.5) {
                    gen -= 1;
                }
                int iGen = (int)(gen + 0.5D);
                sequence[iGen] = sequence.TryGetValue(iGen, out ulong count) ? count + 1 : 1;
            }
            debugMemoryWire = new object[] { sequence };
            return sequence;
        }

        public virtual Dictionary<int, ulong> Sequence(ulong randCount, int leftEdge, int rightEdge) {
            Dictionary<int, ulong> sequence = new Dictionary<int, ulong>();
            for (ulong i = 0; i < randCount; i++) {
                double gen = Next() * (rightEdge - leftEdge) + leftEdge;
                if (gen < -0.5) {
                    gen -= 1;
                }
                int iGen = (int)(gen + 0.5D);
                if (iGen < leftEdge) { } else if (iGen > rightEdge) { } else {
                    sequence[iGen] = sequence.TryGetValue(iGen, out ulong count) ? count + 1 : 1;
                }
            }
            return sequence;
        }

        public Func<double, double> CoreFunction;

        public Func<double, double> IntegralFunction;

        public void RecalcCoreFunction() {
            double a = 1d / parameterSi / Math.Sqrt(2 * Math.PI);
            double b = parameterM;
            double c = parameterSi;
            double z = -1d / 2 / c / c;
            CoreFunction = (x) => { x -= b; return a * Math.Pow(Math.E, x * x * z); };
        }

        public void RecalcIntegralFunction() {
            double a = Math.Sqrt(2) * parameterSi;
            IntegralFunction = (x) => LocalMath.Erf((x - parameterM) / a) / 2 + 0.5;
        }

        public virtual void Prepare() {
            RecalcCoreFunction();
            RecalcIntegralFunction();
        }

        public abstract double Next();

        public double GetParameterM() {
            return parameterM;
        }
    }

    class BasicGenerator : Generator {
        protected double parameterN;

        public BasicGenerator() {
            name = "Базовый";
            AddNewControl("N", "n", 50M, 0);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
        }

        public void SetN(double n) {
            parameterN = n;
        }

        public override double Next() {
            return NormalNext(parameterN);
        }
    }

    //abstract class CLTGenerator : Generator {
    //    protected double parameterN;

    //    public override void CollectParameterValues() {
    //        base.CollectParameterValues();
    //        parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
    //    }

    //    protected double NormalNext() {
    //        double sum = 0;
    //        for (int i = 0; i < (int)parameterN; i++) {
    //            sum += random.NextDouble();
    //        }
    //        double rest = (parameterN - (int)parameterN) * random.NextDouble();
    //        sum += rest;
    //        return sum / parameterN;
    //    }

    //    public CLTGenerator() : base() {
    //        AddNewControl("Исп. последовательности", "n", 10M, 3);
    //    }
    //}

    class PerfectCLTGenerator : Generator {
        protected double parameterN;
        protected double preSi;

        public PerfectCLTGenerator() : base() {
            name = "Идеальный";
            AddNewControl("Исп. посл-ей", "n", 10M, 3, 1, null, 1);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
        }

        private void CalcPreSi(Dictionary<double, ulong> preData) {
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
            preSi = Math.Sqrt(pre_d);
        }

        private Dictionary<double, ulong> NormalSequence(ulong randCount) {
            Dictionary<double, ulong> data = new Dictionary<double, ulong>();
            for (ulong i = 0; i < randCount; i++) {
                double x = NormalNext(parameterN);
                if (data.ContainsKey(x)) {
                    data[x]++;
                } else {
                    data.Add(x, 1);
                }
            }
            return data;
        }

        private int Modificate2I(double x) {
            double gen = (x - parameterN / 2) * parameterSi / preSi + parameterM;
            if (gen < -0.5) {
                gen -= 1;
            }
            return (int)(gen + 0.5D);
        }

        private Dictionary<int, ulong> ModificateSequence2I(Dictionary<double, ulong> preData) {
            Dictionary<int, ulong> data = new Dictionary<int, ulong>();
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                int iGen = Modificate2I(kvp.Key);
                data[iGen] = data.TryGetValue(iGen, out ulong count) ? count + kvp.Value : kvp.Value;
            }
            return data;
        }

        private Dictionary<int, ulong> ModificateSequence2I(Dictionary<double, ulong> preData, int leftEdge, int rightEdge) {
            Dictionary<int, ulong> data = new Dictionary<int, ulong>();
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                int iGen = Modificate2I(kvp.Key); // 1 -> preSi
                if (iGen < leftEdge) { } else if (iGen > rightEdge) { } else {
                    data[iGen] = data.TryGetValue(iGen, out ulong count) ? count + kvp.Value : kvp.Value;
                }
            }
            return data;
        }

        public override Dictionary<int, ulong> Sequence(ulong randCount) {
            Dictionary<double, ulong> preData = NormalSequence(randCount);
            CalcPreSi(preData);
            Dictionary<int, ulong> data = ModificateSequence2I(preData);
            return data;
        }

        public override Dictionary<int, ulong> Sequence(ulong randCount, int leftEdge, int rightEdge) {
            Dictionary<double, ulong> preData = NormalSequence(randCount);
            CalcPreSi(preData);
            Dictionary<int, ulong> data = ModificateSequence2I(preData, leftEdge, rightEdge);
            return data;
        }

        public override double Next() {
            throw new NotImplementedException();
        }
    }

    class StaticCLTGenerator : Generator {
        protected double parameterA;
        protected double parameterB;
        protected double calcedN;

        public StaticCLTGenerator() : base() {
            name = "Стат. одношаговый";
            AddNewControl("параметр a", "a", 0.3153M /*1.8456679511M*/ /*0.288077825M*/, 9);
            AddNewControl("параметр b", "b", 0.4798M /*-0.36761M*/ /*-0.4988888129M*/, 9);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterA = (double)(controls["a"].Tag as NumericUpDown).Value;
            parameterB = (double)(controls["b"].Tag as NumericUpDown).Value;
        }

        public double GetCalcedN() {
            return calcedN;
        }

        public override void Prepare() {
            base.Prepare();
            calcedN = Math.Pow(parameterSi / parameterA, 1 / parameterB);
            logsOutput("Calced N = " + calcedN.ToString() + '\n');
        }

        public override double Next() {
            return NormalNext(calcedN) - (calcedN / 2) + parameterM;
        }
    }

    class DynamicCLTGenerator : Generator {
        protected double parameterA;
        protected double parameterB;
        protected double parameterN;
        protected double modificator;

        public DynamicCLTGenerator() : base() {
            name = "Динам. одношаговый";
            AddNewControl("параметр a", "a", 0.28882M, 9);
            AddNewControl("параметр b", "b", 0.500000000M, 9);
            AddNewControl("Исп. последовательности", "n", 10M, 3, 1, null, 1);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterA = (double)(controls["a"].Tag as NumericUpDown).Value;
            parameterB = (double)(controls["b"].Tag as NumericUpDown).Value;
            parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
        }

        public override void Prepare() {
            base.Prepare();
            modificator = parameterSi / (parameterA * Math.Pow(parameterN, parameterB));
        }

        public override double Next() {
            return (NormalNext(parameterN) - parameterN / 2) * modificator + parameterM;
        }
    }

    class CrossPoint {
        public double X;
        public double YCore;
        public double YIntegral;

        public CrossPoint(double x, double yCore, double yIntegral) {
            X = x;
            YCore = yCore;
            YIntegral = yIntegral;
        }

        public override string ToString() => $"X={X}, Yc={YCore}, Yi={YIntegral}";
    }

    class ModelGenerator : Generator {
        protected int parameterQ;
        protected double parameterD;
        CrossPoint[] model;

        public ModelGenerator() : base() {
            name = "Моделирующий";
            AddNewControl("качество", "q", 100M, 0, 1M);
            AddNewControl("диапазон моделирования", "d", 4M, 2);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterQ = (int)(controls["q"].Tag as NumericUpDown).Value;
            parameterD = (double)(controls["d"].Tag as NumericUpDown).Value;
        }

        public override void Prepare() {
            base.Prepare();
            double left = parameterM - parameterSi * parameterD;
            double right = parameterM + parameterSi * parameterD;

            model = new CrossPoint[parameterQ + 1];
            for (int i = 0; i <= parameterQ; i++) {
                double x = (double)i / parameterQ * (right - left) + left;
                double y = CoreFunction(x);
                model[i] = new CrossPoint(x, y, IntegralFunction(x));
            }
        }

        public override double Next() {
            double rand = random.NextDouble();
            int il = 0;
            int ir = model.Length - 1;
            int im = (il + ir) / 2;
            while (ir - il > 1) {
                if (rand >= model[im].YIntegral) {
                    il = im;
                } else {
                    ir = im;
                }
                im = (il + ir) / 2;
            }
            double yl = model[il].YCore;
            double yr = model[ir].YCore;
            double yd = rand - model[il].YIntegral;
            double local_rand = yd / (model[ir].YIntegral - model[il].YIntegral);
            if (yr > yl) {
                double body = yl / yr;
                if (local_rand > body) {
                    local_rand = random.NextDouble() + random.NextDouble();
                    if (local_rand >= 1) {
                        local_rand = 1 - (local_rand - 1);
                    }
                } else {
                    local_rand = random.NextDouble();
                }
            } else {
                double body = yr / yl;
                if (local_rand > body) {
                    local_rand = random.NextDouble() + random.NextDouble();
                    if (local_rand >= 1) {
                        local_rand = 1 - (local_rand - 1);
                    }
                    local_rand = 1 - local_rand;
                } else {
                    local_rand = random.NextDouble();
                }
            }
            double xd = local_rand * (model[ir].X - model[il].X);
            double x = xd + model[il].X;
            return x;
        }
    }

    class BoxMullerGenerator : Generator {
        public BoxMullerGenerator() : base() {
            name = "Бокс-Мюлллер";
        }

        public override double Next() {
            double u1 = 1.0 - random.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            double randNormal = parameterM + parameterSi * randStdNormal; //random normal(mean,stdDev^2)
            return randNormal;
        }
    }
}
