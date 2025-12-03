using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day02;

using IdRange = (ulong start, ulong end);

internal sealed class Day02 : DayBase {
    private const ulong NINES_BCD = 0x9999999999999999;

    private static ReadOnlySpan<ulong> PowTens => [1, 10, 100, 1_000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000, 10_000_000_000, 100_000_000_000, 1_000_000_000_000];
    private static ReadOnlySpan<uint> Factors => [0, 0, 0b10, 0b10, 0b110, 0b10, 0b1110, 0b10, 0b10110, 0b1010, 0b100110];

    private readonly IdRange[] ranges = new IdRange[37];

    [SkipLocalsInit]
    public Day02() {
        using var reader = GetDataReader();

        Span<char> buffer = stackalloc char[10];
        for (var write = 0; !reader.EndOfStream; ++write) {
            var sp = reader.ReadUntil('-', buffer);
            var start = ReadBCD(sp);
            sp = reader.ReadUntil(',', buffer);
            ranges[write] = (start, ReadBCD(sp));
        }
    }

    private static ulong ReadBCD(ReadOnlySpan<char> span) {
        var res = 0UL;
        for (var i = 0; i < span.Length; ++i)
            res = res << 4 | span[i] & 0xFU;
        return res;
    }

    private static ulong FromBCD(ulong bcd) {
        Debug.Assert(Log10(bcd) < PowTens.Length);
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
        return upperBCD > lowerBCD ? Dec(upperBCD) : upperBCD;
    }

    private static ulong GetSymmetricLowerBound(ulong numBCD) {
        var log10 = Log10(numBCD);
        if ((log10 & 1) == 1)
            return 1UL << (log10 >>> 1 << 2);

        var halfDigitShift = log10 << 1 & ~3;
        var halfMask = (1UL << halfDigitShift) - 1;
        var upperBCD = numBCD >>> halfDigitShift;
        var lowerBCD = numBCD & halfMask;
        return upperBCD < lowerBCD ? Inc(upperBCD) : upperBCD;
    }

    private static ulong GetRepeatingUpperBound(ulong numBCD, int groupSize, int groupsCount) {
        Debug.Assert(Log10(numBCD) == groupSize * groupsCount);
        var msdShift = groupSize * (groupsCount - 1 << 2);
        var upperGroup = numBCD >>> msdShift;
        var groupShift = groupSize << 2;
        var groupMask = (1UL << groupShift) - 1;
        for (var shift = msdShift - groupShift; shift >= 0; shift -= groupShift) {
            var group = numBCD >>> shift & groupMask;
            if (upperGroup != group)
                return upperGroup > group ? Dec(upperGroup) : upperGroup;
        }
        return upperGroup;
    }

    private static ulong GetRepeatingLowerBound(ulong numBCD, int groupSize, int groupsCount) {
        Debug.Assert(Log10(numBCD) == groupSize * groupsCount);
        var msdShift = groupSize * (groupsCount - 1 << 2);
        var upperGroup = numBCD >>> msdShift;
        var groupShift = groupSize << 2;
        var groupMask = (1UL << groupShift) - 1;
        for (var shift = msdShift - groupShift; shift >= 0; shift -= groupShift) {
            var group = numBCD >>> shift & groupMask;
            if (upperGroup != group)
                return upperGroup < group ? Inc(upperGroup) : upperGroup;
        }
        return upperGroup;
    }

    private static ulong GetSymmetricSumForRange(ulong startBCD, ulong endBCD) {
        var lowerBoundBCD = GetSymmetricLowerBound(startBCD);
        var upperBoundBCD = GetSymmetricUpperBound(endBCD);

        if (lowerBoundBCD > upperBoundBCD)
            return 0;

        Debug.Assert(Log10(lowerBoundBCD) == Log10(upperBoundBCD));
        var sum = GetSumOfConsecutive(lowerBoundBCD, upperBoundBCD);
        return sum + sum * PowTens[Log10(lowerBoundBCD)];
    }

    private static ulong GetRepeatingSumForRange(ulong startBCD, ulong endBCD) {
        var (startLog10, endLog10) = (Log10(startBCD), Log10(endBCD));
        if (endLog10 > startLog10) {
            var powTenBCD = 1UL << (endLog10 - 1 << 2);
            return GetRepeatingSumForRange(startBCD, powTenBCD - 1 & NINES_BCD) + GetRepeatingSumForRange(powTenBCD, endBCD);
        }

        var total = 0UL;
        for (var bits = Factors[startLog10]; bits != 0;) {
            var groupSize = BitOperations.Log2(bits);
            bits ^= 1U << groupSize;

            var groupCount = endLog10 / groupSize;
            var lowerBoundBCD = GetRepeatingLowerBound(startBCD, groupSize, groupCount);
            var upperBoundBCD = GetRepeatingUpperBound(endBCD, groupSize, groupCount);

            if (lowerBoundBCD > upperBoundBCD)
                continue;

            Debug.Assert(Log10(lowerBoundBCD) == Log10(upperBoundBCD));
            var groupSum = GetSumOfConsecutive(lowerBoundBCD, upperBoundBCD);
            var duplicateGroupSum = GetRepeatingSumForRange(lowerBoundBCD, upperBoundBCD);
            groupSum -= duplicateGroupSum;
            total += groupSum;

            for (var i = 1; i < groupCount; ++i)
                total += groupSum * PowTens[i * groupSize];
        }
        return total;

    }

    private static ulong GetSumOfConsecutive(ulong startBCD, ulong endBCD) {
        var lower = FromBCD(startBCD);
        var upper = FromBCD(endBCD);
        var count = upper - lower + 1;
        return count * (lower + upper) >>> 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Log10(ulong numBCD) => BitOperations.Log2(numBCD) + 4 >>> 2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Inc(ulong numBCD) {
        var tzc = (int)Bmi1.X64.TrailingZeroCount(numBCD ^ NINES_BCD);
        var one = 1UL << (tzc & ~3);
        return numBCD + one & ~(one - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Dec(ulong numBCD) {
        var tzc = (int)Bmi1.X64.TrailingZeroCount(numBCD);
        var one = 1UL << (tzc & ~3);
        return numBCD - one | NINES_BCD & (one - 1);
    }

    public override object? Part1() {
        var sum = 0UL;
        foreach (var r in ranges)
            sum += GetSymmetricSumForRange(r.start, r.end);

        Print("The sum of the summetric ids is: {0}", sum);
        return sum;
    }

    public override object? Part2() {
        var sum = 0UL;
        foreach (var r in ranges)
            sum += GetRepeatingSumForRange(r.start, r.end);

        Print("The sum of the repeating ids is: {0}", sum);
        return sum;
    }
}
