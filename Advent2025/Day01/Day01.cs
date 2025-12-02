using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Advent2025.Day01;

internal class Day01 : DayBase {
    private const int MOD = 100;

    private readonly int zeroLands;
    private readonly int zeroPasses;

    public Day01() {
        var n = 50;
        using var reader = GetDataReader();
        for (var write = 0; !reader.EndOfStream; ++write) {
            var mask = (reader.Read() >>> 1 & 1) - 1;
            var turn = (reader.ReadNextInt() ^ mask) - mask;

            var prev = n;
            var (rotations, rem) = Math.DivRem(turn, MOD);
            n += rem;
            var gtModBit = MOD - n >>> 31;
            var gteModBit = MOD - 1 - n >>> 31;
            n -= -gteModBit & MOD;
            Debug.Assert(n < MOD);
            var ltZeroBit = n >>> 31;
            n += -ltZeroBit & MOD;
            Debug.Assert(n >= 0);
            var zeroBit = ~-n >>> 31;

            zeroPasses += Abs(rotations) + (gtModBit | ltZeroBit & -prev >>> 31);
            zeroLands += zeroBit;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Abs(int n) {
        var mask = n >> 31;
        return (n ^ mask) - mask;
    }

    public override object? Part1() {
        Print("The number of times the dial ended on 0: {0}", zeroLands);
        return zeroLands;
    }

    public override object? Part2() {
        var count = zeroLands + zeroPasses;
        Print("The number of times the dial passed 0: {0}", count);
        return count;
    }
}
