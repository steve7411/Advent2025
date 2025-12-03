using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025;

internal static class MathUtils {
    public static ReadOnlySpan<ulong> PowTens => [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000];

    public static T LCM<T>(T a, T b) where T : INumber<T> =>
        T.Abs(a * b) / GCD(a, b);

    public static T GCD<T>(T a, T b) where T : INumberBase<T>, IModulusOperators<T, T, T> =>
        b == T.Zero ? a : GCD(b, a % b);

    public static ulong GetNextWithSamePopulation(this ulong n) {
        var lsb = Bmi1.X64.ExtractLowestSetBit(n);
        var ripple = n + lsb;
        var newLsb = Bmi1.X64.ExtractLowestSetBit(ripple);
        var ones = (newLsb >>> (int)Bmi1.X64.TrailingZeroCount(lsb) + 1) - 1U;
        return ripple | ones;
    }

    public static ulong GetPreviousWithSamePopulation(this ulong n) {
        var ripple = n + 1U;
        var diff = ripple ^ n;
        var common = ripple & n;
        var commonLSB = Bmi1.X64.ExtractLowestSetBit(common);
        return common - (commonLSB >>> (int)Bmi1.X64.TrailingZeroCount(diff + 1U));
    }

    public static uint GetNextWithSamePopulation(this uint n) {
        var lsb = Bmi1.ExtractLowestSetBit(n);
        var ripple = n + lsb;
        var newLsb = Bmi1.ExtractLowestSetBit(ripple);
        var ones = (newLsb >>> (int)Bmi1.TrailingZeroCount(lsb) + 1) - 1U;
        return ripple | ones;
    }

    public static uint GetPreviousWithSamePopulation(this uint n) {
        var ripple = n + 1U;
        var diff = ripple ^ n;
        var common = ripple & n;
        var commonLSB = Bmi1.ExtractLowestSetBit(common);
        return common - (commonLSB >>> (int)Bmi1.TrailingZeroCount(diff + 1U));
    }

    public static T GetNextWithSamePopulation<T>(this T n) where T : IBinaryInteger<T> {
        var lsb = n & -n;
        var ripple = n + lsb;
        var newLsb = ripple & -ripple;
        var ones = (newLsb >>> int.CreateTruncating(T.TrailingZeroCount(lsb)) + 1) - T.One;
        return ripple | ones;
    }

    public static T GetPreviousWithSamePopulation<T>(this T n) where T : IBinaryInteger<T> {
        var ripple = n + T.One;
        var diff = ripple ^ n;
        var common = ripple & n;
        var commonLSB = common & -common;
        return common - (commonLSB >>> int.CreateTruncating(T.TrailingZeroCount(diff + T.One)));
    }

    public static T CombinationCount<T>(T n, T r) where T : INumber<T> {
        var ans = T.One;
        for (T i = T.One, j = n - r + T.One; i <= r; ++i, ++j)
            ans = ans * j / i;
        return ans;
    }

    public static uint ReverseBits(uint n) {
        const uint EVEN_BITS = 0x55555555;
        const uint EVEN_ADJACENT_PAIR_BITS = 0x33333333;
        const uint EVEN_NIBBLES = 0x0F0F0F0F;
        const uint EVEN_BYTES = 0x00FF00FF;
        const uint EVEN_ADJACENT_PAIR_BYTES = 0x0000FFFF;

        // Swap even and odd bits, then even and odd pairs, then even and odd nibbles, etc
        n = (n & EVEN_BITS) << 1 | (n & ~EVEN_BITS) >> 1;
        n = (n & EVEN_ADJACENT_PAIR_BITS) << 2 | (n & ~EVEN_ADJACENT_PAIR_BITS) >> 2;
        n = (n & EVEN_NIBBLES) << 4 | (n & ~EVEN_NIBBLES) >> 4;
        n = (n & EVEN_BYTES) << 8 | (n & ~EVEN_BYTES) >> 8;
        n = (n & EVEN_ADJACENT_PAIR_BYTES) << 16 | (n & ~EVEN_ADJACENT_PAIR_BYTES) >> 16;
        return n;
    }

    public static ulong ReverseBits(ulong n) {
        const ulong EVEN_BITS = 0x5555555555555555;
        const ulong EVEN_ADJACENT_PAIR_BITS = 0x3333333333333333;
        const ulong EVEN_NIBBLES = 0x0F0F0F0F0F0F0F0F;
        const ulong EVEN_BYTES = 0x00FF00FF00FF00FF;
        const ulong EVEN_ADJACENT_PAIR_BYTES = 0x0000FFFF0000FFFF;
        const ulong EVEN_ADJACENT_QUAD_BYTES = 0x00000000FFFFFFFF;

        // Swap even and odd bits, then even and odd pairs, then even and odd nibbles, etc
        n = (n & EVEN_BITS) << 1 | (n & ~EVEN_BITS) >> 1;
        n = (n & EVEN_ADJACENT_PAIR_BITS) << 2 | (n & ~EVEN_ADJACENT_PAIR_BITS) >> 2;
        n = (n & EVEN_NIBBLES) << 4 | (n & ~EVEN_NIBBLES) >> 4;
        n = (n & EVEN_BYTES) << 8 | (n & ~EVEN_BYTES) >> 8;
        n = (n & EVEN_ADJACENT_PAIR_BYTES) << 16 | (n & ~EVEN_ADJACENT_PAIR_BYTES) >> 16;
        n = (n & EVEN_ADJACENT_QUAD_BYTES) << 32 | (n & ~EVEN_ADJACENT_QUAD_BYTES) >> 32;
        return n;
    }

    public static ulong FromBCD(ulong bcd) {
        Debug.Assert(Log10BCD(bcd) < PowTens.Length);
        var res = 0UL;
        ref var pow = ref Unsafe.AsRef(in PowTens[0]);
        for (; bcd != 0; bcd >>= 4, pow = ref Unsafe.Add(ref pow, 1))
            res += pow * (bcd & 0xF);
        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Log10BCD(ulong numBCD) => BitOperations.Log2(numBCD) + 4 >>> 2;
}
