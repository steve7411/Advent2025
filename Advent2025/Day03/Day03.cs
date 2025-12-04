using System.Runtime.CompilerServices;

namespace Advent2025.Day03;

internal sealed class Day03 : DayBase {
    private readonly ulong sumOfTwoDigitMaxes;
    private readonly ulong sumOfTwelveDigitMaxes;

    public Day03() {
        using var reader = GetDataReader();
        while (!reader.EndOfStream) {
            var (twoDigitBCD, twelveDigitBCD) = ProcessLine(reader);
            sumOfTwoDigitMaxes += twoDigitBCD;
            sumOfTwelveDigitMaxes += twelveDigitBCD;
        }
    }

    [SkipLocalsInit]
    private static (ulong twoDigitBCD, ulong twelveDigitBCD) ProcessLine(StreamReader reader) {
        const int BUFFER_LEN = 100;
        
        var buffer = reader.ReadLine(stackalloc byte[BUFFER_LEN]);
        Span<byte> nextMaxes = stackalloc byte[buffer.Length - 12];
        byte maxIdx = (byte)nextMaxes.Length;
        for (var i = nextMaxes.Length - 1; i >= 0; --i) {
            if (buffer[maxIdx] <= buffer[i])
                maxIdx = (byte)i;
            nextMaxes[i] = maxIdx;
        }

        return (MathUtils.FromBCD(GetMax(buffer, nextMaxes, 2)), MathUtils.FromBCD(GetMax(buffer, nextMaxes, 12)));
    }

    private static ulong GetMax(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> nextMaxes, int digits) {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int maxIdx(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> nextMaxes, int idx, int end) {
            var max = idx = idx < nextMaxes.Length ? nextMaxes[idx] : idx;
            for (idx = Math.Max(idx, nextMaxes.Length); idx < end; ++idx) {
                if (buffer[max] < buffer[idx])
                    max = idx;
            }
            return max;
        }

        var joltageBCD = 0UL;
        for (int pow = 1, idx = 0, optionalEnd = buffer.Length - digits; pow <= digits; ++pow, ++idx) {
            idx = maxIdx(buffer, nextMaxes, idx, optionalEnd + pow);
            joltageBCD = joltageBCD << 4 | buffer[idx] & 0xFUL;
        }
        return joltageBCD;
    }

    public override object? Part1() {
        Print("The sum of two digit maxes is: {0}", sumOfTwoDigitMaxes);
        return Box<ulong>.Instance(sumOfTwoDigitMaxes);
    }

    public override object? Part2() {
        Print("The sum of twelve digit maxes is: {0}", sumOfTwelveDigitMaxes);
        return Box<ulong>.Instance(sumOfTwelveDigitMaxes);
    }
}
