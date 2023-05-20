using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PseudoRandomGeneratorAnalysis {

    abstract class Generator {
        protected static readonly Random random = new Random();
        public string name;
        public readonly Dictionary<string, Panel> controls;
        protected double parameter_m;
        protected double parameter_si;

        public Generator() {
            name = "Новый генератор";
            controls = new Dictionary<string, Panel>();
            AddNewControl("Мат. ожидание", "m", 50M, 3);
            AddNewControl("СКв отклонение", "si", 10M, 3, 0M);
        }

        protected NumericUpDown ConstructInput(string name, decimal defaultValue, int quality, decimal? min = null, decimal? max = null) {
            if (!min.HasValue) min = decimal.MinValue;
            if (!max.HasValue) max = decimal.MaxValue;

            NumericUpDown newInput = new NumericUpDown();
            newInput.Name = "input_" + name;
            newInput.DecimalPlaces = quality;
            newInput.Increment = (decimal)(1D / Math.Pow(10, quality));
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

        protected void AddNewControl(string label, string name, decimal defaultValue, int quality, decimal? min = null, decimal? max = null) {
            NumericUpDown newInput = ConstructInput(name, defaultValue, quality, min, max);
            Panel newControl = ConstructControl(label, newInput);
            controls.Add(name, newControl);
        }

        public virtual void CollectParameterValues() {
            parameter_m = (double)(controls["m"].Tag as NumericUpDown).Value;
            parameter_si = (double)(controls["si"].Tag as NumericUpDown).Value;
        }

        public virtual Dictionary<int, ulong> ISequence(ulong randCount) {
            Dictionary<int, ulong> sequence = new Dictionary<int, ulong>();
            for (ulong i = 0; i < randCount; i++) {
                double gen = Next();
                if (gen < -0.5) {
                    gen -= 1;
                }
                int iGen = (int)(gen + 0.5D);
                sequence[iGen] = sequence.TryGetValue(iGen, out ulong count) ? count + 1 : 1;
            }
            return sequence;
        }

        public virtual Dictionary<int, ulong> ISequence(ulong randCount, int leftEdge, int rightEdge) {
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

        public double CoreFunction(double x) {
            double a = 1d / parameter_si / Math.Sqrt(2 * Math.PI);
            double z = -1d / 2 / parameter_si / parameter_si;
            x -= parameter_m;
            return a * Math.Pow(Math.E, x * x * z);
        }

        public abstract void Prepare();

        public abstract double Next();

        public double GetParameterM() {
            return parameter_m;
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

    class GrandCLTGenerator : Generator {
        protected double parameterN;
        protected double preSi;

        public GrandCLTGenerator() : base() {
            name = "Идеальный генератор";
            AddNewControl("Исп. последовательности", "n", 10M, 3);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
        }

        protected double NormalNext() {
            double sum = 0;
            ulong iParameterN = (ulong)parameterN;
            for (ulong i = 0; i < iParameterN; i++) {
                sum += random.NextDouble();
            }
            double rest = (parameterN - iParameterN) * random.NextDouble();
            sum += rest;
            return sum / parameterN;
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
                double x = NormalNext();
                if (data.ContainsKey(x)) {
                    data[x]++;
                } else {
                    data.Add(x, 1);
                }
            }
            return data;
        }

        private int Modificate2I(double x) {
            double gen = (x - 0.5D) * parameter_si / preSi + parameter_m;
            if (gen < -0.5) {
                gen -= 1;
            }
            return (int)(gen + 0.5D);
        }

        private Dictionary<int, ulong> ModificateSequence2I(Dictionary<double, ulong> preData) {
            Dictionary<int, ulong> data = new Dictionary<int, ulong>();
            foreach (KeyValuePair<double, ulong> kvp in preData) {
                int iGen = Modificate2I(kvp.Key); // 1 -> preSi
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

        public override Dictionary<int, ulong> ISequence(ulong randCount) {
            Dictionary<double, ulong> preData = NormalSequence(randCount);
            CalcPreSi(preData);
            Dictionary<int, ulong> data = ModificateSequence2I(preData);
            return data;
        }

        public override Dictionary<int, ulong> ISequence(ulong randCount, int leftEdge, int rightEdge) {
            Dictionary<double, ulong> preData = NormalSequence(randCount);
            CalcPreSi(preData);
            Dictionary<int, ulong> data = ModificateSequence2I(preData, leftEdge, rightEdge);
            return data;
        }

        public override void Prepare() { }

        public override double Next() {
            throw new NotImplementedException();
        }
    }

    class DynamicNCLTGenerator : Generator {
        protected double parameterA;
        protected double parameterB;
        protected double calcedN;

        public DynamicNCLTGenerator() : base() {
            name = "Генератор на динамическом N";
            AddNewControl("параметр a", "a", 1.8456679511M /*0.288077825M*/, 9);
            AddNewControl("параметр b", "b", -0.36761M /*-0.4988888129M*/, 9);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterA = (double)(controls["a"].Tag as NumericUpDown).Value;
            parameterB = (double)(controls["b"].Tag as NumericUpDown).Value;
        }

        public void SetParameters(double parameterA, double parameterB) {
            this.parameterA = parameterA;
            this.parameterB = parameterB;
        }

        public void SetParameters(double[] parameters) {
            parameterA = parameters[0];
            parameterB = parameters[1];
        }

        public void SetSi(double si) {
            parameter_si = si;
        }

        public double GetCalcedN() {
            return calcedN;
        }

        protected double NormalNext() {
            double sum = 0;
            ulong ICalcedN = (ulong)calcedN;
            for (ulong i = 0; i < calcedN; i++) {
                sum += random.NextDouble();
            }
            double rest = (calcedN - ICalcedN) * random.NextDouble();
            sum += rest;
            return sum;
        }

        public override void Prepare() {
            calcedN = 1 + Math.Pow(1 / parameter_si / parameterA, 1 / parameterB);
            //calcedN = parameter_si;
        }

        public override double Next() {
            return NormalNext() - (calcedN / 2) + parameter_m; // / calcedN;
        }
    }

    class ConstNCLTGenerator : Generator {
        protected double parameterA;
        protected double parameterB;
        protected double parameterN;
        protected double preSi;

        public ConstNCLTGenerator() : base() {
            name = "Генератор на ранней мутации";
            AddNewControl("параметр a", "a", 0.288077825M, 9);
            AddNewControl("параметр b", "b", -0.4988888129M, 9);
            AddNewControl("Исп. последовательности", "n", 10M, 3);
        }

        public override void CollectParameterValues() {
            base.CollectParameterValues();
            parameterA = (double)(controls["a"].Tag as NumericUpDown).Value;
            parameterB = (double)(controls["b"].Tag as NumericUpDown).Value;
            parameterN = (double)(controls["n"].Tag as NumericUpDown).Value;
        }

        protected double NormalNext() {
            double sum = 0;
            ulong iParameterN = (ulong)parameterN;
            for (ulong i = 0; i < iParameterN; i++) {
                sum += random.NextDouble();
            }
            double rest = (parameterN - iParameterN) * random.NextDouble();
            sum += rest;
            return sum / parameterN;
        }

        private double Modificate(double x) {
            return (x - 0.5D) * parameter_si / preSi + parameter_m;
        }

        public override Dictionary<int, ulong> ISequence(ulong randCount, int leftEdge, int rightEdge) {
            Dictionary<int, ulong> data = new Dictionary<int, ulong>();

            double preSi = parameterA * Math.Pow(parameterN, parameterB);

            for (ulong i = 0; i < randCount; i++) {
                double gen = Modificate(NormalNext());
                if (gen < -0.5) {
                    gen -= 1;
                }
                int iGen = (int)(gen + 0.5D);
                if (iGen < leftEdge) { } else if (iGen > rightEdge) { } else {
                    data[iGen] = data.TryGetValue(iGen, out ulong count) ? count + 1 : 1;
                }
            }
            return data;
        }

        public override void Prepare() {
            preSi = parameterA * Math.Pow(parameterN, parameterB);
        }

        public override double Next() {
            return Modificate(NormalNext());
        }
    }


    /*
    class CustomNormalGenerator : NormalGenerator {

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

    class RasterNormalGenerator : RasterCustomGenerator {

        public 
            override double CoreFunction(double x) {
            double a = 1d / parameter_si / Math.Sqrt(2 * Math.PI);
            double b = parameter_m;
            double c = parameter_si;
            double z = -1d / 2 / c / c;
            coreFunction = (x) => { x -= b; return a * Math.Pow(Math.E, x * x * z); };
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
    }*/

}
