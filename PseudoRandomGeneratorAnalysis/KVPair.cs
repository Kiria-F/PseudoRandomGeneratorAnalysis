using System;

namespace PseudoRandomGeneratorAnalysis {
    internal class KVPair <TK, TV> {
        public TK Key;
        public TV Value;

        public KVPair() {
        }

        public KVPair(TK key, TV value) {
            Key = key;
            Value = value;
        }

        public override bool Equals(object obj) => obj is KVPair<TK, TV> && Equals((KVPair<TK, TV>)obj);

        public bool Equals(KVPair<TK, TV> pair) => Key.Equals(pair.Key) && Value.Equals(pair.Value);

        public override int GetHashCode() => Key.GetHashCode() ^ Value.GetHashCode();

        public override string ToString() => $"{{K={Key}, V={Value}}}";
    }
}
