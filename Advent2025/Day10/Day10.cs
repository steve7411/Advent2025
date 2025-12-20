using System.Diagnostics;
using System.Numerics;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Advent2025.Day10;

internal readonly unsafe ref struct SixteenSegmentedMemory {
    private const int MAX_JOLTAGE = 10;
    private const int MAX_SIZE = 1 << MAX_JOLTAGE;
    private const int ENDS_PADDING_COUNT = 64 / sizeof(ushort);

    [ThreadStatic]
    private static ushort* baseAddr;
    private readonly ushort* lens;
    private readonly ushort* data;

    public SixteenSegmentedMemory(int len) {
        if (baseAddr is null)
            baseAddr = (ushort*)NativeMemory.AlignedAlloc(((MAX_SIZE << 4) + MAX_SIZE + ENDS_PADDING_COUNT * 2) * sizeof(ushort), sizeof(ushort));

        lens = baseAddr + ENDS_PADDING_COUNT;
        data = lens + len;
        NativeMemory.Clear(lens, (nuint)(len * sizeof(ushort)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Add(uint idx, ushort val) {
        Debug.Assert(lens[idx] < 16);
        data[idx << 4 | lens[idx]++] = val;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<ushort> AsSpan(int idx) => new(data + (idx << 4), lens[idx]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ushort* GetSegment(int idx, out ushort* end) {
        var start = data + (idx << 4);
        end = start + lens[idx];
        return start;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeMemory() {
        if (baseAddr is not null) {
            NativeMemory.AlignedFree(baseAddr);
            baseAddr = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsAny(int idx) => lens[idx] > 0;
}

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
        //Parallel.For(0, inputBuffer.Length, new() { MaxDegreeOfParallelism = -1 }, () => (0U, 0U), (i, _, acc) => {
        //    ref var input = ref inputBuffer[i];
        Parallel.ForEach(inputBuffer, new() { MaxDegreeOfParallelism = -1 }, () => (0U, 0U), (input, _, _, acc) => {
            var (buttons, joltages, desired) = input;
            var (joltage, lights) = joltages.Length switch {
                <= 6 => ShortestPathJoltages<ulong>(joltages, buttons, desired),
                _ => ShortestPathJoltages<UInt128>(joltages, buttons, desired),
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
    private static (uint joltage, uint lights) ShortestPathJoltages<T>(ReadOnlyMemory<int> joltages, ReadOnlyMemory<int> buttons, uint lights) where T : unmanaged, IBinaryInteger<T> {
        var buttonSpan = buttons.Span;
        var maskEnd = 1 << buttons.Length;
        var combs = stackalloc T[maskEnd];
        (*combs, combs[1]) = (T.Zero, SWARHelper<T>.Expand((uint)buttonSpan[0]));
        var masks = stackalloc uint[maskEnd];
        (*masks, masks[1]) = (0, (uint)buttonSpan[0]);

        for (uint msb = 2, log = 1; msb < maskEnd; msb <<= 1, ++log) {
            var button = (uint)buttonSpan[(int)log];
            TensorPrimitives.Add(new(combs, (int)msb), SWARHelper<T>.Expand(button), new(combs + msb, (int)msb));
            TensorPrimitives.Xor(new(masks, (int)msb), button, new(masks + msb, (int)msb));
        }

        SixteenSegmentedMemory parityMap = new(1 << joltages.Length);
        for (var i = 0; i < maskEnd; ++i)
            parityMap.Add(masks[i], (ushort)i);

        var minLights = uint.MaxValue >>> 2;
        for (var curr = parityMap.GetSegment((int)lights, out var end); curr < end; ++curr)
            minLights = MathUtils.Min(minLights, Popcnt.PopCount(*curr));

        var target = SWARHelper<T>.Expand(joltages.Span);
        var mVal = SearchDict(combs, target, parityMap);
        //var mVal = DFS(combs, target, parityMap, []);
        //var mVal = SearchLinearScan(combs, target, parityMap);
        //var mVal = BFS(combs, target, parityMap);
        //var mVal = SearchDijkstra(combs, target, parityMap);
        return (mVal, minLights);
    }

    [SkipLocalsInit]
    private static uint SearchLinearScan<T>(T* combs, in T requirements, in SixteenSegmentedMemory parityMap) where T : unmanaged, IBinaryInteger<T> {
        var prevBuffer = stackalloc T[MAX_SEARCH_ROW_LEN];
        var nextBuffer = stackalloc T[MAX_SEARCH_ROW_LEN];
        var prevMins = stackalloc uint[MAX_SEARCH_ROW_LEN];
        var nextMins = stackalloc uint[MAX_SEARCH_ROW_LEN];

        var (prevLen, nextLen) = (1, 0);
        (*prevBuffer, *prevMins) = (requirements, 0);

        var hasZero = false;
        for (var i = 0; prevLen > 1 | !hasZero; ++i) {
            var nextSpan = ReadOnlySpan<T>.Empty;
            for (var j = 0; j < prevLen; ++j) {
                var n = prevBuffer[j];
                var cost = prevMins[j];
                var curr = parityMap.GetSegment((int)SWARHelper<T>.CompressParity(n), out var end);
                end = n == T.Zero ? curr + 1 : end;
                for (; curr < end; ++curr) {
                    var p = (uint)*curr;
                    var remaining = n - combs[p];
                    if ((remaining & SWARHelper<T>.BORROW_MASK) != T.Zero)
                        continue;

                    var shifted = remaining >>> 1;
                    if (!parityMap.ContainsAny((int)SWARHelper<T>.CompressParity(shifted)))
                        continue;
                    hasZero |= shifted == T.Zero;
                    var nextCost = cost + (Popcnt.PopCount(p) << i);

                    var idx = nextSpan.IndexOf(shifted);
                    if (idx >= 0) {
                        var m = nextMins + idx;
                        *m = MathUtils.Min(*m, nextCost);
                    } else {
                        nextBuffer[nextLen] = shifted;
                        nextMins[nextLen] = nextCost;
                        Debug.Assert(nextLen < MAX_SEARCH_ROW_LEN);
                        nextSpan = new(nextBuffer, ++nextLen);
                    }
                }
            }
            var temp = prevBuffer;
            prevBuffer = nextBuffer;
            nextBuffer = temp;

            var temp2 = prevMins;
            prevMins = nextMins;
            nextMins = temp2;

            (prevLen, nextLen) = (nextLen, 0);
        }

        var zeroIdx = TensorPrimitives.IndexOfMin(new ReadOnlySpan<T>(prevBuffer, prevLen));
        return prevMins[zeroIdx];
    }

    private static uint SearchDict<T>(T* combs, in T requirements, in SixteenSegmentedMemory parityMap) where T : unmanaged, IBinaryInteger<T> {
        Dictionary<T, uint> prev = ThreadDicts<T, uint>.DictA;
        Dictionary<T, uint> next = ThreadDicts<T, uint>.DictB;
        prev.Clear();
        prev.Add(requirements, 0);

        var hasZero = false;
        for (var i = 0; prev.Count > 1 | !hasZero; ++i) {
            next.Clear();
            foreach (var (n, cost) in prev) {
                var curr = parityMap.GetSegment((int)SWARHelper<T>.CompressParity(n), out var end);
                end = n == T.Zero ? curr + 1 : end;
                for (; curr < end; ++curr) {
                    var p = (uint)*curr;
                    var remaining = n - combs[p];
                    if ((remaining & SWARHelper<T>.BORROW_MASK) != T.Zero)
                        continue;

                    var shifted = remaining >>> 1;
                    hasZero |= shifted == T.Zero;
                    ref var d = ref CollectionsMarshal.GetValueRefOrAddDefault(next, shifted, out var exists);
                    d = MathUtils.Min(exists ? d : uint.MaxValue >>> 2, cost + (Popcnt.PopCount(p) << i));
                }
            }
            (prev, next) = (next, prev);
        }
        return prev[T.Zero];
    }

    private static uint SearchDijkstra<T>(T* combs, in T requirements, in SixteenSegmentedMemory parityMap) where T : unmanaged, IBinaryInteger<T> {
        PriorityQueue<(T reqs, int depth), uint> q = new();
        q.Enqueue((requirements, 0), 0);
        while (q.TryDequeue(out var curr, out var cost)) {
            if (curr.reqs == T.Zero)
                return cost;

            for (var ptr = parityMap.GetSegment((int)SWARHelper<T>.CompressParity(curr.reqs), out var end); ptr < end; ++ptr) {
                var p = (uint)*ptr;
                var remaining = curr.reqs - combs[p];
                if ((remaining & SWARHelper<T>.BORROW_MASK) == T.Zero)
                    q.Enqueue((remaining >>> 1, curr.depth + 1), (Popcnt.PopCount(p) << curr.depth) + cost);
            }
        }
        throw new UnreachableException();
    }

    private static uint BFS<T>(T* combs, in T requirements, in SixteenSegmentedMemory parityMap) where T : unmanaged, IBinaryInteger<T> {
        Queue<(T reqs, uint cost, int depth)> q = new();
        q.Enqueue((requirements, 0, 0));
        var (min, hasZero) = (uint.MaxValue >>> 2, false);
        while (q.Count > 0) {
            for (var i = q.Count; i > 0; --i) {
                var (reqs, cost, depth) = q.Dequeue();
                var isZero = reqs == T.Zero;
                hasZero |= isZero;
                if (isZero) {
                    min = MathUtils.Min(min, cost);
                    continue;
                }

                for (var ptr = parityMap.GetSegment((int)SWARHelper<T>.CompressParity(reqs), out var end); ptr < end; ++ptr) {
                    var p = (uint)*ptr;
                    var remaining = reqs - combs[p];
                    if ((remaining & SWARHelper<T>.BORROW_MASK) == T.Zero) {
                        var newCost = (Popcnt.PopCount(p) << depth) + cost;
                        var shifted = remaining >>> 1;
                        q.Enqueue((shifted, newCost, depth + 1));
                    }
                }
            }
        }
        return min;

    }

    private static uint DFS<T>(T* combs, in T requirements, in SixteenSegmentedMemory parityMap, Dictionary<T, uint> memo) where T : unmanaged, IBinaryInteger<T> {
        if (memo.TryGetValue(requirements, out var cached))
            return cached;

        Debug.Assert((requirements & SWARHelper<T>.BORROW_MASK) == T.Zero);
        if (requirements == T.Zero)
            return 0;

        var res = uint.MaxValue >>> 2;
        for (var curr = parityMap.GetSegment((int)SWARHelper<T>.CompressParity(requirements), out var end); curr < end; ++curr) {
            var p = (uint)*curr;
            var remaining = requirements - combs[p];
            if ((remaining & SWARHelper<T>.BORROW_MASK) != T.Zero)
                continue;

            Debug.Assert((remaining & SWARHelper<T>.PARITY_MASK) == T.Zero);
            res = MathUtils.Min(res, Popcnt.PopCount(p) + (DFS(combs, remaining >>> 1, parityMap, memo) << 1));
        }
        return memo[requirements] = res;
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
