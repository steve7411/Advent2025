using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025.Day10;

internal interface ISWARHelpwer<T> where T : unmanaged, IBinaryInteger<T> {
    public const int SPACING = 10;

    abstract static T BORROW_MASK { get; }

    abstract static T Expand(ReadOnlySpan<int> ints);
    abstract static T Expand(uint n);
    abstract static uint CompressParity(in T n);
}

#if DEBUG
public static class SWARDebugHelper<T> where T : unmanaged, IBinaryInteger<T> {

    public static readonly T LEAST_SIGNIFICANT_DIGIT_MASK = (T.One << ISWARHelpwer<T>.SPACING) - T.One;

    [SkipLocalsInit]
    public static string GetString(T n, int digitCount = 0) {
        Span<char> buffer = stackalloc char[40];
        if (digitCount == 0)
            digitCount = Math.Max(1, (int.CreateTruncating(T.Log2(n)) + (ISWARHelpwer<T>.SPACING - 1)) / ISWARHelpwer<T>.SPACING);

        var ten = T.CreateTruncating(10);
        var write = buffer.Length;
        for (var i = digitCount - 1; i >= 0; --i) {
            var digitBits = n >>> i * ISWARHelpwer<T>.SPACING & LEAST_SIGNIFICANT_DIGIT_MASK;
            if (digitBits == T.Zero) {
                buffer[--write] = '0';
                buffer[--write] = ',';
                continue;
            }

            while (digitBits != T.Zero) {
                (digitBits, var digit) = T.DivRem(digitBits, ten);
                buffer[--write] = (char)(uint.CreateTruncating(digit) | '0');
            }
            buffer[--write] = ',';
        }

        return new(buffer[(write + 1)..]);
    }
}
#endif
