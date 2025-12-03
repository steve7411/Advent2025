using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day02;

internal sealed class Day02 : DayBase {
    private const ulong NINES_BCD = 0x9999999999999999;

    private static ReadOnlySpan<uint> Factors => [0, 0, 0b10, 0b10, 0b110, 0b10, 0b1110, 0b10, 0b10110, 0b1010, 0b100110];

    private readonly ulong symmetricSum;
    private readonly ulong repeatingSum;

    [SkipLocalsInit]
    public Day02() {
        using var reader = GetDataReader();

        Span<char> buffer = stackalloc char[10];
        for (var write = 0; !reader.EndOfStream; ++write) {
            var sp = reader.ReadUntil('-', buffer);
            var startBCD = ReadBCD(sp);
            sp = reader.ReadUntil(',', buffer);
            var endBCD = ReadBCD(sp);
            symmetricSum += GetSymmetricSumForRange(startBCD, endBCD);
            repeatingSum += GetRepeatingSumForRange(startBCD, endBCD);
        }
    }

    private static ulong ReadBCD(ReadOnlySpan<char> span) {
        var res = 0UL;
        for (var i = 0; i < span.Length; ++i)
            res = res << 4 | span[i] & 0xFU;
        return res;
    }

    private static ulong GetSymmetricUpperBound(ulong numBCD) {
        var log10 = MathUtils.Log10BCD(numBCD);
        if ((log10 & 1) == 1)
            return NINES_BCD & (1UL << (log10 >>> 1 << 2)) - 1;

        var halfDigitShift = log10 << 1 & ~3;
        var halfMask = (1UL << halfDigitShift) - 1;
        var upperBCD = numBCD >>> halfDigitShift;
        var lowerBCD = numBCD & halfMask;
        return upperBCD > lowerBCD ? Dec(upperBCD) : upperBCD;
    }

    private static ulong GetSymmetricLowerBound(ulong numBCD) {
        var log10 = MathUtils.Log10BCD(numBCD);
        if ((log10 & 1) == 1)
            return 1UL << (log10 >>> 1 << 2);

        var halfDigitShift = log10 << 1 & ~3;
        var halfMask = (1UL << halfDigitShift) - 1;
        var upperBCD = numBCD >>> halfDigitShift;
        var lowerBCD = numBCD & halfMask;
        return upperBCD < lowerBCD ? Inc(upperBCD) : upperBCD;
    }

    private static ulong GetRepeatingUpperBound(ulong numBCD, int groupSize, int groupsCount) {
        Debug.Assert(MathUtils.Log10BCD(numBCD) == groupSize * groupsCount);
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
        Debug.Assert(MathUtils.Log10BCD(numBCD) == groupSize * groupsCount);
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

        Debug.Assert(MathUtils.Log10BCD(lowerBoundBCD) == MathUtils.Log10BCD(upperBoundBCD));
        var sum = GetSumOfConsecutive(lowerBoundBCD, upperBoundBCD);
        return sum + sum * MathUtils.PowTens[MathUtils.Log10BCD(lowerBoundBCD)];
    }

    private static ulong GetRepeatingSumForRange(ulong startBCD, ulong endBCD) {
        var (startLog10, endLog10) = (MathUtils.Log10BCD(startBCD), MathUtils.Log10BCD(endBCD));
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

            Debug.Assert(MathUtils.Log10BCD(lowerBoundBCD) == MathUtils.Log10BCD(upperBoundBCD));
            var groupSum = GetSumOfConsecutive(lowerBoundBCD, upperBoundBCD);
            var duplicateGroupSum = GetRepeatingSumForRange(lowerBoundBCD, upperBoundBCD);
            groupSum -= duplicateGroupSum;
            total += groupSum;

            for (int i = groupSize, end = groupSize * groupCount; i < end; i += groupSize)
                total += groupSum * MathUtils.PowTens[i];
        }
        return total;

    }

    private static ulong GetSumOfConsecutive(ulong startBCD, ulong endBCD) {
        var lower = MathUtils.FromBCD(startBCD);
        var upper = MathUtils.FromBCD(endBCD);
        var count = upper - lower + 1;
        return count * (lower + upper) >>> 1;
    }

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
        Print("The sum of the summetric ids is: {0}", symmetricSum);
        return symmetricSum;
    }

    public override object? Part2() {
        Print("The sum of the repeating ids is: {0}", repeatingSum);
        return repeatingSum;
    }
}
