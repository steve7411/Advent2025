using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Advent2025.Day05;

using IdRange = (ulong start, ulong end);

internal sealed class Day05 : DayBase {
    private static readonly Comparer<(ulong start, ulong end)> rangeSearchComparer = Comparer<IdRange>.Create(static (a, b) => a.end.CompareTo(b.end));

    private readonly int freshIngredientCount;
    private readonly ulong freshIdCount;

    [SkipLocalsInit]
    public Day05() {
        Span<char> buffer = stackalloc char[15];
        var newLineDump = buffer[..Environment.NewLine.Length];
        using var reader = GetDataReader();

        List<IdRange> ranges = new(182);
        ReadRanges(buffer, reader, ranges);
        reader.Read(newLineDump);

        ranges.Sort();
        var merged = MergeRanges(CollectionsMarshal.AsSpan(ranges), ref freshIdCount);

        for (var write = 0; !reader.EndOfStream; ++write) {
            var sp = reader.ReadLine(buffer);
            var idBCD = ReadBCD(sp);

            var idx = merged.BinarySearch((idBCD, idBCD), rangeSearchComparer);
            idx ^= idx >> 31;
            if (idx < merged.Length && idBCD >= ranges[idx].start & idBCD <= ranges[idx].end)
                ++freshIngredientCount;
        }
    }

    private static Span<IdRange> MergeRanges(Span<(ulong start, ulong end)> span, ref ulong freshIdCount) {
        ref var left = ref span[0];
        ref var right = ref Unsafe.Add(ref left, 1);
        ref var write = ref left;
        var mergedCount = 0;
        for (ref var end = ref Unsafe.Add(ref left, span.Length); Unsafe.IsAddressLessThan(ref right, ref end); left = ref right, right = ref Unsafe.Add(ref right, 1), ++mergedCount) {
            for (; Unsafe.IsAddressLessThan(ref right, ref end) && right.start <= left.end; right = ref Unsafe.Add(ref right, 1)) {
                if (right.end >= left.end)
                    left.end = right.end;
            }
            freshIdCount += MathUtils.FromBCD(left.end) - MathUtils.FromBCD(left.start) + 1;
            write = left;
            write = ref Unsafe.Add(ref write, 1);
        }
        return span[..mergedCount];
    }

    private static void ReadRanges(Span<char> buffer, StreamReader reader, List<IdRange> list) {
        var firstNewLineChar = Environment.NewLine[0];
        for (var write = 0; !reader.EndOfStream & reader.Peek() is not '\r' and not '\n'; ++write) {
            var sp = reader.ReadUntil('-', buffer);
            var startBCD = ReadBCD(sp);
            sp = reader.ReadUntil(firstNewLineChar, buffer);
            var endBCD = ReadBCD(sp);
            list.Add((startBCD, endBCD));
            reader.ConsumeFullNewLine(firstNewLineChar);
        }
    }

    private static ulong ReadBCD(ReadOnlySpan<char> span) {
        var res = 0UL;
        Debug.Assert(span.Length <= sizeof(ulong) * 2);
        for (var i = 0; i < span.Length; ++i) {
            Debug.Assert(BitOperations.Log2(res) + 4 < 64);
            res = res << 4 | span[i] & 0xFU;
        }
        return res;
    }

    public override object? Part1() {
        Print("The number of fresh ingredients is: {0}", freshIngredientCount);
        return Box<int>.Instance(freshIngredientCount);
    }

    public override object? Part2() {
        Print("The number of fresh ingredients IDs is: {0}", freshIdCount);
        return Box<ulong>.Instance(freshIdCount);
    }
}
