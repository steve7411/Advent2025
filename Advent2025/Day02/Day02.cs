using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Advent2025.Day02;

using static System.Runtime.InteropServices.JavaScript.JSType;
using IdRange = (ulong start, ulong end);

internal sealed class Day02 : DayBase {
    private const ulong NINES_BCD = 0x9999999999999999;

    private static ReadOnlySpan<ulong> PowTens => [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000];

    private readonly IdRange[] ranges;

    [SkipLocalsInit]
    public Day02() {
        using var reader = GetDataReader();

        Span<char> buffer = stackalloc char[10];
        List<IdRange> list = [];
        while (!reader.EndOfStream) {
            var sp = reader.ReadUntil('-', buffer);
            var start = ReadBCD(sp);
            //Console.Out.Write(sp);
            sp = reader.ReadUntil(',', buffer);
            list.Add((start, ReadBCD(sp)));
            //Console.Out.Write('-');
            //Console.Out.WriteLine(sp);
        }
        ranges = [.. list];
    }

    private static ulong ReadBCD(ReadOnlySpan<char> span) {
        var res = 0UL;
        for (var i = 0; i < span.Length; ++i)
            res = res << 4 | span[i] & 0xFU;
        return res;
    }

    private static ulong FromBCD(ulong bcd) {
        var res = 0UL;
        ref var pow = ref Unsafe.AsRef(in PowTens[0]);
        for (; bcd != 0; bcd >>= 4, pow = ref Unsafe.Add(ref pow, 1))
            res += pow * (bcd & 0xF);
        return res;
    }

    private static ulong GetSymmetricUpperBound(ulong numBCD) {
        var log10 = Log10(numBCD);
        if ((log10 & 1) == 1)
            return NINES_BCD & (1UL << (log10 >>> 1 << 2)) - 1;

        var halfDigitShift = log10 << 1 & ~3;
        var halfMask = (1UL << halfDigitShift) - 1;
        var upperBCD = numBCD >>> halfDigitShift;
        var lowerBCD = numBCD & halfMask;
        return upperBCD > lowerBCD ? upperBCD - 1 : upperBCD;
    }

    private static ulong GetSymmetricLowerBound(ulong numBCD) {
        var log10 = Log10(numBCD);
        if ((log10 & 1) == 1)
            return 1UL << (log10 >>> 1 << 2);

        var halfDigitShift = log10 << 1 & ~3;
        var halfMask = (1UL << halfDigitShift) - 1;
        var upperBCD = numBCD >>> halfDigitShift;
        var lowerBCD = numBCD & halfMask;
        return upperBCD < lowerBCD ? upperBCD + 1 : upperBCD;
    }

    private static ulong GetSymmetricSumForRange(IdRange range) {
        var (start, end) = (FromBCD(range.start), FromBCD(range.end));
        var lowerBoundBCD = GetSymmetricLowerBound(range.start);
        var upperBoundBCD = GetSymmetricUpperBound(range.end);

        if (lowerBoundBCD > upperBoundBCD)
            return 0;

        var lower = FromBCD(lowerBoundBCD);
        var upper = FromBCD(upperBoundBCD);
        var count = upper - lower + 1;
        var sum = count * (lower + upper) >>> 1;
        return sum + sum * PowTens[Log10(lowerBoundBCD)];
    }

    private static int Log10(ulong numBCD) => BitOperations.Log2(numBCD) + 4 >>> 2;

    public override object? Part1() {
        var sum = 0UL;
        checked {
            foreach (var r in ranges) {
                sum += GetSymmetricSumForRange(r);
            }
        }

        Print("The sum of the summetric IDs is: {0}", sum);
        return sum;
    }

    public override object? Part2() {
        return base.Part2();
    }
}
