using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day04;

internal unsafe sealed class Day04 : DayBase {
    const uint NINE_BITS = 0x1FF;

    private struct Buffer { public fixed ulong cells[139 << 2]; }

    private readonly int width;
    private readonly int height;
    private readonly int elemsPerRow;
    private Buffer grid;

    [SkipLocalsInit]
    public Day04() {
        Span<char> newlineDump = stackalloc char[Environment.NewLine.Length];

        using var reader = GetDataReader();
        (width, height) = reader.BaseStream.GetLineInfoForRegularFile();
        elemsPerRow = width + 64 >>> 6;

        for (var rowOffset = 0; !reader.EndOfStream; rowOffset += 4) {
            var rem = width + 1;
            for (var i = 0; i < elemsPerRow; ++i, rem -= 64) {
                var end = Math.Min(64, rem);
                ref var bits = ref grid.cells[rowOffset + i];
                for (var x = i == 0 ? 1 : 0; x < end; ++x)
                    bits |= (ulong)(reader.Read() >>> 6) << x;
            }
            reader.Read(newlineDump);
        }
    }

    private uint CountRemovable() {
        var count = 0U;
        Span<uint> neighborhoods = stackalloc uint[width];
        for (var y = 0; y <= height; ++y) {
            var rowOffset = y << 2;
            var rem = width;
            for (var i = 0; i < elemsPerRow; ++i, rem -= 64) {
                var bits = grid.cells[rowOffset + i];
                var idx = i << 6;
                var end = Math.Min(2, rem);
                for (int j = 0, k = 0; j < 2; ++j) {
                    for (; k < end; ++k, ++idx, bits >>>= 1) {
                        ref var relevant = ref neighborhoods[idx];
                        relevant = relevant << 3 & NINE_BITS | (uint)bits & 7;
                        count += Popcnt.PopCount(relevant & ~0x10U) < 4 ? relevant >>> 4 & (uint)(-y >>> 31) : 0;
                    }
                    bits |= grid.cells[rowOffset + i + 1] << 62;
                    end = Math.Min(64, rem);
                }
            }
        }
        return count;
    }

    [SkipLocalsInit]
    private uint RemoveAllPossible() {
        var removeCount = 0U;
        Span<uint> buffer = stackalloc uint[width + 2];
        var neighborhoods = buffer[1..^1];

        for (var count = 1U; count != 0; removeCount += count) {
            neighborhoods.Clear();
            count = 0;
            for (var y = 0; y <= height; ++y) {
                var rowOffset = y << 2;
                var rem = width;
                for (var i = 0; i < elemsPerRow; ++i, rem -= 64) {
                    var bits = grid.cells[rowOffset + i];
                    var idx = i << 6;
                    var end = Math.Min(2, rem);
                    for (int j = 0, k = 1; j < 2; ++j) {
                        for (; k <= end; ++k, ++idx, bits >>>= 1) {
                            ref var relevant = ref neighborhoods[idx];
                            relevant = relevant << 3 & NINE_BITS | (uint)bits & 7;
                            if (Popcnt.PopCount(relevant & ~0x10U) < 4 & (relevant & 16) != 0 & y > 0) {
                                ++count;
                                Unsafe.Add(ref relevant, -1) ^= 32;
                                relevant ^= 16;
                                Unsafe.Add(ref relevant, 1) ^= 1;
                                var gridOffset = k >>> 6;
                                grid.cells[rowOffset - 4 + i + gridOffset] ^= 1UL << k;
                            }
                        }
                        bits |= grid.cells[rowOffset + i + 1] << 62;
                        end = Math.Min(64, rem);
                    }
                }
            }
        }
        return removeCount;
    }

    public override object? Part1() {
        var count = CountRemovable();
        Print("The number of accessible paper rolls is: {0}", count);
        return Box<uint>.Instance(count);
    }

    public override object? Part2() {
        var removeCount = RemoveAllPossible();
        Print("The number of accessible paper rolls after all rounds: {0}", removeCount);
        return Box<uint>.Instance(removeCount);
    }
}
