using System.Runtime.CompilerServices;

namespace Advent2025.Day03;

internal sealed class Day03 : DayBase {
    private readonly int sumOfTwoDigitMaxes;
    private readonly ulong sumOfTwelveDigitMaxes;

    public Day03() {
        using var reader = GetDataReader();
        Span<char> newlineDump = stackalloc char[Environment.NewLine.Length];

        var dp = new ulong[101, 12];
        while (!reader.EndOfStream) {
            var (twoDigitBCD, twelveDigitBCD) = ProcessLine(reader, dp);
            sumOfTwoDigitMaxes += (int)MathUtils.FromBCD((uint)twoDigitBCD);
            sumOfTwelveDigitMaxes += (twelveDigitBCD);
            reader.Read(newlineDump);
        }
    }

    [SkipLocalsInit]
    private static (int twoDigitBCD, ulong twelveDigitBCD) ProcessLine(StreamReader reader, ulong[,] dp) {
        const int BUFFER_LEN = 100;
        Span<byte> buffer = stackalloc byte[BUFFER_LEN];
        Span<byte> maxes = stackalloc byte[BUFFER_LEN];
        var len = ReadLine(reader, buffer, maxes);
        Array.Clear(dp);
        var maxTwelve = MathUtils.FromBCD(GetMax12Digit(buffer[..len], dp));
        return (GetMax2Digit(buffer, maxes, len), maxTwelve);

    }

    private static int GetMax2Digit(Span<byte> buffer, Span<byte> maxes, int len) {
        var max = 0;
        for (var i = len - 1; i >= 0; --i)
            max = Math.Max(max, maxes[i] << 4 | buffer[i]);
        return max;
    }

    private static ulong GetMax12Digit(Span<byte> buffer, ulong[,] dp) {
        for (var idx = buffer.Length - 1; idx >= 0; --idx)
            dp[idx, 0] = Math.Max(buffer[idx], dp[idx + 1, 0]);

        for (int pow = 1, shift = 4; pow < 12; ++pow, shift += 4) {
            for (var idx = buffer.Length - pow - 1; idx >= 0; --idx)
                dp[idx, pow] = Math.Max((ulong)buffer[idx] << shift | dp[idx + 1, pow - 1], dp[idx + 1, pow]);
        }

        return dp[0, 11];
    }

    private static int ReadLine(StreamReader reader, Span<byte> buffer, Span<byte> maxes) {
        var write = 0;
        for (byte max = 0; !reader.EndOfStream & !char.IsWhiteSpace((char)reader.Peek()); ++write) {
            var curr = (byte)(reader.Read() & 0xF);

            buffer[write] = curr;
            maxes[write] = max;

            max = byte.Max(max, curr);
        }
        return write;
    }

    public override object? Part1() {
        Print("The sum of two digit maxes is: {0}", sumOfTwoDigitMaxes);
        return sumOfTwoDigitMaxes;
    }

    public override object? Part2() {
        Print("The sum of twelve digit maxes is: {0}", sumOfTwelveDigitMaxes);
        return sumOfTwelveDigitMaxes;
    }
}
