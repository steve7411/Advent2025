using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

public static class SWARHelper<T> where T : unmanaged, IBinaryInteger<T> {
    public const int SPACING = 10;

    private static readonly ulong LOW_PARITY;
    private static readonly ulong HIGH_PARITY;

    public static readonly T BORROW_MASK;
    public static readonly T PARITY_MASK;
    public static readonly T DIGIT_MASK;
    public static readonly T LEAST_SIGNIFICANT_DIGIT_MASK = (T.One << SPACING) - T.One;

    static unsafe SWARHelper() {
        Debug.Assert(typeof(T) == typeof(ulong) || typeof(T) == typeof(UInt128));

        var totalWidth = int.CreateTruncating(T.LeadingZeroCount(T.Zero));
        var width = totalWidth - totalWidth % SPACING;
        PARITY_MASK = T.One;
        for (var i = SPACING; i < width; i += SPACING)
            PARITY_MASK |= T.One << i;

        BORROW_MASK = PARITY_MASK << SPACING - 1;
        DIGIT_MASK = BORROW_MASK - PARITY_MASK;

        LOW_PARITY = ulong.CreateTruncating(PARITY_MASK);

        Debug.Assert(BitConverter.IsLittleEndian);
        if (PARITY_MASK is UInt128 parity) {
            var lower = (ulong*)&parity;
            HIGH_PARITY = lower[1];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Expand(ReadOnlySpan<int> ints) {
        var res = T.Zero;
        var max = (1 << SPACING - 1) - 1;
        for (var i = ints.Length - 1; i >= 0; --i) {
            Debug.Assert(ints[i] <= max);
            res = res << SPACING | T.CreateTruncating(ints[i]);
        }
        return res;
    }

    public static unsafe T Expand(uint n) {
        var low = Bmi2.X64.ParallelBitDeposit(n, LOW_PARITY);
        if (typeof(T) == typeof(ulong))
            return Unsafe.As<ulong, T>(ref low);

        Debug.Assert(sizeof(T) == 16);
        UInt128 value = new(Bmi2.X64.ParallelBitDeposit(n >>> 7, HIGH_PARITY), low);
        return Unsafe.As<UInt128, T>(ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint CompressParity(T n) {
        n &= PARITY_MASK;
        var lower = (ulong*)&n;
        var low = Bmi2.X64.ParallelBitExtract(*lower, LOW_PARITY);
        if (typeof(T) == typeof(ulong))
            return (uint)low;

        Debug.Assert(sizeof(T) == 16);
        var high = Bmi2.X64.ParallelBitExtract(lower[1], HIGH_PARITY) << 7;
        return (uint)(high | low);
    }

    [SkipLocalsInit]
    public static string GetString(T n, int digitCount = 0) {
        Span<char> buffer = stackalloc char[40];
        if (digitCount == 0)
            digitCount = Math.Max(1, (int.CreateTruncating(T.Log2(n)) + (SPACING - 1)) / SPACING);

        var ten = T.CreateTruncating(10);
        var write = buffer.Length;
        for (var i = digitCount - 1; i >= 0; --i) {
            var digitBits = n >>> i * SPACING & LEAST_SIGNIFICANT_DIGIT_MASK;
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
