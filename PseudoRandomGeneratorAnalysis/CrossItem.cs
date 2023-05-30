using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PseudoRandomGeneratorAnalysis {
    internal class CrossItem : IComparable {
        public double X;
        public double? Y;
        public double? YCorePerfect;
        public double? YIntegralPerfect;

        public CrossItem(double x) {
            X = x;
        }

        public CrossItem(double x, double? y) {
            X = x;
            Y = y;
        }

        public CrossItem(double x, double? y, double? yCorePerfect) {
            X = x;
            Y = y;
            YCorePerfect = yCorePerfect;
        }

        public CrossItem(double x, double? y, double? yCorePerfect, double? yIntegralPerfect) {
            X = x;
            Y = y;
            YCorePerfect = yCorePerfect;
            YIntegralPerfect = yIntegralPerfect;
        }

        public int CompareTo(object obj) {
            return CompareTo(obj as CrossItem);
        }

        public int CompareTo(CrossItem crossItem) {
            return X.CompareTo(crossItem.X);
        }

        public override bool Equals(object obj) => obj is CrossItem && Equals((CrossItem)obj);

        public bool Equals(CrossItem item) => X.Equals(item.X);

        public override int GetHashCode() => X.GetHashCode();

        public override string ToString() => $"X={X}, Y={Y}, Ycore={YCorePerfect}, Yintegral={YIntegralPerfect}";
    }
}
