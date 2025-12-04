using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day04;

internal sealed class Day04 : DayBase {
    private readonly int width;
    private readonly int height;
    private readonly byte[,] grid;

    public Day04() {
        Span<byte> buffer = stackalloc byte[138];

        using var reader = GetDataReader();
        (width, height) = reader.BaseStream.GetLineInfoForRegularFile();
        grid = new byte[height, width];
        Span<byte> gridSpan = MemoryMarshal.CreateSpan(ref grid[0, 0], grid.Length);
        for(var idx = 0; !reader.EndOfStream; idx += width)
            reader.ReadLine(gridSpan[idx..]);
        gridSpan.RightShift(6);
    }

    private uint GetNeighborCount(int x, int y) {
        var res = 0U;
        var cell = grid[y, x];
        if (cell == 0)
            return 8;
        var (minX, maxX) = (Math.Max(0, x - 1), Math.Min(width - 1, x + 1));
        var (minY, maxY) = (Math.Max(0, y - 1), Math.Min(height - 1, y + 1));
        var rowMask = (1U << (maxX - minX + 1 << 3)) - 1;
        for (var i = minY; i <= maxY; ++i) {
            var bits = Unsafe.As<byte, uint>(ref grid[i, minX]) & rowMask;
            res += Popcnt.PopCount(bits);
        }

        var final = res - cell;
        return final;
    }

    private uint RemoveIfPossible(int x, int y) {
        var res = 0U;
        ref var cell = ref grid[y, x];
        if (cell == 0)
            return 0;
        var (minX, maxX) = (Math.Max(0, x - 1), Math.Min(width - 1, x + 1));
        var (minY, maxY) = (Math.Max(0, y - 1), Math.Min(height - 1, y + 1));
        var rowMask = (1U << (maxX - minX + 1 << 3)) - 1;
        for (var i = minY; i <= maxY; ++i) {
            var bits = Unsafe.As<byte, uint>(ref grid[i, minX]) & rowMask;
            res += Popcnt.PopCount(bits);
        }

        var final = res - cell;
        if (final < 4) {
            cell = 0;
            return 1;
        }
        return 0;
    }

    public override object? Part1() {
        var count = 0U;
        for (var y = 0; y < height; ++y) {
            for (var x = 0; x < width; ++x)
                count += GetNeighborCount(x, y) < 4 ? 1U : 0;
        }

        Print("The number of accessible paper rolls is: {0}", count);
        return Box<uint>.Instance(count);
    }


    public override object? Part2() {
        var removeCount = 0U;
        for (var count = 1U; count != 0;) {
            count = 0;
            for (var y = 0; y < height; ++y) {
                for (var x = 0; x < width; ++x)
                    count += RemoveIfPossible(x, y);
            }
            removeCount += count;
        }

        Print("The number of accessible paper rolls after all rounds: {0}", removeCount);
        return Box<uint>.Instance(removeCount);
    }
}
