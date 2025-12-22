using System.Diagnostics;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal unsafe sealed class Day10 : DayBase {
    private static class ThreadDicts<TKey, TVal> where TKey : notnull {
        [ThreadStatic] private static Dictionary<TKey, TVal>? dictA;
        [ThreadStatic] private static Dictionary<TKey, TVal>? dictB;

        public static Dictionary<TKey, TVal> DictA => dictA ??= new(MAX_SEARCH_ROW_LEN);
        public static Dictionary<TKey, TVal> DictB => dictB ??= new(MAX_SEARCH_ROW_LEN);
    }

    const int SIZE = 193;
    const int MAX_SEARCH_ROW_LEN = 339;

    private static readonly int[] buttonBuffer = new int[SIZE * 10];
    private static readonly int[] joltageBuffer = new int[SIZE * 10];
    private static readonly (ReadOnlyMemory<int> buttons, ReadOnlyMemory<int> joltages, uint desired)[] inputBuffer = new (ReadOnlyMemory<int>, ReadOnlyMemory<int>, uint)[SIZE];

    private readonly uint minPressesLightsSum;
    private readonly uint minPressesJoltagesSum;

    [SkipLocalsInit]
    public Day10() {
        Span<char> newLineDump = stackalloc char[Environment.NewLine.Length];

        using var reader = GetDataReader();
        for (int buttonsIdx = 0, joltageIdx = 0, line = 0; !reader.EndOfStream; ++line) {
            var desired = 0U;

            var ch = reader.Read();
            Debug.Assert(ch == '[');
            for (var i = 0; reader.Peek() != ']'; ++i)
                desired |= ((uint)reader.Read() & 1) << i;

            reader.Read();
            ch = reader.Read();
            Debug.Assert(ch == ' ');
            var buttons = ReadButtons(reader, buttonBuffer.AsMemory(buttonsIdx));
            var joltages = ReadJoltages(reader, joltageBuffer.AsMemory(joltageIdx));
            reader.Read(newLineDump);

            inputBuffer[line] = (buttons, joltages, desired);
            buttonsIdx += buttons.Length;
            joltageIdx += joltages.Length;
        }

        var (localJoltageSum, localLightsSum) = (0U, 0U);
        Parallel.ForEach(inputBuffer, new() { MaxDegreeOfParallelism = -1 }, () => (0U, 0U), static (input, _, _, acc) => {
            var (buttons, joltages, desired) = input;
            var (joltage, lights) = joltages.Length switch {
                <= 6 => ShortestPathJoltages<ulong, SWARHelper64>(joltages, buttons, desired),
                _ => ShortestPathJoltages<UInt128, SWARHelper128>(joltages, buttons, desired),
            };
            return (acc.Item1 + joltage, acc.Item2 + lights);
        }, results => {
            Interlocked.Add(ref localJoltageSum, results.Item1);
            Interlocked.Add(ref localLightsSum, results.Item2);
            SixteenSegmentedMemory.FreeMemory();
        });

        minPressesJoltagesSum = localJoltageSum;
        minPressesLightsSum = localLightsSum;
    }

    private static ReadOnlyMemory<int> ReadJoltages(StreamReader reader, Memory<int> buffer) {
        var ch = reader.Read();
        Debug.Assert(ch == '{');

        var idx = 0;
        var span = buffer.Span;
        for (; reader.Peek() is not (-1 or '\r' or '\n'); ++idx)
            span[idx] = reader.ReadNextInt();
        return buffer.Slice(0, idx);
    }

    private static ReadOnlyMemory<int> ReadButtons(StreamReader reader, Memory<int> buffer) {
        var idx = 0;
        var span = buffer.Span;
        for (; reader.Peek() == '('; ++idx) {
            reader.Read();
            ref var n = ref span[idx];
            for (n = 0; reader.Peek() != ' ';) {
                n |= 1 << (reader.Read() & 0xF);
                var ch = reader.Read();
                Debug.Assert(ch is ',' or ')');
            }
            reader.Read();
        }
        return buffer.Slice(0, idx);
    }

    [SkipLocalsInit]
    private static (uint joltage, uint lights) ShortestPathJoltages<TUInt, Tswar>(ReadOnlyMemory<int> joltages, ReadOnlyMemory<int> buttons, uint lights)
        where TUInt : unmanaged, IBinaryInteger<TUInt>
        where Tswar : ISWARHelper<TUInt> {

        var buttonSpan = buttons.Span;
        var maskEnd = 1 << buttons.Length;
        var combs = stackalloc TUInt[maskEnd];
        (*combs, combs[1]) = (TUInt.Zero, Tswar.Expand((uint)buttonSpan[0]));
        var masks = stackalloc uint[maskEnd];
        (*masks, masks[1]) = (0, (uint)buttonSpan[0]);

        for (uint msb = 2, log = 1; msb < maskEnd; msb <<= 1, ++log) {
            var button = (uint)buttonSpan[(int)log];
            TensorPrimitives.Add(new(combs, (int)msb), Tswar.Expand(button), new(combs + msb, (int)msb));
            TensorPrimitives.Xor(new(masks, (int)msb), button, new(masks + msb, (int)msb));
        }

        SixteenSegmentedMemory parityMap = new(1 << joltages.Length);
        for (var i = 0; i < maskEnd; ++i)
            parityMap.Add(masks[i], (ushort)i);

        var minLights = uint.MaxValue >>> 2;
        for (var curr = parityMap.GetSegment((int)lights, out var end); curr < end; ++curr)
            minLights = MathUtils.Min(minLights, Popcnt.PopCount(*curr));

        var target = Tswar.Expand(joltages.Span);
        var mVal = SearchDict<TUInt, Tswar>(combs, target, parityMap);
        //var mVal = DFS(combs, target, parityMap, []);
        //var mVal = SearchLinearScan(combs, target, parityMap);
        //var mVal = BFS(combs, target, parityMap);
        //var mVal = SearchDijkstra(combs, target, parityMap);
        return (mVal, minLights);
    }

    private static uint SearchDict<TUInt, Tswar>(TUInt* combs, in TUInt requirements, in SixteenSegmentedMemory parityMap)
        where TUInt : unmanaged, IBinaryInteger<TUInt>
        where Tswar : ISWARHelper<TUInt> {
        var prev = ThreadDicts<TUInt, uint>.DictA;
        var next = ThreadDicts<TUInt, uint>.DictB;
        prev.Clear();
        prev.Add(requirements, 0);

        var hasZero = false;
        for (var i = 0; prev.Count > 1 | !hasZero; ++i) {
            next.Clear();
            foreach (var (n, cost) in prev) {
                var curr = parityMap.GetSegment((int)Tswar.CompressParity(n), out var end);
                end = n == TUInt.Zero ? curr + 1 : end;
                for (; curr < end; ++curr) {
                    var p = (uint)*curr;
                    var remaining = n - combs[p];
                    if ((remaining & Tswar.BORROW_MASK) != TUInt.Zero)
                        continue;

                    var shifted = remaining >>> 1;
                    hasZero |= shifted == TUInt.Zero;
                    ref var nextCost = ref CollectionsMarshal.GetValueRefOrAddDefault(next, shifted, out var exists);
                    nextCost = MathUtils.Min(exists ? nextCost : uint.MaxValue >>> 2, cost + (Popcnt.PopCount(p) << i));
                }
            }
            (prev, next) = (next, prev);
        }
        return prev[TUInt.Zero];
    }

    [SkipLocalsInit, Obsolete("Obsolete for part 1, but leaving for posterity since it's very efficient and I think it's neat")]
    private static int ShortestPathLights(int desired, ReadOnlySpan<int> buttons) {
        var end = 1 << buttons.Length;
        var cache = stackalloc uint[end];
        *cache = (uint)desired;
        for (var i = 0; true; ++i) {
            for (var mask = Bmi1.GetMaskUpToLowestSetBit(1U << i); mask < end; mask = mask.GetNextWithSamePopulation()) {
                var buttonIdx = (int)Bmi1.TrailingZeroCount(mask);
                var val = cache[mask] = cache[Bmi1.ResetLowestSetBit(mask)] ^ (uint)buttons[buttonIdx];
                if (val == 0)
                    return (int)Popcnt.PopCount(mask);
            }
        }
    }

    public override object? Part1() {
        Print("The sum of required steps for lights is: {0}", minPressesLightsSum);
        return Box<uint>.Instance(minPressesLightsSum);
    }

    public override object? Part2() {
        Print("The sum of required steps for joltages is: {0}", minPressesJoltagesSum);
        return Box<uint>.Instance(minPressesJoltagesSum);
    }
}
