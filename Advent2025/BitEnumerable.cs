using System.Numerics;

namespace Advent2025;

public readonly struct BitEnumerable<T>(T num) where T : IBinaryInteger<T> {
    public struct BitEnumerator(T bits) {
        private T bits = bits;
        private T current = T.Zero;

        public readonly T Current => current;

        public bool MoveNext() {
            current = bits & -bits;
            bits ^= current;
            return current != T.Zero;
        }
    }

    public BitEnumerator GetEnumerator() => new(num);
}